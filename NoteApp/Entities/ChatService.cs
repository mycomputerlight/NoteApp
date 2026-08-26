using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace NoteApp.Entities
{
    public class ChatService
    {
        private static readonly HttpClient _httpClient = new HttpClient
        {
            BaseAddress = new Uri("https://integrate.api.nvidia.com/"),
              Timeout = TimeSpan.FromMinutes(5)
        };

        private readonly string _apiKey;
        private const string Model = "google/diffusiongemma-26b-a4b-it";

        private readonly IConfiguration _configuration;

        public ChatService(IConfiguration configuration)
        {
            _configuration = configuration;
            _apiKey = _configuration["NvidiaApi:Key"]
                ?? throw new InvalidOperationException("NVIDIA API key bulunamadı. User Secrets'ı kontrol et.");
        }

        public async Task<string> SendMessageAsync(string userMessage, bool stream = false)
        {
            var requestBody = new ChatRequest
            {
                Model = Model,
                Messages = new[]
                {
                    new ChatMessage { Role = "user", Content = userMessage }
                },
                ChatTemplateKwargs = new ChatTemplateKwargs { EnableThinking = false },
                MaxTokens = 4096,
                Stream = stream,
                Temperature = 1.0,
                TopP = 0.95
            };

            var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });

            using var request = new HttpRequestMessage(HttpMethod.Post, "v1/chat/completions")
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json")
            };

            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue(
                stream ? "text/event-stream" : "application/json"));


            Console.WriteLine(">>> NVIDIA API'ye istek gönderiliyor...");

            using var response = await _httpClient.SendAsync(request);

            Console.WriteLine(">>> NVIDIA API'den cevap geldi!");
            Console.WriteLine($"Status Code: {(int)response.StatusCode}");

            var responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new HttpRequestException(
                    $"NVIDIA API hatası ({(int)response.StatusCode}): {responseBody}");
            }

            var parsed = JsonSerializer.Deserialize<ChatResponse>(responseBody);
            return parsed?.Choices?[0]?.Message?.Content ?? string.Empty;
        }
    }

    // ---- DTO'lar ----

    public class ChatRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;

        [JsonPropertyName("messages")]
        public ChatMessage[] Messages { get; set; } = Array.Empty<ChatMessage>();

        [JsonPropertyName("chat_template_kwargs")]
        public ChatTemplateKwargs? ChatTemplateKwargs { get; set; }

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; } = 4096;

        [JsonPropertyName("stream")]
        public bool Stream { get; set; }

        [JsonPropertyName("temperature")]
        public double Temperature { get; set; } = 1.0;

        [JsonPropertyName("top_p")]
        public double TopP { get; set; } = 0.95;
    }

    public class ChatTemplateKwargs
    {
        [JsonPropertyName("enable_thinking")]
        public bool EnableThinking { get; set; }
    }

    public class ChatMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; } = string.Empty;

        [JsonPropertyName("content")]
        public string Content { get; set; } = string.Empty;
    }

    public class ChatResponse
    {
        [JsonPropertyName("choices")]
        public Choice[]? Choices { get; set; }
    }

    public class Choice
    {
        [JsonPropertyName("message")]
        public ChatMessage? Message { get; set; }
    }
}

