using OpenCvSharp;
using System;
using System.Linq;

namespace Client_StreamLAN.Services
{
    public class CameraService
    {
        private VideoCapture? _capture;
        private int _currentIndex = 0;

        public bool IsRunning => _capture != null && _capture.IsOpened();

        /// <summary>Scans camera indices 0‥4 and returns how many are available.</summary>
        public static int GetCameraCount()
        {
            int count = 0;
            for (int i = 0; i < 5; i++)
            {
                using var vc = new VideoCapture(i);
                if (vc.IsOpened()) count++;
                else break;
            }
            return Math.Max(1, count);
        }

        public void Start(int index = 0)
        {
            Stop();
            _currentIndex = index;
            _capture = new VideoCapture(index);
            if (!_capture.IsOpened())
                throw new Exception($"Không mở được camera {index}");
        }

        /// <summary>Switch to a different camera while streaming.</summary>
        public void SwitchCamera(int index)
        {
            if (_currentIndex == index && IsRunning) return;
            Start(index);
        }

        public Mat? GetFrame()
        {
            if (!IsRunning) return null;
            var frame = new Mat();
            _capture!.Read(frame);
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
