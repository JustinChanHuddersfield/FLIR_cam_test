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
using System.Collections;
using DrawingColor = System.Drawing.Color;

// https://flir.custhelp.com/app/answers/detail/a_id/4327/~/flir-spinnaker-sdk---getting-started-with-the-spinnaker-sdk
// see C:\Program Files\Teledyne\Spinnaker\doc\Managed for API reference
//
namespace FLIRcamTest
    {
    public class FLIR : Cameras
        {
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

                    // set acquisiton mode to single frame
                    IEnum iAcquisitionMode = nodeMap.GetNode<IEnum>("AcquisitionMode");
                    if (iAcquisitionMode == null || !iAcquisitionMode.IsWritable || !iAcquisitionMode.IsReadable)
                        {
                        Console.WriteLine("Unable to set acquisition mode to single (node retrieval). Aborting...\n");
                        throw new Exception();
                        }
                    IEnumEntry iAcquisitionModeSingle = iAcquisitionMode.GetEntryByName("SingleFrame");
                    if (iAcquisitionModeSingle == null || !iAcquisitionModeSingle.IsReadable)
                        {
                        Console.WriteLine(
                            "Unable to set acquisition mode to single (enum entry retrieval). Aborting...\n");
                        throw new Exception();
                        }
                    // Set symbolic from entry node as new value for enumeration node
                    iAcquisitionMode.Value = iAcquisitionModeSingle.Symbolic;


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


        // modify code to output byte[] (WIP) 11-02-2025
        public byte[] CaptureImage()
            {
            try
                {
                // Retrieve enumeration node from nodemap
                IEnum iAcquisitionMode = nodeMap.GetNode<IEnum>("AcquisitionMode");
                if (iAcquisitionMode == null || !iAcquisitionMode.IsWritable || !iAcquisitionMode.IsReadable)
                    {
                    Console.WriteLine("Unable to set acquisition mode to single (node retrieval). Aborting...\n");
                    throw new Exception();
                    }
                // Retrieve entry node from enumeration node
                IEnumEntry iAcquisitionModeContinuous = iAcquisitionMode.GetEntryByName("Continuous");
                if (iAcquisitionModeContinuous == null || !iAcquisitionModeContinuous.IsReadable)
                    {
                    Console.WriteLine(
                        "Unable to set acquisition mode to single (enum entry retrieval). Aborting...\n");
                    throw new Exception();
                    }
                // Set symbolic from entry node as new value for enumeration node
                iAcquisitionMode.Value = iAcquisitionModeContinuous.Symbolic;
                
                cam.BeginAcquisition();
                String deviceSerialNumber = "";
                IString iDeviceSerialNumber = nodeMapTLDevice.GetNode<IString>("DeviceSerialNumber");
                deviceSerialNumber = iDeviceSerialNumber.Value;

                IEnum iExposureAuto = nodeMap.GetNode<IEnum>("ExposureAuto");
                if (iExposureAuto == null || !iExposureAuto.IsReadable || !iExposureAuto.IsWritable)
                    {
                    Console.WriteLine("Unable to disable automatic exposure (enum retrieval). Aborting...\n");
                    throw new InvalidOperationException("Unable to disable automatic exposure.");
                    }

                // turn auto exposure mode to OFF
                IEnumEntry iExposureAutoOff = iExposureAuto.GetEntryByName("Off");
                if (iExposureAutoOff == null || !iExposureAutoOff.IsReadable)
                    {
                    Console.WriteLine("Unable to disable automatic exposure (entry retrieval). Aborting...\n");
                    throw new InvalidOperationException("Unable to disable automatic exposure.");
                    }
                iExposureAuto.Value = iExposureAutoOff.Value;

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
                if (iExposureAuto == null || !iExposureAuto.IsReadable || !iExposureAuto.IsWritable)
                    {
                    Console.WriteLine("Unable to disable automatic exposure (enum retrieval). Aborting...\n");
                    throw new InvalidOperationException("Unable to disable automatic exposure.");
                    }
                // turn auto exposure mode to OFF
                IEnumEntry iExposureAutoOff = iExposureAuto.GetEntryByName("Off");
                if (iExposureAutoOff == null || !iExposureAutoOff.IsReadable)
                    {
                    Console.WriteLine("Unable to disable automatic exposure (entry retrieval). Aborting...\n");
                    throw new InvalidOperationException("Unable to disable automatic exposure.");
                    }
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

        public (int zStack, int width, int height, float maxIntensity, int maxX, int maxY, int FWHM_Y) FindFWHM(string imagePath)
            {
            
            // load in the images from the folder
            FileInfo imageInfo = new FileInfo(imagePath);

            // find number of images in the folder using LINQ (in indices instead of (um))
            int zStack = Directory.EnumerateFiles(imagePath).Count();

            // determine image width and height
            Bitmap image = new Bitmap(imagePath);
            var width = image.Width;
            var height = image.Height;
            float[,] intensityMatrix = new float[height, width];        // [row, column] 

            // acquire the max X & Y intensity of a single image
            float maxIntensity = 0;
            int maxX = 0;
            int maxY = 0;
            
            for (int y = 0; y < image.Height; y++)
                {
                for (int x = 0; x< image.Width; x++)
                    {
                    // get pixel colour of the grayscale value
                    DrawingColor pixelColor = image.GetPixel(x, y);

                    // if image is already in grayscale, any of the R,G,B channel in grayscale images can be used
                    float grayValue = pixelColor.R;
                    intensityMatrix[y,x] = grayValue;


                    //check which pixel has highest intensity
                    if (grayValue > maxIntensity)
                        {
                        maxIntensity = grayValue;
                        maxX = x;
                        maxY = y;
                        }

                    }
                }

            // determine FWHM of the image (assuming beam is circular and therefore rotationally symmetrical)
            float halfMax = (maxIntensity / 2);
            
            int idxY0 = -1;     // -1 indicates the index in Y not found yet
            int idxY1 = -1;     // 2nd value to be >= halfMax

            for (int y=0; y < height; y++)
                {
                if (intensityMatrix[y, maxX] >= halfMax)
                    {
                    idxY0 = y;  // first y value >= halfMax, aka top side of the beam profile
                    break;
                    }
                }

            for (int y=height - 1; y >= 0; y--)
                {
                if (intensityMatrix[y, maxX] >= halfMax)
                    {
                    idxY1 = y;  // second y value >= halfMax, aka bottom side of the beam profile
                    break;
                    }
                }


            // initialise FWHM_Y
            int FWHM_Y = 0;
            try
                {
                FWHM_Y = (idxY0 != -1 && idxY1 != -1) ? (idxY1 - idxY0 + 1) : 0;
                }
            catch (Exception e)
                {
                Console.WriteLine("Error: {0}", e.Message);
                throw new Exception(e.Message);
                }

            return (zStack, width, height, maxIntensity, maxX, maxY, FWHM_Y);
            }

        // WIP 19-02-2025
        public int FindFocalPlane(string filePath)
            {
            // rudimentary implementation of loading image files from a directory, need further work
            // https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.createdirectory?view=net-9.0
            //DirectoryInfo makeFolder = Directory.CreateDirectory(@"c:\imagesFolder");   // doesnt not account for checking if folder existed

            // curating list of image paths for all images in the folder
            string[] imagePath = Directory.GetFiles(filePath);

            // find number of images in the folder using LINQ (in indices instead of (um))
            int imageCount = Directory.EnumerateFiles(@"c:\imagesFolder").Count();

            int[,] FWHMarray = new int[imageCount,imageCount];

            int zFocalPlane = 0;
            int minFWHM = 10000; // arbitrary large value for comparison to find smallest value

            // acquiring FWHM value of each image & determine which image index has smallest FWHM value
            for (int imgIdx = 1; imgIdx <= imageCount; imgIdx++)
                {
                // find FWHM of each of the images in the stack
                FWHMarray[imgIdx, 0] = FindFWHM(imagePath[imgIdx]).FWHM_Y;
                FWHMarray[imgIdx, 1] = imgIdx - 1;

                }
                
            for (int z = 1; z <= imageCount; z++) {
                // find the image index among the stack with smallest FWHM
                int currentFWHM = FWHMarray[z, 0];

                // update values only if it's less than temporary current minFWHM
                if (FWHMarray[z,0] < minFWHM)
                    {
                    minFWHM = currentFWHM;
                    zFocalPlane = z;
                    // still requires correlating this zFocalPlane to the actual Z stage position (relative and absolute)
                    }
                
                }

            return zFocalPlane;
            }

        // static int AcquireImages(IManagedCamera cam, INodeMap nodeMap,INodeMap nodeMapTLDevice)  
        public string SaveSnapshot(string filePath)
        {
            try
            {

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
                            convertedImage.Save(filePath + filename);   // for post process
                            Console.WriteLine("Imaged saved at {0}\n", filename);
                            }
                        }
                    }
                
            }
            catch (SpinnakerException ex) { throw ex; }

            return filePath;
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






