using NAudio.Wave;
using System;

namespace Server_StreamLAN.Services
{
    /// <summary>
    /// Plays incoming PCM audio through the default speakers using NAudio.
    /// Uses a BufferedWaveProvider ring buffer fed by the UDP receiver.
    /// </summary>
    public class AudioPlaybackService : IDisposable
    {
        private WaveOutEvent?        _waveOut;
        private BufferedWaveProvider? _buffer;
        private volatile bool        _muted;

        /// <summary>Must match the client's AudioCaptureService.AudioFormat.</summary>
        public static readonly WaveFormat AudioFormat = new(16000, 16, 1);

        public bool IsMuted
        {
            get => _muted;
            set
            {
                _muted = value;
                if (_waveOut != null)
                    _waveOut.Volume = value ? 0f : _volume;
            }
        }

        private float _volume = 1.0f;
        public float Volume
        {
            get => _volume;
            set
            {
                _volume = Math.Clamp(value, 0f, 1f);
                if (_waveOut != null && !_muted)
                    _waveOut.Volume = _volume;
            }
        }

        /// <summary>Initializes the playback device. Call once at startup.</summary>
        public void Start()
        {
            Stop();

            _buffer = new BufferedWaveProvider(AudioFormat)
            {
                BufferDuration        = TimeSpan.FromSeconds(2),
                DiscardOnBufferOverflow = true   // drop old audio if buffer fills up
            };

            _waveOut = new WaveOutEvent
            {
                DesiredLatency = 100  // ms — low latency playback
            };
            _waveOut.Init(_buffer);
            _waveOut.Volume = _muted ? 0f : _volume;
            _waveOut.Play();
        }

        /// <summary>Feeds a chunk of raw PCM data into the playback buffer.</summary>
        public void AddSamples(byte[] pcmData, int offset, int count)
        {
            if (_buffer == null || _muted) return;
            try { _buffer.AddSamples(pcmData, offset, count); }
            catch { /* buffer overflow already handled by DiscardOnBufferOverflow */ }
        }

        /// <summary>Stops playback and releases resources.</summary>
        public void Stop()
        {
            if (_waveOut != null)
            {
                try { _waveOut.Stop(); } catch { }
                _waveOut.Dispose();
                _waveOut = null;
            }
            _buffer = null;
        }

        public void Dispose() => Stop();
    }
}
