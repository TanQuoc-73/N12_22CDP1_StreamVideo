namespace Client_StreamLAN.Services
{

    public class AdaptiveBitrateController
    {
        private const int MinQ  = 15;
        private const int MaxQ  = 85;
        private const int Step  = 5;
        private const int StabilizeFrames = 30; 

        public int Quality { get; private set; } = 50;

        private long _goodStreak;

       
        /// <param name="packetBytes">Total UDP packet size in bytes (header + JPEG).</param>
        /// <param name="sendMs">Elapsed milliseconds for the send call.</param>
        public void Feedback(long packetBytes, long sendMs)
        {
            bool tooLarge = packetBytes > 52_000;   
            bool tooSlow  = sendMs > 50;             

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
                _goodStreak = 0; 
            }
        }

        public void Reset()
        {
            Quality = 50;
            _goodStreak = 0;
        }
    }
}
