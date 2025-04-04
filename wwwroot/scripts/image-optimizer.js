/**
 * Image Optimization Helper Script
 *
 * This script provides functions to help with image optimization and lazy loading
 * to improve site performance based on Lighthouse recommendations.
 */

// Apply lazy loading to all images that don't already have it
function applyLazyLoading() {
  if ("loading" in HTMLImageElement.prototype) {
    // Browser supports native lazy loading
    document.querySelectorAll("img:not([loading])").forEach((img) => {
      img.setAttribute("loading", "lazy");
    });
  } else {
    console.log("Browser does not support native lazy loading");
    // Consider implementing a polyfill for older browsers
  }
}

// Set explicit width and height on images to reduce layout shifts
function setImageDimensions() {
  document.querySelectorAll("img:not([width]):not([height])").forEach((img) => {
    img.addEventListener("load", function () {
      if (!this.hasAttribute("width") && !this.hasAttribute("height")) {
        // Only set attributes if they aren't already set via CSS
        const styles = window.getComputedStyle(this);
        const width = styles.getPropertyValue("width");
        const height = styles.getPropertyValue("height");

        if (width !== "auto" && height !== "auto") {
          this.setAttribute("width", parseInt(width, 10));
          this.setAttribute("height", parseInt(height, 10));
        }
      }
    });
  });
}

// Initialize optimization when DOM is loaded
document.addEventListener("DOMContentLoaded", function () {
  applyLazyLoading();
  setImageDimensions();

  // Listen for dynamic content changes and apply lazy loading
  if (window.MutationObserver) {
    const observer = new MutationObserver(function (mutations) {
      mutations.forEach(function (mutation) {
        if (mutation.addedNodes.length) {
          applyLazyLoading();
        }
      });
    });

    observer.observe(document.body, {
      childList: true,
      subtree: true,
    });
  }
});

// Export functions for use in other scripts
window.imageOptimizer = {
  applyLazyLoading,
  setImageDimensions,
};
