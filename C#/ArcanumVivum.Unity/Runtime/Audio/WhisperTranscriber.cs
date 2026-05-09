using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ArcanumVivum.SpellEngine.Audio
{
    public sealed class WhisperTranscriber
    {
        private readonly HttpClient _httpClient;
        private readonly string _apiKey;

        public WhisperTranscriber(HttpClient httpClient, string apiKey)
        {
            _httpClient = httpClient;
            _apiKey = apiKey;
        }

        public async Task<string?> TranscribeAsync(byte[] audioBytes, string fileName = "audio.wav", CancellationToken cancellationToken = default)
        {
            using var form = new MultipartFormDataContent();
            using var audioContent = new ByteArrayContent(audioBytes);
            audioContent.Headers.ContentType = new MediaTypeHeaderValue("audio/wav");
            form.Add(audioContent, "file", fileName);
            form.Add(new StringContent("whisper-1"), "model");
            form.Add(new StringContent("en"), "language");

            using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/audio/transcriptions")
            {
                Content = form
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

            using var response = await _httpClient.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            using var document = JsonDocument.Parse(json);
            return document.RootElement.TryGetProperty("text", out var textElement)
                ? textElement.GetString()
                : null;
        }

        public static byte[] FloatSamplesToWav(float[] samples, int sampleRate)
        {
            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            const short channels = 1;
            const short bitsPerSample = 16;
            var bytesPerSample = bitsPerSample / 8;
            var byteRate = sampleRate * channels * bytesPerSample;
            var dataSize = samples.Length * bytesPerSample;

            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(36 + dataSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16);
            writer.Write((short)1);
            writer.Write(channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write((short)(channels * bytesPerSample));
            writer.Write(bitsPerSample);
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(dataSize);

            foreach (var sample in samples)
            {
                var clamped = Math.Clamp(sample, -1f, 1f);
                var pcm = (short)(clamped * short.MaxValue);
                writer.Write(pcm);
            }

            writer.Flush();
            return ms.ToArray();
        }
    }
}
