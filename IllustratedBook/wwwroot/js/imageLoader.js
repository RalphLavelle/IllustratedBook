class ImageLoader {

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
                this.displayImage(result.imageUrl, imageContainerId, loadingContainerId);
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