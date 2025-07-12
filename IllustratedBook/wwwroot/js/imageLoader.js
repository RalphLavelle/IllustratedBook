class ImageLoader {

    /**
     * Generates a unique session storage key for the current chapter and page
     * @param {number} bookId - The book ID
     * @param {number} chapterId - The chapter ID
     * @param {number} pageId - The page ID (optional)
     * @returns {string} A unique key for session storage
     */
    getSessionStorageKey(bookId, chapterId, pageId = null) {
        return pageId !== null 
            ? `book_${bookId}_chapter_${chapterId}_page_${pageId}`
            : `book_${bookId}_chapter_${chapterId}`;
    }

    /**
     * Saves page data to browser session storage
     * @param {number} bookId - The book ID
     * @param {number} chapterId - The chapter ID
     * @param {number} pageId - The page ID
     * @param {Object} pageData - The page data to save
     */
    savePageToSessionStorage(bookId, chapterId, pageId, pageData) {
        try {
            const key = this.getSessionStorageKey(bookId, chapterId, pageId);
            const data = {
                bookId: bookId,
                chapterId: chapterId,
                pageId: pageId,
                pageData: pageData,
                timestamp: new Date().toISOString()
            };
            
            sessionStorage.setItem(key, JSON.stringify(data));
            console.log(`Saved page data to session storage: ${key}`);
        } catch (error) {
            console.error('Error saving to session storage:', error);
        }
    }

    /**
     * Retrieves page data from browser session storage
     * @param {number} bookId - The book ID
     * @param {number} chapterId - The chapter ID
     * @param {number} pageId - The page ID
     * @returns {Object|null} The page data if found, null otherwise
     */
    getPageFromSessionStorage(bookId, chapterId, pageId) {
        try {
            const key = this.getSessionStorageKey(bookId, chapterId, pageId);
            const sessionData = sessionStorage.getItem(key);
            
            if (sessionData) {
                const data = JSON.parse(sessionData);
                // Check if data is not too old (30 minutes)
                const timestamp = new Date(data.timestamp);
                const now = new Date();
                const diffInMinutes = (now - timestamp) / (1000 * 60);
                
                if (diffInMinutes < 30) {
                    console.log(`Retrieved page data from session storage: ${key}`);
                    return data.pageData;
                } else {
                    // Remove expired data
                    sessionStorage.removeItem(key);
                    console.log(`Removed expired session data: ${key}`);
                }
            }
            
            return null;
        } catch (error) {
            console.error('Error retrieving from session storage:', error);
            return null;
        }
    }

    /**
     * Checks if page data exists in session storage and optionally loads it
     * @param {number} bookId - The book ID
     * @param {number} chapterId - The chapter ID
     * @param {number} pageId - The page ID
     * @param {Function} callback - Callback function to handle the result
     */
    async checkPageInSessionStorage(bookId, chapterId, pageId, callback) {
        debugger;
        try {
            const cachedData = this.getPageFromSessionStorage(bookId, chapterId, pageId);
            
            if (cachedData) {
                console.log(`Found page data in session storage for page ${pageId}`);
                callback(true, cachedData);
                return;
            }
            
            // If not in session storage, try server-side session storage
            const response = await fetch(`/books/${bookId}/chapters/${chapterId}/pages/${pageId}?handler=PageFromSession`, {
                method: 'GET',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });
            
            if (response.ok) {
                const result = await response.json();
                if (result.success && result.pageData) {
                    console.log(`Found page data in server session storage for page ${pageId}`);
                    // Save to client session storage for future use
                    this.savePageToSessionStorage(bookId, chapterId, pageId, result.pageData);
                    callback(true, result.pageData);
                    return;
                }
            }
            
            console.log(`Page data not found in session storage for page ${pageId}`);
            callback(false, null);
        } catch (error) {
            console.error('Error checking session storage:', error);
            callback(false, null);
        }
    }

    /**
     * Clears all session storage data for a specific book/chapter
     * @param {number} bookId - The book ID
     * @param {number} chapterId - The chapter ID
     */
    clearSessionStorageForChapter(bookId, chapterId) {
        try {
            const keys = Object.keys(sessionStorage);
            const pattern = new RegExp(`^chapter_${bookId}_${chapterId}`);
            
            keys.forEach(key => {
                if (pattern.test(key)) {
                    sessionStorage.removeItem(key);
                    console.log(`Cleared session storage key: ${key}`);
                }
            });
        } catch (error) {
            console.error('Error clearing session storage:', error);
        }
    }

    /**
     * Starts the image generation process for a specific page
     * @param {string} pageUrl - The URL to call for image generation
     * @param {string} imageContainerId - The ID of the container to update
     * @param {string} loadingContainerId - The ID of the loading container to hide
     */
    async startImageGeneration(pageUrl, imageContainerId, loadingContainerId) {
        try {
            console.log('Starting image generation for:', pageUrl);
            
            // Show loading state
            this.showLoading(loadingContainerId);
            
            // Start the image generation process
            const response = await fetch(pageUrl, {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'RequestVerificationToken': this.getAntiForgeryToken()
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            const result = await response.json();
            
            if (result.success) {
                this.displayImage(result.imageUrl, imageContainerId, loadingContainerId, result.fromCache, result.prompt);
            } else {
                this.showError(imageContainerId, loadingContainerId, result.error || 'Failed to start image generation');
            }
        } catch (error) {
            console.error('Error starting image generation:', error);
            this.showError(imageContainerId, loadingContainerId, 'Failed to start image generation');
        }
    }

    /**
     * Displays the generated image
     * @param {string} imageUrl - The URL of the generated image
     * @param {string} imageContainerId - The ID of the container to update
     * @param {string} loadingContainerId - The ID of the loading container to hide
     * @param {boolean} fromCache - Whether the image was loaded from cache
     * @param {string} prompt - The prompt used to generate the image
     */
    displayImage(imageUrl, imageContainerId, loadingContainerId, fromCache = false, prompt = '') {
        const imageContainer = document.getElementById(imageContainerId);
        const loadingContainer = document.getElementById(loadingContainerId);
        
        if (imageContainer && loadingContainer) {
            // Hide loading container
            loadingContainer.style.display = 'none';
            
            // Create and display the image
            const img = document.createElement('img');
            img.src = imageUrl;
            img.alt = 'AI-generated illustration for this page';
            img.className = 'page-illustration';
            img.style.opacity = '0';
            img.style.transition = 'opacity 0.5s ease-in-out';
            
            // Add fade-in effect
            img.onload = () => {
                img.style.opacity = '1';
            };
            
            // Create image info container
            const imageInfo = document.createElement('div');
            imageInfo.className = 'image-info';
            
            // Add cache indicator if image is from cache
            if (fromCache) {
                const cacheIndicator = document.createElement('div');
                cacheIndicator.className = 'cache-indicator';
                cacheIndicator.innerHTML = '<span class="cache-badge">📁 Cached</span>';
                imageInfo.appendChild(cacheIndicator);
            }
            
            // Add prompt info if available
            if (prompt) {
                const promptInfo = document.createElement('div');
                promptInfo.className = 'prompt-info';
                promptInfo.innerHTML = `<small><strong>Prompt:</strong> ${prompt}</small>`;
                imageInfo.appendChild(promptInfo);
            }
            
            imageContainer.innerHTML = '';
            imageContainer.appendChild(img);
            imageContainer.appendChild(imageInfo);
            imageContainer.style.display = 'block';
        }
    }

    /**
     * Shows the loading state
     * @param {string} loadingContainerId - The ID of the loading container to show
     */
    showLoading(loadingContainerId) {
        const loadingContainer = document.getElementById(loadingContainerId);
        if (loadingContainer) {
            loadingContainer.style.display = 'block';
        }
    }

    /**
     * Shows an error message
     * @param {string} imageContainerId - The ID of the container to show error in
     * @param {string} loadingContainerId - The ID of the loading container to hide
     * @param {string} errorMessage - The error message to display
     */
    showError(imageContainerId, loadingContainerId, errorMessage) {
        const imageContainer = document.getElementById(imageContainerId);
        const loadingContainer = document.getElementById(loadingContainerId);
        
        if (loadingContainer) {
            loadingContainer.style.display = 'none';
        }
        
        if (imageContainer) {
            imageContainer.innerHTML = `
                <div class="image-generation-error">
                    <p class="error-message">${errorMessage}</p>
                    <button onclick="location.reload()" class="retry-button">Retry</button>
                </div>
            `;
            imageContainer.style.display = 'block';
        }
    }

    /**
     * Gets the anti-forgery token from the page
     * @returns {string} The anti-forgery token
     */
    getAntiForgeryToken() {
        const tokenElement = document.querySelector('input[name="__RequestVerificationToken"]');
        return tokenElement ? tokenElement.value : '';
    }
}

// Create a global instance
window.imageLoader = new ImageLoader(); 