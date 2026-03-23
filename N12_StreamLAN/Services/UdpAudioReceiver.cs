using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Server_StreamLAN.Services
{

    public class UdpAudioReceiver : IDisposable
    {
        private readonly UdpClient _udp;
        private readonly AudioPlaybackService _playback;
        private CancellationTokenSource? _cts;

        public const int AudioPort = 9002;

        public UdpAudioReceiver(AudioPlaybackService playback)
        {
            _playback = playback;
            _udp      = new UdpClient(AudioPort);
        }

        public void Start()
        {
            _cts?.Cancel();
            _cts = new CancellationTokenSource();
            var ct = _cts.Token;

            Task.Run(async () =>
            {
                while (!ct.IsCancellationRequested)
                {
                    try
                    {
                        var result = await _udp.ReceiveAsync(ct);

                        if (PacketProtocol.Unpack(result.Buffer, out _, out byte flags, out byte[] pcm)
                            && (flags & PacketProtocol.FlagAudio) != 0)
                        {
                            _playback.AddSamples(pcm, 0, pcm.Length);
                        }
                    }
                    catch (OperationCanceledException) { break; }
                    catch {}
                }
            }, ct);
        }

        public void Dispose()
        {
            _cts?.Cancel();
            try { _udp.Close(); } catch { }
            _udp.Dispose();
        }
    }
}
