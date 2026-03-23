using System;
using System.IO;
using OpenCvSharp;

namespace Server_StreamLAN.Services
{

    public class RecordingService
    {
        private readonly object _lock = new();
        private VideoWriter? _writer;
        private string? _currentPath;
        private OpenCvSharp.Size _frameSize;
        private bool _isRecording; 
        private const double DefaultFps = 25.0;
        private const string CapturesFolder = "Captures";
        private static readonly int FourCcMp4 = FourCC.FromString("mp4v");

        public bool IsRecording => _isRecording;

        public string? CurrentRecordingPath => _currentPath;

        public void Start()
        {
            lock (_lock)
            {
                Stop();
                string dir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, CapturesFolder);
                Directory.CreateDirectory(dir);
                string fileName = $"StreamLAN_{DateTime.Now:yyyyMMdd_HHmmss}.mp4";
                _currentPath = Path.Combine(dir, fileName);
                _frameSize = default;
                _isRecording = true;
            }
        }

        public void WriteFrame(Mat frame)
        {
            if (frame == null || frame.Empty()) return;

            lock (_lock)
            {
                if (!_isRecording || _currentPath == null) return;

                try
                {
                    if (_writer == null || !_writer.IsOpened())
                    {
                        _frameSize = frame.Size();
                        _writer = new VideoWriter(_currentPath, FourCcMp4, DefaultFps, _frameSize);
                        if (!_writer.IsOpened())
                        {
                            _writer.Dispose();
                            _writer = null;
                            return;
                        }
                    }

                    if (frame.Size() != _frameSize)
                    {
                        using var resized = new Mat();
                        Cv2.Resize(frame, resized, _frameSize);
                        _writer.Write(resized);
                    }
                    else
                    {
                        _writer.Write(frame);
                    }
                }
                catch
                {
                  
                }
            }
        }

        public void Stop()
        {
            lock (_lock)
            {
                try
                {
                    _writer?.Release();
                    _writer?.Dispose();
                }
                catch { }
                _writer = null;
                _currentPath = null;
                _isRecording = false;
            }
        }
    }
}
