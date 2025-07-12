# Session Storage Implementation

## Overview

The session storage functionality has been implemented to cache page data and avoid unnecessary calls to the book service. This improves performance by storing frequently accessed data in both server-side and client-side session storage.

## How It Works

### Server-Side Session Storage

1. **Storage**: When a page is loaded, the current page data is automatically saved to the server's session storage using a unique key format: `chapter_{bookId}_{chapterId}_page_{pageId}`

2. **Retrieval**: Before making a call to the book service, the application first checks if the requested page data exists in session storage.

3. **Fallback**: If the data is not found in session storage, it falls back to the original method of retrieving data from JSON files or the database.

### Client-Side Session Storage

1. **Browser Storage**: Page data is also cached in the browser's session storage for faster client-side access.

2. **Expiration**: Client-side cached data expires after 30 minutes to ensure data freshness.

3. **Automatic Cleanup**: Expired data is automatically removed from session storage.

## Key Features

### Performance Benefits

- **Reduced Server Load**: Avoids repeated calls to the book service for the same page data
- **Faster Page Loading**: Cached data loads much faster than fresh server requests
- **Bandwidth Savings**: Reduces data transfer between client and server

### Data Management

- **Automatic Caching**: Page data is automatically saved when pages are loaded
- **Smart Retrieval**: Checks session storage first before making server requests
- **Error Handling**: Graceful fallback if session storage operations fail

### Configuration

The session storage is configured in `Program.cs`:

```csharp
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});
```

## Implementation Details

### Server-Side Methods

- `GetSessionStorageKey()`: Generates unique keys for session storage
- `SavePageToSessionStorage()`: Saves page data to server session
- `GetPageFromSessionStorage()`: Retrieves page data from server session
- `GetChapterFromSessionStorage()`: Retrieves chapter data from server session

### Client-Side Methods

- `getSessionStorageKey()`: Generates unique keys for browser session storage
- `savePageToSessionStorage()`: Saves page data to browser session storage
- `getPageFromSessionStorage()`: Retrieves page data from browser session storage
- `checkPageInSessionStorage()`: Checks both client and server session storage
- `clearSessionStorageForChapter()`: Clears all session data for a chapter

## Usage Examples

### Checking Session Storage Before Page Load

```javascript
// Check if page data exists in session storage
window.imageLoader.checkPageInSessionStorage(bookId, chapterId, pageId, (found, data) => {
    if (found) {
        // Use cached data
        displayPageContent(data);
    } else {
        // Load from server
        window.location.href = `/books/${bookId}/chapters/${chapterId}/pages/${pageId}`;
    }
});
```

### Saving Page Data to Session Storage

```javascript
// Save current page data
window.imageLoader.savePageToSessionStorage(bookId, chapterId, pageId, pageData);
```

## Best Practices

1. **Always Check Session Storage First**: Before making server requests, check if data exists in session storage
2. **Handle Errors Gracefully**: Session storage operations should not break the application flow
3. **Set Appropriate Timeouts**: Data should expire to ensure freshness
4. **Log Operations**: Log session storage operations for debugging purposes

## Troubleshooting

### Common Issues

1. **Session Data Not Found**: Check if the session key format matches between save and retrieve operations
2. **Expired Data**: Ensure the expiration time is appropriate for your use case
3. **Browser Compatibility**: Session storage is supported in all modern browsers

### Debugging

- Check browser console for session storage logs
- Verify session storage keys are consistent
- Monitor server logs for session storage errors

## Future Enhancements

1. **Compression**: Implement data compression for large page content
2. **Selective Caching**: Cache only frequently accessed pages
3. **Cache Warming**: Pre-load popular pages into session storage
4. **Analytics**: Track cache hit rates and performance improvements 