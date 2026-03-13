using System;
using System.IO;
using OpenCvSharp;

namespace Server_StreamLAN.Services
{
    /// <summary>
    /// Writes video frames to disk using VideoWriter. Saves to Captures folder with timestamp-based filenames.
    /// </summary>
    public class RecordingService
    {
        private readonly object _lock = new();
        private VideoWriter? _writer;
        private string? _currentPath;
        private OpenCvSharp.Size _frameSize;
        private bool _isRecording; // true from Start() until Stop() so UI "Stop" works even before first frame
        private const double DefaultFps = 25.0;
        private const string CapturesFolder = "Captures";
        private static readonly int FourCcMp4 = FourCC.FromString("mp4v");

        /// <summary>True from Start() until Stop(), so Stop button is correct even before first frame.</summary>
        public bool IsRecording => _isRecording;

        public string? CurrentRecordingPath => _currentPath;

        /// <summary>Starts a new recording. Frame size is taken from the first frame written.</summary>
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

        /// <summary>Writes a frame. Initializes VideoWriter on first frame.</summary>
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
                    // Ignore write errors to keep stream stable
                }
            }
        }

        /// <summary>Stops recording and releases the writer so the file is saved and closed.</summary>
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
