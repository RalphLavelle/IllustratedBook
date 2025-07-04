using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using IllustratedBook.Services;

namespace IllustratedBook.Pages
{
    /// <summary>
    /// Page model for testing the ChatService functionality
    /// This page allows users to test the OpenAI connection and generate prompts
    /// </summary>
    public class ChatTestModel : PageModel
    {
        private readonly ChatService _chatService;

        // Properties for the page model
        [BindProperty]
        public string InputText { get; set; } = string.Empty;

        public string GeneratedPrompt { get; set; } = string.Empty;
        public string ErrorMessage { get; set; } = string.Empty;
        public bool? ConnectionTestResult { get; set; }

        /// <summary>
        /// Constructor that injects the ChatService dependency
        /// </summary>
        /// <param name="chatService">The chat service for generating prompts</param>
        public ChatTestModel(ChatService chatService)
        {
            _chatService = chatService;
        }

        /// <summary>
        /// Handles GET requests to the page
        /// </summary>
        public void OnGet()
        {
            // Page is ready to accept input
        }

        /// <summary>
        /// Handles POST requests to test the OpenAI connection
        /// </summary>
        public async Task<IActionResult> OnPostTestConnectionAsync()
        {
            try
            {
                // Test the connection to OpenAI
                ConnectionTestResult = await _chatService.TestConnectionAsync();
                
                // Return to the same page to show the result
                return Page();
            }
            catch (Exception ex)
            {
                // Handle any errors that occur during the test
                ConnectionTestResult = false;
                ErrorMessage = $"Connection test failed: {ex.Message}";
                return Page();
            }
        }

        /// <summary>
        /// Handles POST requests to generate a prompt
        /// </summary>
        public async Task<IActionResult> OnPostGeneratePromptAsync()
        {
            // Validate the input
            if (string.IsNullOrWhiteSpace(InputText))
            {
                ErrorMessage = "Please enter some text to generate a prompt.";
                return Page();
            }

            try
            {
                // Generate the prompt using the ChatService
                GeneratedPrompt = await _chatService.GenerateFluxPromptAsync(InputText);
                
                // Clear any previous error messages
                ErrorMessage = string.Empty;
                
                // Return to the same page to show the generated prompt
                return Page();
            }
            catch (ArgumentException ex)
            {
                // Handle validation errors
                ErrorMessage = ex.Message;
                return Page();
            }
            catch (InvalidOperationException ex)
            {
                // Handle service errors
                ErrorMessage = $"Service error: {ex.Message}";
                return Page();
            }
            catch (Exception ex)
            {
                // Handle any other unexpected errors
                ErrorMessage = $"An unexpected error occurred: {ex.Message}";
                return Page();
            }
        }
    }
} 