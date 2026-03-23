using OpenCvSharp;
using System;

namespace Client_StreamLAN.Services
{
    public enum StreamState { Stopped, Running, Paused, Reconnecting }

    public class StreamController
    {
        public StreamState State { get; private set; } = StreamState.Stopped;
        public bool IsRunning    => State == StreamState.Running;
        public bool IsPaused     => State == StreamState.Paused;

        public event Action<StreamState>? StateChanged;

        public void Start()
        {
            if (State == StreamState.Stopped) ChangeState(StreamState.Running);
        }
        public void Stop() => ChangeState(StreamState.Stopped);
        public void Pause()
        {
            if (State == StreamState.Running) ChangeState(StreamState.Paused);
        }
        public void Resume()
        {
            if (State == StreamState.Paused || State == StreamState.Reconnecting)
                ChangeState(StreamState.Running);
        }
        public void SetReconnecting() => ChangeState(StreamState.Reconnecting);

        private void ChangeState(StreamState s)
        {
            State = s;
            StateChanged?.Invoke(s);
        }

        public Size Resolution    { get; set; } = new Size(640, 480);
        public int  FrameDelayMs  { get; set; } = 33;  
        public int  ManualQuality { get; set; } = 50; 
        public bool UseAdaptive   { get; set; } = true;

        public bool   FlipH       { get; set; }
        public bool   FlipV       { get; set; }
        public double Brightness  { get; set; } = 0;    
        public double Contrast    { get; set; } = 1.0; 

        public Mat ApplyControls(Mat src)
        {
            var dst = new Mat();
            src.ConvertTo(dst, MatType.CV_8UC3, Contrast, Brightness);

            if (FlipH && FlipV)  Cv2.Flip(dst, dst, FlipMode.XY);
            else if (FlipH)      Cv2.Flip(dst, dst, FlipMode.Y);
            else if (FlipV)      Cv2.Flip(dst, dst, FlipMode.X);

            return dst;
        }
    }
}
