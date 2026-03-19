namespace Client_StreamLAN.Services
{
    /// <summary>
    /// Adjusts JPEG quality automatically based on packet size and send latency.
    /// Quality range: MinQ (low bandwidth) → MaxQ (high bandwidth).
    /// </summary>
    public class AdaptiveBitrateController
    {
        private const int MinQ  = 15;
        private const int MaxQ  = 85;
        private const int Step  = 5;
        private const int StabilizeFrames = 30; // consecutive good frames before upgrade

        public int Quality { get; private set; } = 50;

        private long _goodStreak;

        /// <summary>
        /// Call after every successful send. Adjusts Quality for the next frame.
        /// </summary>
        /// <param name="packetBytes">Total UDP packet size in bytes (header + JPEG).</param>
        /// <param name="sendMs">Elapsed milliseconds for the send call.</param>
        public void Feedback(long packetBytes, long sendMs)
        {
            bool tooLarge = packetBytes > 52_000;   // 52 KB — keeps headroom under 65 KB UDP limit
            bool tooSlow  = sendMs > 50;             // >50 ms send lag

            if (tooLarge || tooSlow)
            {
                Quality = System.Math.Max(MinQ, Quality - Step);
                _goodStreak = 0;
            }
            else if (sendMs < 20 && packetBytes < 32_000)
            {
                _goodStreak++;
                if (_goodStreak >= StabilizeFrames)
                {
                    Quality = System.Math.Min(MaxQ, Quality + Step);
                    _goodStreak = 0;
                }
            }
            else
            {
                _goodStreak = 0; // neutral — don't change quality
            }
        }

        public void Reset()
        {
            Quality = 50;
            _goodStreak = 0;
        }
    }
}
