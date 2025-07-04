using Microsoft.AspNetCore.Mvc;

namespace IllustratedBook.Services
{
    /// <summary>
    /// Example class showing different ways to use the ChatService
    /// This class demonstrates integration patterns for the chat service
    /// </summary>
    public class ChatServiceExamples
    {
        private readonly ChatService _chatService;

        /// <summary>
        /// Constructor that injects the ChatService
        /// </summary>
        /// <param name="chatService">The chat service for generating prompts</param>
        public ChatServiceExamples(ChatService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// Example 1: Generate a prompt from book content
        /// This shows how to use the service with book text
        /// </summary>
        /// <param name="bookContent">The content from a book or chapter</param>
        /// <returns>The generated image prompt</returns>
        public async Task<string> GeneratePromptFromBookContentAsync(string bookContent)
        {
            try
            {
                // Take the first 200 characters to keep it manageable
                var truncatedContent = bookContent.Length > 200 
                    ? bookContent.Substring(0, 200) + "..." 
                    : bookContent;

                // Generate the prompt
                var prompt = await _chatService.GenerateFluxPromptAsync(truncatedContent);
                return prompt;
            }
            catch (Exception ex)
            {
                // Log the error and return a fallback prompt
                Console.WriteLine($"Error generating prompt from book content: {ex.Message}");
                return "A beautiful, photorealistic illustration with high dynamic range and vibrant colors, captured from an unusual angle using professional photography equipment.";
            }
        }

        /// <summary>
        /// Example 2: Generate multiple prompts for different scenes
        /// This shows how to batch process multiple text inputs
        /// </summary>
        /// <param name="sceneDescriptions">List of scene descriptions</param>
        /// <returns>List of generated prompts</returns>
        public async Task<List<string>> GenerateMultiplePromptsAsync(List<string> sceneDescriptions)
        {
            var prompts = new List<string>();

            foreach (var description in sceneDescriptions)
            {
                try
                {
                    var prompt = await _chatService.GenerateFluxPromptAsync(description);
                    prompts.Add(prompt);
                }
                catch (Exception ex)
                {
                    // Log the error and add a fallback prompt
                    Console.WriteLine($"Error generating prompt for scene '{description}': {ex.Message}");
                    prompts.Add($"A photorealistic scene with high dynamic range and vibrant colors, captured from an unusual angle using a Hasselblad H6D-400c medium format camera with a Carl Zeiss Planar 80mm f/2.8 lens.");
                }
            }

            return prompts;
        }

        /// <summary>
        /// Example 3: Generate a prompt with error handling and retry logic
        /// This shows a more robust approach to using the service
        /// </summary>
        /// <param name="text">The input text</param>
        /// <param name="maxRetries">Maximum number of retry attempts</param>
        /// <returns>The generated prompt or a fallback</returns>
        public async Task<string> GeneratePromptWithRetryAsync(string text, int maxRetries = 3)
        {
            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    var prompt = await _chatService.GenerateFluxPromptAsync(text);
                    return prompt;
                }
                catch (HttpRequestException ex)
                {
                    // Network-related errors - retry
                    Console.WriteLine($"Attempt {attempt} failed with HTTP error: {ex.Message}");
                    
                    if (attempt == maxRetries)
                    {
                        // Last attempt failed, return fallback
                        return GenerateFallbackPrompt(text);
                    }
                    
                    // Wait before retrying (exponential backoff)
                    await Task.Delay(1000 * attempt);
                }
                catch (Exception ex)
                {
                    // Other errors - don't retry
                    Console.WriteLine($"Non-retryable error: {ex.Message}");
                    return GenerateFallbackPrompt(text);
                }
            }

            return GenerateFallbackPrompt(text);
        }

        /// <summary>
        /// Example 4: Generate a prompt for a specific book chapter
        /// This shows integration with your existing book structure
        /// </summary>
        /// <param name="chapterTitle">The chapter title</param>
        /// <param name="chapterContent">The chapter content</param>
        /// <returns>The generated image prompt</returns>
        public async Task<string> GenerateChapterImagePromptAsync(string chapterTitle, string chapterContent)
        {
            try
            {
                // Combine title and content for better context
                var combinedText = $"Chapter: {chapterTitle}. {chapterContent}";
                
                // Limit the length to avoid API token limits
                var limitedText = combinedText.Length > 300 
                    ? combinedText.Substring(0, 300) + "..." 
                    : combinedText;

                var prompt = await _chatService.GenerateFluxPromptAsync(limitedText);
                return prompt;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error generating chapter prompt: {ex.Message}");
                return $"A photorealistic illustration for the chapter '{chapterTitle}', featuring high dynamic range and vibrant colors, captured from an unusual angle using professional photography equipment.";
            }
        }

        /// <summary>
        /// Helper method to generate a fallback prompt when the service fails
        /// </summary>
        /// <param name="originalText">The original text that failed to process</param>
        /// <returns>A basic fallback prompt</returns>
        private string GenerateFallbackPrompt(string originalText)
        {
            // Create a simple fallback prompt based on the original text
            var keywords = ExtractKeywords(originalText);
            return $"A photorealistic image featuring {keywords}, with high dynamic range and vibrant colors, captured from an unusual angle using a Hasselblad H6D-400c medium format camera with a Carl Zeiss Planar 80mm f/2.8 lens.";
        }

        /// <summary>
        /// Helper method to extract keywords from text for fallback prompts
        /// </summary>
        /// <param name="text">The text to extract keywords from</param>
        /// <returns>Simple keywords for the fallback prompt</returns>
        private string ExtractKeywords(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "a beautiful scene";

            // Simple keyword extraction - in a real application, you might use more sophisticated NLP
            var words = text.Split(' ', StringSplitOptions.RemoveEmptyEntries)
                           .Where(word => word.Length > 3) // Filter out short words
                           .Take(5) // Take first 5 words
                           .ToArray();

            return words.Length > 0 ? string.Join(", ", words) : "a beautiful scene";
        }
    }
} 