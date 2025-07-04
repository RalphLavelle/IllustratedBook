# ChatService Documentation

## Overview

The `ChatService` is a service that connects to OpenAI's API to generate detailed prompts for the Flux Dev image generation model. It takes text input and creates optimized prompts that specify photorealistic style, unusual camera angles, high dynamic range, and vibrant colors.

## Features

- **OpenAI Integration**: Connects to OpenAI's GPT models using your API key
- **Prompt Generation**: Creates detailed prompts for the Flux Dev model
- **Error Handling**: Comprehensive error handling with fallback options
- **Configuration**: Uses settings from `appsettings.json`
- **Testing**: Includes connection testing functionality

## Configuration

The service uses the following configuration from `appsettings.json`:

```json
{
  "Images": {
    "OpenAI": {
      "API_KEY": "your-openai-api-key-here",
      "Model": "gpt-4o-mini"
    }
  }
}
```

## Basic Usage

### 1. Inject the Service

```csharp
public class MyController : Controller
{
    private readonly ChatService _chatService;

    public MyController(ChatService chatService)
    {
        _chatService = chatService;
    }
}
```

### 2. Generate a Prompt

```csharp
public async Task<IActionResult> GenerateImagePrompt(string text)
{
    try
    {
        var prompt = await _chatService.GenerateFluxPromptAsync(text);
        return Ok(prompt);
    }
    catch (Exception ex)
    {
        return BadRequest($"Error generating prompt: {ex.Message}");
    }
}
```

### 3. Test the Connection

```csharp
public async Task<IActionResult> TestConnection()
{
    var isConnected = await _chatService.TestConnectionAsync();
    return Ok(new { Connected = isConnected });
}
```

## Advanced Usage Examples

### Example 1: Generate Prompt from Book Content

```csharp
public async Task<string> GenerateBookIllustration(string bookContent)
{
    var examples = new ChatServiceExamples(_chatService);
    return await examples.GeneratePromptFromBookContentAsync(bookContent);
}
```

### Example 2: Batch Process Multiple Scenes

```csharp
public async Task<List<string>> GenerateChapterPrompts(List<string> scenes)
{
    var examples = new ChatServiceExamples(_chatService);
    return await examples.GenerateMultiplePromptsAsync(scenes);
}
```

### Example 3: Retry Logic with Fallback

```csharp
public async Task<string> GenerateRobustPrompt(string text)
{
    var examples = new ChatServiceExamples(_chatService);
    return await examples.GeneratePromptWithRetryAsync(text, maxRetries: 3);
}
```

## Error Handling

The service includes comprehensive error handling:

- **Validation Errors**: Throws `ArgumentException` for invalid input
- **API Errors**: Throws `HttpRequestException` for network/API issues
- **Parsing Errors**: Throws `JsonException` for response parsing issues
- **Fallback Prompts**: Provides default prompts when the service fails

## Testing

### Test Page

Visit `/ChatTest` to test the service functionality:
- Test OpenAI connection
- Generate prompts from text input
- Copy generated prompts to clipboard

### Programmatic Testing

```csharp
// Test connection
var isConnected = await _chatService.TestConnectionAsync();

// Test prompt generation
var prompt = await _chatService.GenerateFluxPromptAsync("A magical forest");
```

## Integration with Existing Code

### In Controllers

```csharp
[HttpPost]
public async Task<IActionResult> CreateBookIllustration(int bookId, string content)
{
    var prompt = await _chatService.GenerateFluxPromptAsync(content);
    
    // Use the prompt with your image generation service
    var imageUrl = await _imageService.GenerateImage(prompt);
    
    return Ok(new { Prompt = prompt, ImageUrl = imageUrl });
}
```

### In Razor Pages

```csharp
public class BookPageModel : PageModel
{
    private readonly ChatService _chatService;

    public BookPageModel(ChatService chatService)
    {
        _chatService = chatService;
    }

    public async Task<IActionResult> OnPostGenerateIllustrationAsync()
    {
        var prompt = await _chatService.GenerateFluxPromptAsync(BookContent);
        // Process the prompt...
        return Page();
    }
}
```

## Best Practices

1. **Input Validation**: Always validate input before sending to the service
2. **Error Handling**: Use try-catch blocks and provide fallback options
3. **Rate Limiting**: Be mindful of OpenAI API rate limits
4. **Token Limits**: Keep input text reasonable (under 300 characters for best results)
5. **Caching**: Consider caching generated prompts for repeated content

## Troubleshooting

### Common Issues

1. **API Key Not Found**: Ensure your OpenAI API key is correctly set in `appsettings.json`
2. **Network Errors**: Check your internet connection and firewall settings
3. **Rate Limiting**: If you hit rate limits, implement exponential backoff
4. **Invalid Responses**: The service includes fallback prompts for failed requests

### Debug Information

Enable detailed logging by adding to `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "IllustratedBook.Services": "Debug"
    }
  }
}
```

## Security Considerations

- Never expose your API key in client-side code
- Use environment variables for production API keys
- Implement proper authentication for service endpoints
- Monitor API usage to prevent abuse

## Performance Tips

- Use async/await for all service calls
- Implement caching for frequently used prompts
- Consider batching multiple requests
- Monitor response times and implement timeouts 