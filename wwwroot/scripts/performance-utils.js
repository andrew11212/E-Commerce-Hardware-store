/**
 * Performance Optimization Utilities
 * Based on Lighthouse recommendations to improve website performance
 */

// Function to compress CSS by removing comments and whitespace
function minifyCssOnTheFly() {
  const styleElements = document.querySelectorAll("style");
  styleElements.forEach((style) => {
    if (style.textContent) {
      // Basic CSS minification (comments, extra whitespace)
      const minified = style.textContent
        .replace(/\/\*[\s\S]*?\*\//g, "") // Remove comments
        .replace(/\s+/g, " ") // Collapse whitespace
        .replace(/\s*([{}:;,])\s*/g, "$1") // Remove spaces around punctuation
        .trim();
      style.textContent = minified;
    }
  });
}

// Defer non-critical JavaScript
function deferNonCriticalJS() {
  document
    .querySelectorAll("script:not([defer]):not([async])")
    .forEach((script) => {
      // Skip already processed scripts and core scripts
      if (
        script.hasAttribute("data-processed") ||
        script.src.includes("jquery") ||
        script.src.includes("bootstrap.bundle")
      ) {
        return;
      }

      // Mark as processed to avoid reprocessing
      script.setAttribute("data-processed", "true");

      // If it's an external script, add defer
      if (script.src) {
        script.defer = true;
      }
    });
}

// Preconnect to critical third-party origins
function preconnectToOrigins() {
  const origins = [
    "https://cdn.jsdelivr.net",
    "https://cdnjs.cloudflare.com",
    "https://fonts.googleapis.com",
    "https://fonts.gstatic.com",
  ];

  origins.forEach((origin) => {
    if (!document.querySelector(`link[rel="preconnect"][href="${origin}"]`)) {
      const link = document.createElement("link");
      link.rel = "preconnect";
      link.href = origin;
      link.crossOrigin = "anonymous";
      document.head.appendChild(link);
    }
  });
}

// Initialize performance optimizations
document.addEventListener("DOMContentLoaded", function () {
  // Apply optimizations
  minifyCssOnTheFly();
  deferNonCriticalJS();
  preconnectToOrigins();

  // Measure and report performance metrics
  if (window.performance && window.performance.mark) {
    window.performance.mark("app-ready");

    // Report metrics when page is fully loaded
    window.addEventListener("load", () => {
      window.performance.mark("fully-loaded");
      window.performance.measure(
        "time-to-ready",
        "navigationStart",
        "app-ready"
      );
      window.performance.measure(
        "total-load-time",
        "navigationStart",
        "fully-loaded"
      );

      console.log("Performance metrics:");
      const readyTime = window.performance.getEntriesByName("time-to-ready")[0];
      const loadTime =
        window.performance.getEntriesByName("total-load-time")[0];

      if (readyTime)
        console.log(`App ready in: ${readyTime.duration.toFixed(2)}ms`);
      if (loadTime)
        console.log(`Page fully loaded in: ${loadTime.duration.toFixed(2)}ms`);
    });
  }
});

// Export functions for use in other scripts
window.performanceUtils = {
  minifyCssOnTheFly,
  deferNonCriticalJS,
  preconnectToOrigins,
};
