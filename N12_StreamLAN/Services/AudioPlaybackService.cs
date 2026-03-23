using NAudio.Wave;
using System;

namespace Server_StreamLAN.Services
{

    public class AudioPlaybackService : IDisposable
    {
        private WaveOutEvent?        _waveOut;
        private BufferedWaveProvider? _buffer;
        private volatile bool        _muted;

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

        public void Start()
        {
            Stop();

            _buffer = new BufferedWaveProvider(AudioFormat)
            {
                BufferDuration        = TimeSpan.FromSeconds(2),
                DiscardOnBufferOverflow = true 
            };

            _waveOut = new WaveOutEvent
            {
                DesiredLatency = 100 
            };
            _waveOut.Init(_buffer);
            _waveOut.Volume = _muted ? 0f : _volume;
            _waveOut.Play();
        }

        public void AddSamples(byte[] pcmData, int offset, int count)
        {
            if (_buffer == null || _muted) return;
            try { _buffer.AddSamples(pcmData, offset, count); }
            catch { }
        }

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
