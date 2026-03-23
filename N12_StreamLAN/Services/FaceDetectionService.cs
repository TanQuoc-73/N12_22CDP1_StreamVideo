using System;
using System.IO;
using OpenCvSharp;

namespace Server_StreamLAN.Services
{

    public class FaceDetectionService
    {
        private CascadeClassifier? _faceCascade;
        private static readonly Scalar GreenColor = new(0, 255, 0);
        private const int BoxThickness = 2;

        public bool IsAvailable => _faceCascade != null;

        public FaceDetectionService()
        {
            LoadCascade();
        }

        private void LoadCascade()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string[] paths =
            {
                Path.Combine(baseDir, "Data", "haarcascade_frontalface_default.xml"),
                Path.Combine(baseDir, "haarcascade_frontalface_default.xml"),
            };

            foreach (string path in paths)
            {
                if (!File.Exists(path)) continue;
                try
                {
                    _faceCascade = new CascadeClassifier(path);
                    if (!_faceCascade.Empty()) return;
                    _faceCascade.Dispose();
                    _faceCascade = null;
                }
                catch
                {
                    _faceCascade = null;
                }
            }
        }

        public void DetectAndDraw(Mat frame)
        {
            if (_faceCascade == null || frame.Empty()) return;

            try
            {
                using var gray = new Mat();
                Cv2.CvtColor(frame, gray, ColorConversionCodes.BGR2GRAY);
                Cv2.EqualizeHist(gray, gray);

                Rect[] faces = _faceCascade.DetectMultiScale(gray, 1.1, 5, HaarDetectionTypes.ScaleImage, new OpenCvSharp.Size(30, 30));

                foreach (Rect r in faces)
                    Cv2.Rectangle(frame, r, GreenColor, BoxThickness, LineTypes.Link8);
            }
            catch
            {
        
            }
        }

        public void Dispose() => _faceCascade?.Dispose();
    }
}
