using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Threading;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using SpinnakerNET;
using SpinnakerNET.GenApi;

namespace FLIRcamTest
    {
    public class FLIR : Cameras
        {
        //cam cam;
        
        // Setting the properties
        public bool IsConnected { get; private set; } = false;

        public bool IsBitDepthChangeImplemented { get; private set; } = true;

        public string deviceModel { get; private set; } = "";

        public float exposureUs { get; private set; }

        private readonly float _exposureMinUs = 56;

        private readonly float _exposureMaxUs = 10000;

        public uint width { get; private set; }

        public uint height { get; private set; }

        public uint bitDepth { get; private set; }


        public void Connect()
            {
            int startCam(IManagedCamera cam)
                {
                int result = 0;
                int level = 0;

                // Following drawn from NodMapInfo_CSharp.cs example from Teledyne SDK
                try
                    {
                    // RetrieveTL stream nodemap (immutable info of camera, serial number, vendor, model)
                    Console.WriteLine("\n*** Printing TL Device NodeMap ***\n");

                    INodeMap genTLNodeMap = cam.GetTLDeviceNodeMap();

                    result = printCategoryNodeAndAllFeatures(genTLNodeMap.GetNode<ICategory>("Root"), level);

                    // Retrieve TL stream nodemap (provide info on streaming performance)
                    Console.WriteLine("*** PRINTING TL STREAM NODEMAP ***\n");

                    INodeMap nodeMapTLStream = cam.GetTLStreamNodeMap();

                    result = result | printCategoryNodeAndAllFeatures(nodeMapTLStream.GetNode<ICategory>("Root"), level);

                    // Initialise camera
                    Console.WriteLine("*** PRINTING GENICAM NODEMAP ***\n");
                    cam.Init();

                    // Retrieve GenICam nodemap (to configure camera -> image height, width, enable/ disable trigger mode
                    INodeMap appLayerNodeMap = cam.GetNodeMap();

                    result = result | printCategoryNodeAndAllFeatures(appLayerNodeMap.GetNode<ICategory>("Root"), level);

                    // Deinitialise camera
                    cam.DeInit();

                    cam.Dispose();

                    }
                catch (SpinnakerException ex)
                    {
                        Console.WriteLine("Error: {0}", ex.Message);
                        result = -1;
                    }

                return result;

                }

            int QueryInterface(IManagedInterface managedInterface)
                {
                int result = 0;

                try
                    {
                    // Retrieve TL nodemap from interface
                    INodeMap nodeMapInterface = managedInterface.GetTLNodeMap();
                    // Print interface display name (1. node is distinguished by type, related to its value's data type;
                    // 2. nodes to be checked for availability & read/writability prior to attempt to read or write)
                    IString iInterfaceDisplayName = nodeMapInterface.GetNode<IString>("InterfaceDisplayName");

                    if (iInterfaceDisplayName != null && iInterfaceDisplayName.IsReadable)
                        {
                        string interfaceDisplayName = iInterfaceDisplayName.Value;

                        Console.WriteLine("{0}", interfaceDisplayName);
                        }
                    else
                        {
                        Console.WriteLine("Interface display name not readable");
                        }

                    // Retrieve list of interfaces from the system (retrieved from the systme object)
                    ManagedSystem system = new ManagedSystem();
                    ManagedInterfaceList interfaceList = system.GetInterfaces();
                    Console.WriteLine("Number of interfaces detected: {0}\n", interfaceList.Count);
                    // Update list of cameras on the interface
                    managedInterface.UpdateCameras();
                    // Retrieve list of cameras from the interface
                    // Camera lists are constructed using list objects of IManagedCamera objects
                    ManagedCameraList camList = managedInterface.GetCameras();
                    // Return if no cameras detected
                    if (camList.Count == 0)
                        {
                        Console.WriteLine("\tNo devices detected.\n");
                        return 0;
                        }
                    else
                        {
                        Console.WriteLine("Number of cameras detected: {0}\n", camList.Count);
                        }

                    // Print device vendor and model name for each camera on the interface
                    for (int i = 0; i < camList.Count; i++)
                        {
                        //Select camera
                        IManagedCamera cam = camList[i];

                        // Retrieve TL device nodemap; please see NodeMapInfo_CSharp example for additional information on TL device nodemaps
                        INodeMap nodeMapTLDevice = cam.GetTLDeviceNodeMap();
                        Console.Write("\tDevice {0} ", i);

                        // Dispose and clear managed camera
                        cam.Dispose();
                        camList.Clear();

                        }
                catch (SpinnakerException ex)
                    {
                    Console.WriteLine("Error " + ex.Message);
                    result = -1;
                    }
                

                return result;
                }
            }

        // Retrieve the desired entry node from the enumeration node
        // We can populate the entry name with symbolic of the desired stream mode
        IEnumEntry iStreamModeCustom = iStreamMode.GetEntryByName(streamMode);
            if (iStreamModeCustom == null || !iStreamModeCustom.IsReadable)
            {
                // Failed to get custom stream mode
                Console.WriteLine("Custom stream mode is not available...");
                return -1;
            }

        public void Disconnect()
            {
            //cam.StopAcquisition();
            //cam.CloseDevice();
            cam.Dispose();
            camList.Clear();
            interfaceList.Clear();
            system.Dispose();
            


            }

        public WriteableBitmap CaptureImage(WriteableBitmap image)
            {
            try
                {
                cam.GetImage(out image, 1000);
                return image;
                }
            catch (Exception ex) { throw ex; }
            }

        public byte[] CaptureImage()
            {
            try
                {
                cam.GetImage(out byte[] byteArrayIn, 1000);
                return byteArrayIn;
                }
            catch (Exception ex) { throw ex; }
            }

        // yet to further investigate this _method_
        public void UpdateParameters()
            {
            throw new NotImplementedException();
            }

        public void SetExposure(float exposure)
            {
            if (exposure < _exposureMinUs)
                throw new ArgumentOutOfRangeException("Error: Camera exposure must be above minimum of " + _exposureMinUs.ToString() + "\u03BBs.");

            if (exposure > _exposureMaxUs)
                {
                throw new ArgumentOutOfRangeException("Error: Camera exposure must be below maximum of " + _exposureMaxUs.ToString() + "\u03BBs.");
                }
            try
                {
                exposureUs = exposure;
                cam.SetParam(PRM.EXPOSURE, exposureUs);
                Thread.Sleep(50);
                cam.GetParam(PRM.EXPOSURE, out float tempExp); // because the value actually set will be slightly different.
                exposureUs = tempExp;
                }
            catch (System.ApplicationException appExc)
                {
                throw appExc;
                }
            }

        public void SetBitDepth(uint bitDepthToSet)
            {
            throw new NotImplementedException();
            }

        public void SaveSnapshot(string filePath)
            {
            try
                {
                WriteableBitmap image;
                cam.GetImage(out image, 1000);


                using (FileStream stream =
            new FileStream(filePath, FileMode.Create))
                    {
                    PngBitmapEncoder encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(image));
                    encoder.Save(stream);
                    }
                }
            catch (Exception ex) { throw ex; }
            }

        ~FLIR()   // destructor to free up resources
            {
            if (IsConnected)
                {
                Disconnect();
                }
            }
        }
    }






