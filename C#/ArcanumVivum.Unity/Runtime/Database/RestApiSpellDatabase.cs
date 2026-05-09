using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ArcanumVivum.SpellEngine.Models;

namespace ArcanumVivum.SpellEngine.Database
{
    public sealed class RestApiSpellDatabase : ISpellDatabase
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false
        };

        private readonly HttpClient _httpClient;
        private readonly string _baseUrl;

        public RestApiSpellDatabase(HttpClient httpClient, string baseUrl)
        {
            _httpClient = httpClient;
            _baseUrl = baseUrl.TrimEnd('/');
        }

        public async Task<IReadOnlyList<SpellSearchHit>> SearchAsync(IReadOnlyList<float> embedding, int topK = 3, CancellationToken cancellationToken = default)
        {
            var payload = JsonSerializer.Serialize(new { embedding, topK }, JsonOptions);
            using var response = await _httpClient.PostAsync(
                $"{_baseUrl}/search",
                new StringContent(payload, Encoding.UTF8, "application/json"),
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<SpellSearchHit>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<SpellSearchHit>>(json, JsonOptions) ?? new List<SpellSearchHit>();
        }

        public async Task UpsertAsync(SpellRecord spell, CancellationToken cancellationToken = default)
        {
            var payload = JsonSerializer.Serialize(spell, JsonOptions);
            using var response = await _httpClient.PostAsync(
                _baseUrl,
                new StringContent(payload, Encoding.UTF8, "application/json"),
                cancellationToken);

            response.EnsureSuccessStatusCode();
        }

        public async Task<SpellRecord?> GetAsync(string id, CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.GetAsync($"{_baseUrl}/{id}", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<SpellRecord>(json, JsonOptions);
        }

        public async Task<IReadOnlyList<SpellRecord>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.GetAsync(_baseUrl, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return Array.Empty<SpellRecord>();
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            return JsonSerializer.Deserialize<List<SpellRecord>>(json, JsonOptions) ?? new List<SpellRecord>();
        }

        public async Task<int> SizeAsync(CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.GetAsync($"{_baseUrl}/count", cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                return 0;
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var wrapper = JsonSerializer.Deserialize<CountResponse>(json, JsonOptions);
            return wrapper?.Count ?? 0;
        }

        private sealed class CountResponse
        {
            public int Count { get; set; }
        }
    }
}
