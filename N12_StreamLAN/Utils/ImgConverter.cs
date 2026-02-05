using OpenCvSharp;
using OpenCvSharp.WpfExtensions;
using System.Windows.Media.Imaging;

namespace Server_StreamLAN.Utils
{
    public class ImgConverter
    {
        public static BitmapSource ToBitmapSource(Mat mat)
        {
            return mat.ToBitmapSource();
        }
    }
}
