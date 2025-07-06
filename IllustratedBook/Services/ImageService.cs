using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace IllustratedBook.Services
{
    /// <summary>
    /// Service for generating images using Replicate's API
    /// This service takes a prompt and generates an image using the configured AI model
    /// </summary>
    public class ImageService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly string _apiToken;
        private readonly string _model;
        private readonly string _baseUrl = "https://api.replicate.com/v1/predictions";

        /// <summary>
        /// Constructor that initializes the service with configuration and HTTP client
        /// </summary>
        /// <param name="httpClient">HTTP client for making API calls</param>
        /// <param name="configuration">Application configuration containing API settings</param>
        public ImageService(HttpClient httpClient, IConfiguration configuration)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            
            // Get API token and model from configuration
            _apiToken = _configuration["Images:Replicate:API_token"] ?? throw new InvalidOperationException("Replicate API token not found in configuration");
            _model = _configuration["Images:Replicate:Model"] ?? "black-forest-labs/flux-schnell";
            
            // Set up HTTP client headers for Replicate API
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {_apiToken}");
        }

        /// <summary>
        /// Generates an image based on the provided prompt
        /// </summary>
        /// <param name="prompt">The text prompt describing the image to generate</param>
        /// <returns>The URL of the generated image</returns>
        public async Task<string> GenerateImageAsync(string prompt)
        {
            // Validate input
            if (string.IsNullOrWhiteSpace(prompt))
            {
                throw new ArgumentException("Prompt cannot be null or empty", nameof(prompt));
            }

            try
            {
                // Create the request payload for Replicate API
                var requestPayload = new
                {
                    version = "c221b2b8ef527988fb59bf24a8b97c4561f1c671f73bd389f866bfb27c061316",
                    input = new
                    {
                        prompt = prompt,
                        width = 1024,
                        height = 1024,
                        num_inference_steps = 20,
                        guidance_scale = 7.5,
                        negative_prompt = "blurry, low quality, distorted, deformed"
                    }
                };

                // Serialize the request to JSON
                var jsonContent = JsonSerializer.Serialize(requestPayload);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Make the API call to create a prediction
                var response = await _httpClient.PostAsync(_baseUrl, content);
                
                // Check if the request was successful
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Replicate API request failed with status {response.StatusCode}: {errorContent}");
                }

                // Read and parse the response
                var responseContent = await response.Content.ReadAsStringAsync();
                var predictionResponse = JsonSerializer.Deserialize<ReplicatePredictionResponse>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (predictionResponse?.Id == null)
                {
                    throw new InvalidOperationException("No prediction ID received from Replicate API");
                }

                // Poll for the result
                var imageUrl = await PollForResultAsync(predictionResponse.Id);
                return imageUrl;
            }
            catch (HttpRequestException ex)
            {
                // Log the error (in a real application, you'd use a proper logging framework)
                Console.WriteLine($"HTTP request error: {ex.Message}");
                throw new InvalidOperationException("Failed to communicate with Replicate API", ex);
            }
            catch (JsonException ex)
            {
                // Log the error
                Console.WriteLine($"JSON parsing error: {ex.Message}");
                throw new InvalidOperationException("Failed to parse Replicate API response", ex);
            }
            catch (Exception ex)
            {
                // Log any other errors
                Console.WriteLine($"Unexpected error: {ex.Message}");
                throw new InvalidOperationException("An unexpected error occurred while generating the image", ex);
            }
        }

        /// <summary>
        /// Polls the Replicate API for the result of an image generation request
        /// </summary>
        /// <param name="predictionId">The ID of the prediction to check</param>
        /// <returns>The URL of the generated image</returns>
        private async Task<string> PollForResultAsync(string predictionId)
        {
            var maxAttempts = 30; // Maximum number of polling attempts
            var delayMs = 2000; // Wait 2 seconds between attempts

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                // Wait before making the request (except for the first attempt)
                if (attempt > 0)
                {
                    await Task.Delay(delayMs);
                }

                // Make the request to check the prediction status
                var response = await _httpClient.GetAsync($"{_baseUrl}/{predictionId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new HttpRequestException($"Failed to check prediction status: {errorContent}");
                }

                // Read and parse the response
                var responseContent = await response.Content.ReadAsStringAsync();
                var prediction = JsonSerializer.Deserialize<ReplicatePrediction>(responseContent, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (prediction == null)
                {
                    throw new InvalidOperationException("Failed to parse prediction response");
                }

                // Check the status of the prediction
                switch (prediction.Status)
                {
                    case "succeeded":
                        // Extract the image URL from the output
                        if (prediction.Output != null && prediction.Output.Length > 0)
                        {
                            return prediction.Output[0];
                        }
                        throw new InvalidOperationException("No image URL found in successful prediction");
                    
                    case "failed":
                        throw new InvalidOperationException($"Image generation failed: {prediction.Error}");
                    
                    case "canceled":
                        throw new InvalidOperationException("Image generation was canceled");
                    
                    case "processing":
                    case "starting":
                        // Continue polling
                        continue;
                    
                    default:
                        throw new InvalidOperationException($"Unknown prediction status: {prediction.Status}");
                }
            }

            throw new TimeoutException("Image generation timed out after maximum attempts");
        }

        /// <summary>
        /// Test method to verify the service is working correctly
        /// </summary>
        /// <returns>True if the service can connect to Replicate successfully</returns>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                // Use a simple test prompt
                var testResult = await GenerateImageAsync("A simple red circle on white background");
                return !string.IsNullOrWhiteSpace(testResult);
            }
            catch
            {
                return false;
            }
        }
    }

    /// <summary>
    /// Data classes for deserializing Replicate API responses
    /// These classes match the structure of Replicate's JSON response
    /// </summary>
    public class ReplicatePredictionResponse
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
    }

    public class ReplicatePrediction
    {
        public string Id { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string[] Output { get; set; } = Array.Empty<string>();
        public string Error { get; set; } = string.Empty;
    }
} 