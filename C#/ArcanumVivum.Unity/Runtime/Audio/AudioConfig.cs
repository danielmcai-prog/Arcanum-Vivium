namespace ArcanumVivum.SpellEngine.Audio
{
    public sealed class AudioConfig
    {
        public string WakeWord { get; set; } = "arcanum";
        public float SilenceThresholdDb { get; set; } = -50f;
        public int SilenceDurationMs { get; set; } = 2000;
        public int SampleRate { get; set; } = 16000;
        public int ChunkDurationMs { get; set; } = 500;
    }
}
