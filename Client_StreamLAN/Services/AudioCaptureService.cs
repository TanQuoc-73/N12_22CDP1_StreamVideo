using NAudio.Wave;
using System;
using System.Threading;

namespace Client_StreamLAN.Services
{

    public class AudioCaptureService : IDisposable
    {
        private WaveInEvent? _waveIn;
        private UdpSender?   _sender;
        private uint         _seqNo;
        private volatile bool _enabled;

        public static readonly WaveFormat AudioFormat = new(16000, 16, 1);
        public const int AudioPort = 9002;

        public bool IsCapturing => _waveIn != null;

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public void Start(string serverIp)
        {
            Stop();

            _sender = new UdpSender(serverIp, AudioPort);
            _seqNo  = 0;

            _waveIn = new WaveInEvent
            {
                WaveFormat       = AudioFormat,
                BufferMilliseconds = 40 
            };

            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.StartRecording();
            _enabled = true;
        }

        public void Stop()
        {
            _enabled = false;

            if (_waveIn != null)
            {
                try { _waveIn.StopRecording(); } catch { }
                _waveIn.DataAvailable -= OnDataAvailable;
                _waveIn.Dispose();
                _waveIn = null;
            }

            _sender?.Dispose();
            _sender = null;
        }

        private void OnDataAvailable(object? sender, WaveInEventArgs e)
        {
            if (!_enabled || _sender == null || e.BytesRecorded == 0) return;

            try
            {
                byte[] packet = PacketProtocol.Pack(
                    Interlocked.Increment(ref _seqNo),
                    PacketProtocol.FlagAudio,
                    e.Buffer.AsSpan(0, e.BytesRecorded).ToArray());

                if (packet.Length < 65_000)
                    _sender.Send(packet);
            }
            catch
            {
            }
        }

        public void Dispose() => Stop();
    }
}
