using System;
using System.Collections.Generic;
using UnityEngine;

namespace ArcanumVivum.SpellEngine.Integration
{
    public sealed class UnityMicrophoneRecorder : MonoBehaviour
    {
        [SerializeField] private int sampleRate = 16000;
        [SerializeField] private int clipLengthSeconds = 10;
        [SerializeField] private float silenceThresholdDb = -50f;
        [SerializeField] private int silenceDurationMs = 2000;

        private AudioClip? _clip;
        private string? _device;
        private bool _isRecording;
        private float _silenceElapsedMs;
        private readonly List<float> _capturedSamples = new();

        public event Action<float>? OnAudioLevel;
        public event Action<float[]>? OnSilenceDetected;

        public bool IsRecording => _isRecording;

        public void StartRecording()
        {
            if (_isRecording)
            {
                return;
            }

            if (Microphone.devices.Length == 0)
            {
                throw new InvalidOperationException("No microphone device found.");
            }

            _device = Microphone.devices[0];
            _capturedSamples.Clear();
            _silenceElapsedMs = 0;
            _clip = Microphone.Start(_device, true, clipLengthSeconds, sampleRate);
            _isRecording = true;
        }

        public float[] StopRecording()
        {
            if (!_isRecording || _device == null)
            {
                return Array.Empty<float>();
            }

            _isRecording = false;
            Microphone.End(_device);
            return _capturedSamples.ToArray();
        }

        private void Update()
        {
            if (!_isRecording || _clip == null)
            {
                return;
            }

            var sampleWindow = new float[1024];
            var micPos = Microphone.GetPosition(_device);
            if (micPos < sampleWindow.Length)
            {
                return;
            }

            _clip.GetData(sampleWindow, micPos - sampleWindow.Length);
            _capturedSamples.AddRange(sampleWindow);

            var rms = 0f;
            foreach (var sample in sampleWindow)
            {
                rms += sample * sample;
            }
            rms = Mathf.Sqrt(rms / sampleWindow.Length);
            var db = 20f * Mathf.Log10(Mathf.Max(rms, 0.001f));

            OnAudioLevel?.Invoke(db);

            if (db < silenceThresholdDb)
            {
                _silenceElapsedMs += Time.deltaTime * 1000f;
                if (_silenceElapsedMs >= silenceDurationMs)
                {
                    var data = StopRecording();
                    OnSilenceDetected?.Invoke(data);
                    _silenceElapsedMs = 0f;
                }
            }
            else
            {
                _silenceElapsedMs = 0f;
            }
        }
    }
}
