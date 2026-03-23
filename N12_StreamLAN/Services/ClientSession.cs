using System;
using System.Net;

namespace Server_StreamLAN.Services
{

    public class ClientSession
    {
        public IPEndPoint EndPoint    { get; }
        public DateTime   LastSeen    { get; set; }
        public int        FrameCount  { get; set; }
        public uint       LastSeqNo   { get; set; }
        public int        PacketLostCount { get; set; }

        private DateTime _lastFpsMark = DateTime.UtcNow;
        private int _framesSinceMark;
        public double CurrentFps { get; private set; }

        public ClientSession(IPEndPoint ep)
        {
            EndPoint = ep;
            LastSeen = DateTime.UtcNow;
        }

        public void RecordFrame(uint seqNo)
        {
            if (FrameCount > 0)
            {
                uint expected = LastSeqNo + 1;
                if (seqNo > expected)
                    PacketLostCount += (int)(seqNo - expected);
            }
            LastSeqNo = seqNo;
            LastSeen  = DateTime.UtcNow;
            FrameCount++;

            _framesSinceMark++;
            var elapsed = (DateTime.UtcNow - _lastFpsMark).TotalSeconds;
            if (elapsed >= 1.0)
            {
                CurrentFps = _framesSinceMark / elapsed;
                _framesSinceMark = 0;
                _lastFpsMark = DateTime.UtcNow;
            }
        }

        public double PacketLossPercent =>
            FrameCount + PacketLostCount > 0
                ? PacketLostCount * 100.0 / (FrameCount + PacketLostCount)
                : 0;

        public override string ToString() =>
            $"{EndPoint}  FPS:{CurrentFps:F0}  Loss:{PacketLossPercent:F1}%";
    }
}
