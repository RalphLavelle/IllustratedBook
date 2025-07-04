using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace IllustratedBook.Services
{
    /// <summary>
    /// Service for generating AI-powered prompts using OpenAI's API
    /// This service takes text input and generates prompts for image generation models
    /// </summary>
    public class ChatService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly string _baseUrl = "https://api.openai.com/v1/chat/completions";

        /// <summary>
        /// Constructor that initializes the service with configuration and HTTP client
        /// </summary>
        /// <param name="httpClient">HTTP client for making API calls</param>
        /// <param name="configuration">Application configuration containing API settings</param>
        public ChatService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            
            // Get API key and model from configuration
            _apiKey = _configuration["Images:OpenAI:API_KEY"] ?? throw new InvalidOperationException("OpenAI API key not found in configuration");
            _model = _configuration["Images:OpenAI:Model"] ?? "gpt-4o-mini";
            
            // Set up HTTP client headers for OpenAI API
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
            _httpClient.DefaultRequestHeaders.Add("Content-Type", "application/json");
        }

        /// <summary>
        /// Generates a prompt for the Flux Dev model based on the provided text
        /// </summary>
        /// <param name="text">The input text to base the image prompt on</param>
        /// <returns>The generated prompt for the Flux Dev model</returns>
        public async Task<string> GenerateFluxPromptAsync(string text)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("Text input cannot be null or empty", nameof(text));
            }

            try
            {
                // Create the system prompt that will guide the AI
                var systemPrompt = "You are an expert at creating detailed, artistic prompts for image generation. Your responses should be creative, descriptive, and optimized for AI image generation models.";

                // Create the user prompt with the specific requirements
                var userPrompt = $@"Using this text - ""{text}"", generate a prompt for the Flux Dev model. Ask it to make the image photorealistic, taken from an odd angle, with a high dynamic range, very colourful. The photo should be taken with a Hasselblad H6D-400c medium format camera, using a Carl Zeiss Planar 80mm f/2.8 lens.";

                // Create the request payload for OpenAI API
                var requestPayload = new
                {
                    model = _model,
                    messages = new[]
                    {
                        new { role = "system", content = systemPrompt },
                        new { role = "user", content = userPrompt }
                    },
                    max_tokens = 500, // Limit response length
                    temperature = 0.7 // Add some creativity while keeping it focused
                };

                // Serialize the request to JSON
                var jsonContent = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Make the API call to OpenAI
                var response = await _httpClient.PostAsync(_baseUrl, content);
                
                // Check if the request was successful
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"OpenAI API request failed with status {response.StatusCode}: {errorContent}");
                }

                // Read and parse the response
                var responseContent = await response.Content.ReadAsStringAsync();
                var responseObject = JsonSerializer.Deserialize<OpenAIResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Extract the generated prompt from the response
                var generatedPrompt = responseObject?.Choices?.FirstOrDefault()?.Message?.Content;
                
                if (string.IsNullOrWhiteSpace(generatedPrompt))
                {
                    throw new InvalidOperationException("No content received from OpenAI API");
                }

                return generatedPrompt.Trim();
            }
            catch (HttpRequestException ex)
            {
                // Log the error (in a real application, you'd use a proper logging framework)
                Console.WriteLine($"HTTP request error: {ex.Message}");
                throw new InvalidOperationException("Failed to communicate with OpenAI API", ex);
            }
            catch (JsonException ex)
            {
                // Log the error
                Console.WriteLine($"JSON parsing error: {ex.Message}");
                throw new InvalidOperationException("Failed to parse OpenAI API response", ex);
            }
            catch (Exception ex)
            {
                // Log any other errors
                Console.WriteLine($"Unexpected error: {ex.Message}");
                throw new InvalidOperationException("An unexpected error occurred while generating the prompt", ex);
            }
        }

        /// <summary>
        /// Test method to verify the service is working correctly
        /// </summary>
        /// <returns>True if the service can connect to OpenAI successfully</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                // Use a simple test prompt
                var testResult = await GenerateFluxPromptAsync("A beautiful sunset over mountains");
                return !string.IsNullOrWhiteSpace(testResult);
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Data classes for deserializing OpenAI API responses
    /// These classes match the structure of OpenAI's JSON response
    /// </summary>
    public class OpenAIResponse
    {
        public List<Choice> Choices { get; set; } = new();
    }

    public class Choice
    {
        public Message Message { get; set; } = new();
    }

    public class Message
    {
        public string Content { get; set; } = string.Empty;
    }
} 