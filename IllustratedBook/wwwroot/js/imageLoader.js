/**
 * Image Loader - Handles asynchronous image generation for book pages
 * This script allows the page content to load immediately while images generate in the background
 */

class ImageLoader {
    constructor() {
        this.pollingInterval = 2000; // Check every 2 seconds
        this.maxAttempts = 30; // Maximum 60 seconds of polling
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
                // Start polling for the generated image
                this.pollForImage(result.imageUrl, imageContainerId, loadingContainerId);
            } else {
                this.showError(imageContainerId, loadingContainerId, result.error || 'Failed to start image generation');
            }
        } catch (error) {
            console.error('Error starting image generation:', error);
            this.showError(imageContainerId, loadingContainerId, 'Failed to start image generation');
        }
    }

    /**
     * Polls for the generated image until it's ready
     * @param {string} imageUrl - The URL to check for the generated image
     * @param {string} imageContainerId - The ID of the container to update
     * @param {string} loadingContainerId - The ID of the loading container to hide
     */
    async pollForImage(imageUrl, imageContainerId, loadingContainerId) {
        let attempts = 0;
        
        const poll = async () => {
            attempts++;
            
            try {
                const response = await fetch(imageUrl, { method: 'HEAD' });
                
                if (response.ok) {
                    // Image is ready, display it
                    this.displayImage(imageUrl, imageContainerId, loadingContainerId);
                    return;
                }
            } catch (error) {
                console.log('Image not ready yet, attempt:', attempts);
            }
            
            // Continue polling if we haven't exceeded max attempts
            if (attempts < this.maxAttempts) {
                setTimeout(poll, this.pollingInterval);
            } else {
                this.showError(imageContainerId, loadingContainerId, 'Image generation timed out');
            }
        };
        
        // Start polling
        poll();
    }

    /**
     * Displays the generated image
     * @param {string} imageUrl - The URL of the generated image
     * @param {string} imageContainerId - The ID of the container to update
     * @param {string} loadingContainerId - The ID of the loading container to hide
     */
    displayImage(imageUrl, imageContainerId, loadingContainerId) {
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
            
            imageContainer.innerHTML = '';
            imageContainer.appendChild(img);
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