using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FLIRcamTest
{
    public interface Cameras
    {
        void Connect();
        void Disconnect();

        void UpdateParameters(); // get the width, height bit depth from the camera

        WriteableBitmap CaptureImage(WriteableBitmap image); // captures a single frame; for Spinnaker SDK camera

        byte[] CaptureImage();

        //byte[] CaptureImage(ulong image); // added for Spinnaker SDK camera

        void SetExposure(float exposureUs);

        bool IsConnected { get; }

        uint width { get; }
        uint height { get; }
        float exposureUs { get; }
        uint bitDepth { get; }
        string deviceModel { get; }
        bool IsBitDepthChangeImplemented { get; }

        void SetBitDepth(uint bitDepthToSetformat);

        void SaveSnapshot(string filePath);
    }
}
