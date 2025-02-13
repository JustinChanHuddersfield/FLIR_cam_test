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
using System.Windows.Media.Converters;
using Spinnaker;
using System.Windows.Media.Media3D;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;

// https://flir.custhelp.com/app/answers/detail/a_id/4327/~/flir-spinnaker-sdk---getting-started-with-the-spinnaker-sdk
namespace FLIRcamTest
    {
    public class FLIR : Cameras
        {
        //FLIR cam;
        private INodeMap nodeMap;
        private INodeMap nodeMapTLDevice;
        //private readonly ManagedCamera cam;  // the class version, relating directly under the SDK's class
        private IManagedCamera cam; // the interface version as used in Spinnaker SDK exampels
        //private ManagedCameraList camList;
        //private ManagedInterfaceList interfaceList;
        //private ManagedSystem system;
        
        //ManagedInterface 

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

        //public 


        public void Connect()
            {
            //Referencing Acquisition_CSharp.cs example from Spinnaker SDK

            int result = 0;
            //cam = new FLIR();
            ManagedSystem system = new ManagedSystem();
            ManagedCameraList camList = new ManagedCameraList();
            ManagedInterfaceList interfaceList = new ManagedInterfaceList();
            IManagedInterface managedInterface = new ManagedInterface();

            try
                {
                // terminate connection if no interface or cameras detected
                if (camList.Count == 0 || interfaceList.Count == 0)
                    {
                    // End acquisition
                    cam.EndAcquisition();

                    // Deinitialise camera & system
                    cam.DeInit();
                    cam.Dispose();

                    // Clear camera list before releasing system
                    camList.Clear();

                    // Clear interface list before releasing system
                    interfaceList.Clear();

                    // Release system
                    system.Dispose();

                    Console.WriteLine("No cameras detected!");
                    Console.WriteLine("Press Enter to exit...");
                    Console.ReadLine();
                   
                    }

                // Following drawn from NodMapInfo_CSharp.cs example from Spinnaker SDK
                if (camList.Count > 0 && camList.Count < 2)
                    {
                    IManagedCamera cam = camList[0];

                    // Initialise camera
                    cam.Init();

                    // Retrieve GenICam nodemap (to configure camera -> image height, width, enable/ disable trigger mode
                    Console.WriteLine("*** PRINTING GENICAM NODEMAP ***\n");
                    INodeMap nodeMap = cam.GetNodeMap();

                    // RetrieveTL stream nodemap (immutable info of camera, serial number, vendor, model)
                    Console.WriteLine("\n*** Printing TL Device NodeMap ***\n");
                    INodeMap genTLNodeMap = cam.GetTLDeviceNodeMap();

                    // Retrieve TL device nodemap and print device information; please see NodeMapInfo_CSharp example for additional information on TL device nodemaps
                    INodeMap nodeMapTLDevice = cam.GetTLDeviceNodeMap();
                    Console.Write("\tDevice {0} ");

                    // Retrieve TL nodemap from interface
                    INodeMap nodeMapInterface = managedInterface.GetTLNodeMap();
                    // Print interface display name (1. node is distinguished by type, related to its value's data type;
                    // 2. nodes to be checked for availability & read/writability prior to attempt to read or write)
                    IString iInterfaceDisplayName = nodeMapInterface.GetNode<IString>("InterfaceDisplayName");

                    // acquire interface display name
                    if (iInterfaceDisplayName != null && iInterfaceDisplayName.IsReadable)
                        {
                        string interfaceDisplayName = iInterfaceDisplayName.Value;

                        Console.WriteLine("{0}", interfaceDisplayName);
                        }
                    else
                        {
                        Console.WriteLine("Interface display name not readable");
                        }

                    // Update list of cameras on the interface
                    managedInterface.UpdateCameras();

                    // Retrieve list of cameras from the interface
                    // Camera lists are constructed using list objects of IManagedCamera objects
                    camList = managedInterface.GetCameras();

                    // Retrieve list of interfaces from the system (retrieved from the systme object)
                    interfaceList = system.GetInterfaces();
                    Console.WriteLine("Number of interfaces detected: {0}\n", interfaceList.Count);


                    }

                }
            catch (SpinnakerException ex)
                {
                Console.WriteLine("Error: {0}", ex.Message);
                throw new SpinnakerException(ex.Message);
                }

            }

        public void Disconnect()
            {
            cam.EndAcquisition(); 
            cam.Dispose();
            camList.Clear();
            interfaceList.Clear();
            system.Dispose();
            }


        // original code adapted from Ximea cam
        //public byte[] CaptureImage()
        //    {
        //    try
        //        {
        //        //cam.GetImage(out byte[] byteArrayIn, 1000);
        //        cam.BeginAcquisition();

        //        return byteArrayIn;
        //        }
        //    catch (Exception ex) { throw ex; }
        //    }

        // Alternative version
        //public WriteableBitmap CaptureImage(WriteableBitmap image)
        //    {
        //    try
        //        {
        //        //cam.GetImage(out image, 1000);
        //        cam.BeginAcquisition();
        //        cam.GetNextImage(1000);

        //        return image;
        //        }
        //    catch (Exception ex) { throw ex; }
        //    }

        // modify code to output byte[] (WIP) 11-02-2025
        public byte[] CaptureImage(IManagedCamera cam, INodeMap nodeMap,INodeMap nodeMapTLDevice)
            {
            //IManagedImage camImageUlong = new ManagedImage();
            try
                {
                // Retrieve enumeration node from nodemap
                IEnum iAcquisitionMode = nodeMap.GetNode<IEnum>("AcquisitionMode");
                // Retrieve entry node from enumeration node
                IEnumEntry iAcquisitionModeContinuous = iAcquisitionMode.GetEntryByName("Continuous");
                // Set symbolic from entry node as new value for enumeration node
                iAcquisitionMode.Value = iAcquisitionModeContinuous.Symbolic;
                
                cam.BeginAcquisition();
                String deviceSerialNumber = "";
                IString iDeviceSerialNumber = nodeMapTLDevice.GetNode<IString>("DeviceSerialNumber");
                deviceSerialNumber = iDeviceSerialNumber.Value;

                // get expolsure time to set an appropriate timeout for GetNextImage
                IFloat iExposureTime = nodeMap.GetNode<IFloat>("ExposureTime");
                // convert exposure time retieved in us to ms for consistency with GetNextImage unit
                double timeout = iExposureTime.Value / 1000 + 1000;

                // Create ImageProcessor instance for post processing images
                IManagedImageProcessor processor = new ManagedImageProcessor();
                // Set default image processor color processing method
                processor.SetColorProcessing(ColorProcessingAlgorithm.HQ_LINEAR);
                
                using (IManagedImage rawImage = cam.GetNextImage((ulong) timeout))
                    {
                    if (rawImage.IsIncomplete)
                        {
                        Console.WriteLine("Image incomplete: image status {0}...", rawImage.ImageStatus);
                        }
                    else
                        {
                        uint width = rawImage.Width;
                        uint height = rawImage.Height;

                        // convert image to mono8
                        using (IManagedImage convertedImage = processor.Convert(rawImage, PixelFormatEnums.Mono8))
                            {
                            String filename = "c#image-";
                            if (deviceSerialNumber != "")
                                {
                                filename = filename + deviceSerialNumber;
                                }
                            filename = filename + ".jpg";
                            convertedImage.Save(filename);
                            }

                        }

                    }

                }

            catch (SpinnakerException ex) { throw ex; }
            }

        public void UpdateParameters()
            {
            throw new NotImplementedException();
            }
        // should work? 11-02-2025
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
                // acquire auto exposure mode
                IEnum iExposureAuto = nodeMap.GetNode<IEnum>("ExposureAuto");
                // turn auto exposure mode to OFF
                IEnumEntry iExposureAutoOff = iExposureAuto.GetEntryByName("Off");
                iExposureAuto.Value = iExposureAutoOff.Value;
                //// set exposure time manually
                //cam.SetParam(PRM.EXPOSURE, exposureUs);   // adapt this line then delete
                IFloat iExposureTime = nodeMap.GetNode<IFloat>("ExposureTime");
                Thread.Sleep(50);
                // ensure eposure time set under max & min limit (if...else)
                iExposureTime.Value = (exposureUs > _exposureMaxUs ? _exposureMaxUs : exposureUs < _exposureMinUs ? _exposureMinUs: exposureUs);
                // convert from double to float
                float expoTime = (float) iExposureTime.Value;
                exposureUs = expoTime;
                
                }
            catch (System.ApplicationException appExc)
                {
                throw appExc;
                }
            }

        // yet to further investigate & edit this _method_
        public void SetBitDepth(uint bitDepthToSet)
            {
            throw new NotImplementedException();
            }
        
        public void SaveSnapshot(string filePath)
        {
            try
            {
                IEnum iAcquisitionMode = nodeMap.GetNode<IEnum>("AcquisitionMode");
                IEnumEntry iAcquisitionModeSingle = iAcquisitionMode.GetEntryByName("SingleFrame");
                // Set symbolic from entry node as new value for enumeration node
                iAcquisitionMode.Value = iAcquisitionModeSingle.Symbolic;

                cam.BeginAcquisition();

                using (IManagedImage rawImage = cam.GetNextImage(1000))
                    {
                    if (rawImage.IsIncomplete)
                        {
                        Console.WriteLine("Image incomplete: image status {0}...", rawImage.ImageStatus);
                        }
                    else
                        {
                        uint width = rawImage.Width;
                        uint height = rawImage.Height;

                        // Create ImageProcessor instance for post processing images
                        IManagedImageProcessor processor = new ManagedImageProcessor();
                        // Set default image processor color processing method
                        processor.SetColorProcessing(ColorProcessingAlgorithm.HQ_LINEAR);
                        // convert rawImage to mono8
                        using (IManagedImage convertedImage = processor.Convert(rawImage, PixelFormatEnums.Mono8))
                            {
                            string filename = "mono8Image-";
                            filename = filename + ".jpg";
                            convertedImage.Save(filename);
                            Console.WriteLine("Imaged saved at {0}\n", filename);
                            }
                        }
                    }

            }
            catch (SpinnakerException ex) { throw ex; }
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






