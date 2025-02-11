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

namespace FLIRcamTest
    {
    public class FLIR : Cameras
        {
        private INodeMap nodeMap;
        private INodeMap nodeMapTLDevice;
        //Camera camX;
        //private ManagedCamera cam;  // the class version, relating directly under the SDK's class
        private IManagedCamera cam; // the interface version as used in Spinnaker SDK exampels
        private ManagedCameraList camList;
        private ManagedInterfaceList interfaceList;
        private ManagedSystem system;
        
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
            //camX = new Camera();

            //Referencing Acquisition_CSharp.cs example from Spinnaker SDK
            //int startCam(IManagedCamera cam)
            //{

                int result = 0;
                int level = 0;

                FileStream fileStream;

                // Following drawn from NodMapInfo_CSharp.cs example from Spinnaker SDK
                try
                    {
                    // check if permission to write to folder == True
                    try
                        {
                        fileStream = new FileStream(@"fsTest.txt", FileMode.Create);
                        fileStream.Close();
                        File.Delete("fsTest.txt");
                        }
                    catch
                        {
                        Console.WriteLine("Failed to write to current folder, please check permissions.");
                        Console.WriteLine("Press Enter to exit.");
                        Console.ReadLine();
                        return -1;
                        }
                        

                    // RetrieveTL stream nodemap (immutable info of camera, serial number, vendor, model)
                    Console.WriteLine("\n*** Printing TL Device NodeMap ***\n");
                    INodeMap genTLNodeMap = cam.GetTLDeviceNodeMap();
                    result = printCategoryNodeAndAllFeatures(genTLNodeMap.GetNode<ICategory>("Root"), level);

                    // Retrieve TL stream nodemap (provide info on streaming performance)
                    Console.WriteLine("*** PRINTING TL STREAM NODEMAP ***\n");
                    INodeMap nodeMapTLStream = cam.GetTLStreamNodeMap();
                    result = result | printCategoryNodeAndAllFeatures(nodeMapTLStream.GetNode<ICategory>("Root"), level);

                    // Retrieve TL device nodemap and print device information
                    INodeMap nodeMapTLDevice = cam.GetTLDeviceNodeMap();
                    result = PrintDeviceInfo(nodeMapTLDevice);

                    // Initialise camera
                    cam.Init();

                    // Retrieve GenICam nodemap (to configure camera -> image height, width, enable/ disable trigger mode
                    Console.WriteLine("*** PRINTING GENICAM NODEMAP ***\n");
                    //INodeMap appLayerNodeMap = cam.GetNodeMap();
                    INodeMap nodeMap = cam.GetNodeMap();
                    result = result | printCategoryNodeAndAllFeatures(appLayerNodeMap.GetNode<ICategory>("Root"), level);

                    // Acquire images
                    result = result | AcquireImages(cam, nodeMap, nodeMapTLDevice);

                    // End acquisition
                    cam.EndAcquisition();

                    // Deinitialise camera
                    cam.DeInit();
                    cam.Dispose();

                    }
                catch (SpinnakerException ex)
                    {
                        Console.WriteLine("Error: {0}", ex.Message);
                        result = -1;
                    }
                

            // combine the following variable QueryInterface with the above startCam
            int QueryInterface(IManagedInterface managedInterface)
                {
                int resultQuery = 0;

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
                    }
                catch (SpinnakerException ex)
                    {
                    Console.WriteLine("Error " + ex.Message);
                    resultQuery = -1;
                    }
                

                return resultQuery;


                }
            cam.BeginAcquisition();

            }

            // why doesn't ximea cam code from James need to call on functions from within the corresponding SDK??
        public void Disconnect()
            {
            cam.EndAcquisition(); 
            cam.Dispose();
            camList.Clear();
            interfaceList.Clear();
            system.Dispose();
            }

        public WriteableBitmap CaptureImage(WriteableBitmap image)
            {
            try
                {
                //cam.GetImage(out image, 1000);
                cam.BeginAcquisition();
                cam.GetNextImage();

                return image;
                }
            catch (Exception ex) { throw ex; }
            }

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

        public byte[] CaptureImage()
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

                // Create ImageProcessor instance for post processing images
                IManagedImageProcessor processor = new ManagedImageProcessor();
                // Set default image processor color processing method
                processor.SetColorProcessing(ColorProcessingAlgorithm.HQ_LINEAR);
                
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

        // yet to further investigate & edit this _method_
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
        
        // not sure if GetNextImage gets the right image format
        public void SaveSnapshot(string filePath)
        {
            try
            {
                WriteableBitmap image;
                cam.GetNextImage(1000);


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






