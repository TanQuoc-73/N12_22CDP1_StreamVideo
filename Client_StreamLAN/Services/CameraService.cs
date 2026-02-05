using OpenCvSharp;
using System;

namespace Client_StreamLAN.Services
{
    public class CameraService
    {
        private VideoCapture _capture;
        public bool IsRunning => _capture != null && _capture.IsOpened();

        public void Start()
        {
            _capture = new VideoCapture(0);
            if (!_capture.IsOpened())
                throw new Exception("Khong mo duoc webcam");
        }

        public Mat GetFrame()
        {
            if (!IsRunning) return null;
            Mat frame = new Mat();
            _capture.Read(frame);
            return frame;
        }

        public void Stop()
        {
            _capture?.Release();
            _capture?.Dispose();
            _capture = null;
        }
    }
}
