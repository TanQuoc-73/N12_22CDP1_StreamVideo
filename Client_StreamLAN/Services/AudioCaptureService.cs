using NAudio.Wave;
using System;
using System.Threading;

namespace Client_StreamLAN.Services
{
    /// <summary>
    /// Captures microphone audio using NAudio and sends PCM packets
    /// to the server over a dedicated UDP channel (port 9002).
    /// Format: 16 kHz, 16-bit, mono — good voice quality at low bandwidth.
    /// </summary>
    public class AudioCaptureService : IDisposable
    {
        private WaveInEvent? _waveIn;
        private UdpSender?   _sender;
        private uint         _seqNo;
        private volatile bool _enabled;

        public static readonly WaveFormat AudioFormat = new(16000, 16, 1);
        public const int AudioPort = 9002;

        /// <summary>Whether audio capture is currently active.</summary>
        public bool IsCapturing => _waveIn != null;

        /// <summary>Enable/disable sending audio (mic mute on client side).</summary>
        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        /// <summary>
        /// Starts capturing from the default microphone and sending to the given server.
        /// </summary>
        public void Start(string serverIp)
        {
            Stop();

            _sender = new UdpSender(serverIp, AudioPort);
            _seqNo  = 0;

            _waveIn = new WaveInEvent
            {
                WaveFormat       = AudioFormat,
                BufferMilliseconds = 40  // 40 ms chunks → ~1280 bytes per chunk (fits in UDP)
            };

            _waveIn.DataAvailable += OnDataAvailable;
            _waveIn.StartRecording();
            _enabled = true;
        }

        /// <summary>Stops capturing and releases resources.</summary>
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

                // Only send if packet fits in a single UDP datagram
                if (packet.Length < 65_000)
                    _sender.Send(packet);
            }
            catch
            {
                // Swallow send errors — audio is best-effort
            }
        }

        public void Dispose() => Stop();
    }
}
