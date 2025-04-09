/**
 * Image Optimizer for Future Technology E-Commerce
 * Handles responsive image loading and optimization
 */

(function () {
  "use strict";

  // Configuration options
  const config = {
    lazyLoadClass: "lazy",
    lazyBackgroundClass: "lazy-background",
    loadingPlaceholder:
      'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 150"%3E%3Crect width="100%25" height="100%25" fill="%23f3f4f6"/%3E%3C/svg%3E',
    errorPlaceholder:
      'data:image/svg+xml,%3Csvg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 300 150"%3E%3Crect width="100%25" height="100%25" fill="%23f9fafb"/%3E%3Cpath d="M118 75L130 87L142 75L148 81L136 93L148 105L142 111L130 99L118 111L112 105L124 93L112 81L118 75Z" fill="%23f43f5e"/%3E%3C/svg%3E',
    rootMargin: "200px 0px",
    threshold: 0.01,
  };

  // Check if device supports WebP
  const checkWebP = () => {
    return new Promise((resolve) => {
      const webP = new Image();
      webP.onload = function () {
        resolve(webP.height === 1);
      };
      webP.onerror = function () {
        resolve(false);
      };
      webP.src =
        "data:image/webp;base64,UklGRiQAAABXRUJQVlA4IBgAAAAwAQCdASoBAAEAAwA0JaQAA3AA/vuUAAA=";
    });
  };

  // Process responsive image sources
  const processResponsiveImage = (img) => {
    // Set loading placeholder if not already loaded
    if (!img.src || img.src === window.location.href) {
      img.src = config.loadingPlaceholder;
    }

    // Add default error handler
    img.onerror = function () {
      if (this.src !== config.errorPlaceholder) {
        this.src = config.errorPlaceholder;
      }
    };

    // Mark as loading
    img.classList.add("is-loading");

    // Function to load and process the image
    const loadImage = () => {
      // Get the data source
      const src = img.dataset.src;

      if (!src) return;

      // Create new image for loading
      const newImg = new Image();

      // When it loads, update the original image
      newImg.onload = function () {
        img.src = src;
        img.classList.remove("is-loading");
        img.classList.add("is-loaded");

        // Handle srcset if available
        if (img.dataset.srcset) {
          img.srcset = img.dataset.srcset;
        }

        // Handle sizes if available
        if (img.dataset.sizes) {
          img.sizes = img.dataset.sizes;
        }

        // Clean up data attributes
        img.removeAttribute("data-src");
        img.removeAttribute("data-srcset");
        img.removeAttribute("data-sizes");
      };

      // Error handling
      newImg.onerror = function () {
        img.src = config.errorPlaceholder;
        img.classList.remove("is-loading");
        img.classList.add("is-error");
      };

      // Start loading
      newImg.src = src;
    };

    // Return the load function
    return loadImage;
  };

  // Process background image
  const processBackgroundImage = (element) => {
    const loadBackground = () => {
      const src = element.dataset.background;

      if (!src) return;

      // Create new image to preload
      const img = new Image();

      img.onload = function () {
        element.style.backgroundImage = `url(${src})`;
        element.classList.add("is-loaded");
        element.classList.remove("is-loading");
        element.removeAttribute("data-background");
      };

      img.onerror = function () {
        element.classList.remove("is-loading");
        element.classList.add("is-error");
      };

      img.src = src;
    };

    return loadBackground;
  };

  // Use Intersection Observer API for lazy loading
  const setupLazyLoading = (supportsWebP) => {
    if (!("IntersectionObserver" in window)) {
      // Fallback for browsers that don't support Intersection Observer
      document
        .querySelectorAll(`img.${config.lazyLoadClass}`)
        .forEach((img) => {
          const loadFunc = processResponsiveImage(img);
          loadFunc();
        });

      document
        .querySelectorAll(`.${config.lazyBackgroundClass}`)
        .forEach((el) => {
          const loadFunc = processBackgroundImage(el);
          loadFunc();
        });

      return;
    }

    // Create observer instance
    const observer = new IntersectionObserver(
      (entries) => {
        entries.forEach((entry) => {
          if (entry.isIntersecting) {
            const element = entry.target;

            // Handle image elements
            if (element.tagName.toLowerCase() === "img") {
              const loadFunc = processResponsiveImage(element);
              loadFunc();
            }
            // Handle background images
            else if (element.classList.contains(config.lazyBackgroundClass)) {
              const loadFunc = processBackgroundImage(element);
              loadFunc();
            }

            // Stop observing the element
            observer.unobserve(element);
          }
        });
      },
      {
        rootMargin: config.rootMargin,
        threshold: config.threshold,
      }
    );

    // Start observing elements
    document.querySelectorAll(`img.${config.lazyLoadClass}`).forEach((img) => {
      // If WebP is supported and there's a WebP version available, use it
      if (supportsWebP && img.dataset.srcWebp) {
        img.dataset.src = img.dataset.srcWebp;

        // Update srcset if WebP versions are available
        if (img.dataset.srcsetWebp) {
          img.dataset.srcset = img.dataset.srcsetWebp;
        }
      }

      observer.observe(img);
    });

    document
      .querySelectorAll(`.${config.lazyBackgroundClass}`)
      .forEach((el) => {
        // Use WebP version if available and supported
        if (supportsWebP && el.dataset.backgroundWebp) {
          el.dataset.background = el.dataset.backgroundWebp;
        }

        observer.observe(el);
      });
  };

  // Apply responsive image techniques
  const setupResponsiveImages = () => {
    // Programmatically select the right image size
    const updateResponsiveImageSources = () => {
      const viewportWidth = window.innerWidth;
      const pixelRatio = window.devicePixelRatio || 1;
      const effectiveWidth = viewportWidth * pixelRatio;

      document.querySelectorAll('img[data-sizes="auto"]').forEach((img) => {
        // Calculate image width based on its container
        const parentWidth = img.parentElement.offsetWidth * pixelRatio;
        const imageWidth = parentWidth > 0 ? parentWidth : effectiveWidth;

        // Set the sizes attribute
        img.sizes = `${Math.ceil(parentWidth / pixelRatio)}px`;

        // If srcset is present, browser will handle the rest
        if (img.srcset) {
          return;
        }

        // If data-src-base and data-widths are present, generate srcset
        const srcBase = img.dataset.srcBase;
        const widths = img.dataset.widths;

        if (srcBase && widths) {
          const widthArray = widths
            .split(",")
            .map((w) => parseInt(w.trim(), 10));
          const extension = srcBase.substring(srcBase.lastIndexOf("."));
          const basePath = srcBase.substring(0, srcBase.lastIndexOf("."));

          let srcsetString = "";
          let bestSrc = "";
          let bestWidth = 0;

          // Sort widths ascending
          widthArray.sort((a, b) => a - b);

          widthArray.forEach((width) => {
            const src = `${basePath}-${width}${extension}`;
            srcsetString += `${src} ${width}w, `;

            // Find best source for current viewport
            if (width >= imageWidth && (bestWidth === 0 || width < bestWidth)) {
              bestWidth = width;
              bestSrc = src;
            }
          });

          // Set srcset
          if (srcsetString) {
            img.srcset = srcsetString.trim().slice(0, -1); // remove trailing comma
          }

          // Set src to best match or largest
          if (!bestSrc && widthArray.length > 0) {
            bestSrc = `${basePath}-${
              widthArray[widthArray.length - 1]
            }${extension}`;
          }

          if (bestSrc) {
            img.src = bestSrc;
          }
        }
      });
    };

    // Initialize
    updateResponsiveImageSources();

    // Update on resize (debounced)
    let resizeTimer;
    window.addEventListener("resize", () => {
      clearTimeout(resizeTimer);
      resizeTimer = setTimeout(updateResponsiveImageSources, 250);
    });
  };

  // Add blur-up effect for images
  const setupBlurUpEffect = () => {
    document.querySelectorAll(".blur-up").forEach((img) => {
      img.onload = function () {
        img.classList.add("loaded");
        if (img.parentElement.classList.contains("img-wrap")) {
          img.parentElement.classList.add("loaded");
        }
      };

      // If already complete, trigger the event
      if (img.complete) {
        img.classList.add("loaded");
        if (img.parentElement.classList.contains("img-wrap")) {
          img.parentElement.classList.add("loaded");
        }
      }
    });
  };

  // Initialize image optimization
  const init = async () => {
    // Check for WebP support
    const supportsWebP = await checkWebP();

    if (supportsWebP) {
      document.body.classList.add("webp-support");
    }

    // Set up responsive image handling
    setupResponsiveImages();

    // Set up lazy loading
    setupLazyLoading(supportsWebP);

    // Set up blur-up effect
    setupBlurUpEffect();
  };

  // Initialize when DOM is ready
  if (document.readyState === "loading") {
    document.addEventListener("DOMContentLoaded", init);
  } else {
    init();
  }

  // Expose methods for external use
  window.ImageOptimizer = {
    refresh: init,
    processImage: (img) => {
      const loadFunc = processResponsiveImage(img);
      loadFunc();
    },
    processBackground: (element) => {
      const loadFunc = processBackgroundImage(element);
      loadFunc();
    },
  };
})();
