using System;

namespace ArcanumVivum.SpellEngine.Audio
{
    public sealed class WakeWordDetector
    {
        private readonly string _wakeWord;
        private string _buffer = string.Empty;

        public WakeWordDetector(string wakeWord = "arcanum")
        {
            _wakeWord = wakeWord.ToLowerInvariant();
        }

        public bool ProcessText(string text)
        {
            _buffer = (_buffer + text).ToLowerInvariant();
            if (_buffer.Length > 100)
            {
                _buffer = _buffer.Substring(_buffer.Length - 100, 100);
            }

            return _buffer.Contains(_wakeWord, StringComparison.Ordinal);
        }

        public void Reset()
        {
            _buffer = string.Empty;
        }
    }
}
