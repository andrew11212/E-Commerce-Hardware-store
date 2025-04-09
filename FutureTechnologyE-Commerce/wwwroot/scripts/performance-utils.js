/**
 * Performance Utilities for Future Technology E-Commerce
 * Optimizes site performance through various techniques
 */

(function () {
  "use strict";

  // Defer non-critical resources
  const deferResource = (type, url, async = true, defer = true) => {
    if (type === "script") {
      const script = document.createElement("script");
      script.src = url;
      if (async) script.async = true;
      if (defer) script.defer = true;
      document.body.appendChild(script);
    } else if (type === "style") {
      const link = document.createElement("link");
      link.rel = "stylesheet";
      link.href = url;
      link.media = "print";
      link.onload = function () {
        this.media = "all";
      };
      document.head.appendChild(link);
    }
  };

  // Intersection Observer for lazy loading
  const setupIntersectionObserver = () => {
    if ("IntersectionObserver" in window) {
      const lazyLoadObserver = new IntersectionObserver(
        (entries) => {
          entries.forEach((entry) => {
            if (entry.isIntersecting) {
              const target = entry.target;

              // Handle different element types
              if (target.tagName.toLowerCase() === "img") {
                if (target.dataset.src) {
                  target.src = target.dataset.src;
                  if (target.dataset.srcset) {
                    target.srcset = target.dataset.srcset;
                  }
                }
              } else if (target.classList.contains("lazy-background")) {
                if (target.dataset.background) {
                  target.style.backgroundImage = `url(${target.dataset.background})`;
                }
              } else if (target.tagName.toLowerCase() === "iframe") {
                if (target.dataset.src) {
                  target.src = target.dataset.src;
                }
              }

              // Animate if needed
              if (target.classList.contains("animate-on-scroll")) {
                target.classList.add("animated");
              }

              // Remove from observation
              lazyLoadObserver.unobserve(target);
            }
          });
        },
        {
          rootMargin: "0px 0px 200px 0px", // Load when within 200px of viewport
          threshold: 0.01,
        }
      );

      // Observe all lazy elements
      document
        .querySelectorAll(".lazy, .lazy-background, .animate-on-scroll")
        .forEach((element) => {
          lazyLoadObserver.observe(element);
        });
    } else {
      // Fallback for browsers that don't support IntersectionObserver
      document.querySelectorAll(".lazy").forEach((img) => {
        if (img.dataset.src) {
          img.src = img.dataset.src;
        }
      });
    }
  };

  // Debounce function for scroll events
  const debounce = (func, wait) => {
    let timeout;
    return function () {
      const context = this;
      const args = arguments;
      clearTimeout(timeout);
      timeout = setTimeout(() => {
        func.apply(context, args);
      }, wait);
    };
  };

  // Throttle function for better performance on continuous events
  const throttle = (func, limit) => {
    let inThrottle;
    return function () {
      const args = arguments;
      const context = this;
      if (!inThrottle) {
        func.apply(context, args);
        inThrottle = true;
        setTimeout(() => (inThrottle = false), limit);
      }
    };
  };

  // Detect and store user device/browser info for optimizations
  const detectDevice = () => {
    const deviceInfo = {
      isMobile:
        /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(
          navigator.userAgent
        ),
      isTablet: /(iPad|tablet|Tablet|Android(?!.*Mobile))/i.test(
        navigator.userAgent
      ),
      isDesktop:
        !/Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(
          navigator.userAgent
        ),
      browser: (function () {
        const ua = navigator.userAgent;
        let browser = "Unknown";
        if (ua.indexOf("Chrome") > -1) browser = "Chrome";
        else if (ua.indexOf("Safari") > -1) browser = "Safari";
        else if (ua.indexOf("Firefox") > -1) browser = "Firefox";
        else if (ua.indexOf("MSIE") > -1 || ua.indexOf("Trident") > -1)
          browser = "IE";
        else if (ua.indexOf("Edge") > -1) browser = "Edge";
        return browser;
      })(),
      supportsWebP: false,
    };

    // Add device class to body for CSS targeting
    document.body.classList.add(
      deviceInfo.isMobile
        ? "mobile-device"
        : deviceInfo.isTablet
        ? "tablet-device"
        : "desktop-device"
    );

    // Test for WebP support
    const webP = new Image();
    webP.onload = function () {
      deviceInfo.supportsWebP = webP.height === 1;
      if (deviceInfo.supportsWebP) {
        document.body.classList.add("webp-support");
      }
    };
    webP.onerror = function () {
      deviceInfo.supportsWebP = false;
    };
    webP.src =
      "data:image/webp;base64,UklGRiQAAABXRUJQVlA4IBgAAAAwAQCdASoBAAEAAwA0JaQAA3AA/vuUAAA=";

    // Store in localStorage for later use
    localStorage.setItem("deviceInfo", JSON.stringify(deviceInfo));

    return deviceInfo;
  };

  // Setup carousel optimizations
  const optimizeCarousels = () => {
    if (typeof $ !== "undefined" && typeof $.fn.carousel !== "undefined") {
      // Pause carousels when not in viewport
      const carousels = document.querySelectorAll(".carousel");
      if (carousels.length && "IntersectionObserver" in window) {
        const carouselObserver = new IntersectionObserver(
          (entries) => {
            entries.forEach((entry) => {
              if (entry.isIntersecting) {
                $(entry.target).carousel("cycle");
              } else {
                $(entry.target).carousel("pause");
              }
            });
          },
          { rootMargin: "0px", threshold: 0.1 }
        );

        carousels.forEach((carousel) => {
          carouselObserver.observe(carousel);
        });
      }

      // Preload next slide image
      $(".carousel").on("slide.bs.carousel", function (e) {
        const nextSlide = $(e.relatedTarget);
        const img = nextSlide.find("img[data-src]");
        if (img.length) {
          img.attr("src", img.data("src"));
          img.removeAttr("data-src");
        }
      });
    }
  };

  // Setup image optimization
  const optimizeImages = () => {
    // Replace image sources with appropriate sizes based on screen width
    const updateImageSources = () => {
      const screenWidth = window.innerWidth;
      const images = document.querySelectorAll("img[data-srcset]");

      images.forEach((img) => {
        if (!img.dataset.srcset) return;

        const srcset = img.dataset.srcset.split(",");
        let bestSource = "";
        let bestWidth = 0;

        srcset.forEach((src) => {
          const parts = src.trim().split(" ");
          if (parts.length === 2) {
            const url = parts[0];
            const width = parseInt(parts[1].replace("w", ""), 10);

            if (
              width >= screenWidth &&
              (bestWidth === 0 || width < bestWidth)
            ) {
              bestWidth = width;
              bestSource = url;
            }
          }
        });

        if (bestSource && (!img.src || img.src !== bestSource)) {
          img.src = bestSource;
        }
      });
    };

    // Initialize
    updateImageSources();
    // Update on resize (throttled)
    window.addEventListener("resize", throttle(updateImageSources, 200));
  };

  // Performance monitoring
  const initPerformanceMonitoring = () => {
    if ("performance" in window && "PerformanceObserver" in window) {
      // Create performance entries observer
      const perfObserver = new PerformanceObserver((list) => {
        const entries = list.getEntries();

        entries.forEach((entry) => {
          // Log this only during development
          if (
            window.location.hostname === "localhost" ||
            window.location.hostname === "127.0.0.1"
          ) {
            console.log(
              "[Performance]",
              entry.name,
              entry.startTime.toFixed(0) + "ms",
              entry.duration
                ? "Duration: " + entry.duration.toFixed(0) + "ms"
                : ""
            );
          }

          // Send to analytics (if available)
          if (
            typeof gtag === "function" &&
            entry.entryType === "largest-contentful-paint"
          ) {
            gtag("event", "performance", {
              metric_name: entry.entryType,
              metric_value: entry.startTime.toFixed(0),
              page_path: window.location.pathname,
            });
          }
        });
      });

      // Observe various performance metrics
      try {
        perfObserver.observe({
          entryTypes: [
            "largest-contentful-paint",
            "first-input",
            "layout-shift",
          ],
        });
      } catch (e) {
        console.warn(
          "Performance observer not fully supported in this browser"
        );
      }
    }
  };

  // Initialize all performance optimizations
  const init = () => {
    // Wait for DOM content to be loaded
    document.addEventListener("DOMContentLoaded", () => {
      detectDevice();
      setupIntersectionObserver();
      optimizeCarousels();
      optimizeImages();
      initPerformanceMonitoring();
      setupNotifications();

      // Remove preload hints after they've served their purpose
      setTimeout(() => {
        document.querySelectorAll('link[rel="preload"]').forEach((link) => {
          link.remove();
        });
      }, 3000);
    });

    // Handle page visibility changes
    document.addEventListener("visibilitychange", () => {
      if (document.visibilityState === "visible") {
        // User has returned to the page, resume any paused activities
        if (typeof $ !== "undefined" && typeof $.fn.carousel !== "undefined") {
          $(".carousel:visible").carousel("cycle");
        }
      } else {
        // User has left the page, pause intensive activities
        if (typeof $ !== "undefined" && typeof $.fn.carousel !== "undefined") {
          $(".carousel").carousel("pause");
        }
      }
    });

    // Back to top functionality
    $(window).scroll(function () {
      if ($(this).scrollTop() > 300) {
        $("#back-to-top").fadeIn().addClass("show");
      } else {
        $("#back-to-top").fadeOut().removeClass("show");
      }

      // Remove navbar scrolled class handling - no sticky navbar
    });

    $("#back-to-top").click(function () {
      $("html, body").animate({ scrollTop: 0 }, 800);
      return false;
    });
  };

  // Setup notifications functionality
  const setupNotifications = () => {
    const notificationToggle = document.getElementById("notificationToggle");
    const notificationPanel = document.getElementById("notificationPanel");
    const notificationOverlay = document.getElementById("notificationOverlay");
    const closeNotifications = document.getElementById("closeNotifications");
    const markAllRead = document.querySelector(".mark-all-read");

    if (notificationToggle && notificationPanel) {
      // Toggle notifications panel when clicking the bell
      notificationToggle.addEventListener("click", function (e) {
        e.preventDefault();
        e.stopPropagation();

        // Open notification panel
        notificationPanel.classList.add("active");
        notificationOverlay.classList.add("active");
        document.body.style.overflow = "hidden"; // Prevent scrolling
      });

      // Close when clicking the overlay
      if (notificationOverlay) {
        notificationOverlay.addEventListener("click", closeNotificationPanel);
      }

      // Close when clicking the close button
      if (closeNotifications) {
        closeNotifications.addEventListener("click", closeNotificationPanel);
      }

      // Close on escape key
      document.addEventListener("keydown", function (e) {
        if (
          e.key === "Escape" &&
          notificationPanel.classList.contains("active")
        ) {
          closeNotificationPanel();
        }
      });

      // Mark all as read functionality
      if (markAllRead) {
        markAllRead.addEventListener("click", function (e) {
          e.preventDefault();
          const counter = document.querySelector(".notification-counter");
          if (counter) {
            counter.textContent = "0";
            counter.style.display = "none";
          }

          // Mark all items as read
          document
            .querySelectorAll(".notification-item.unread")
            .forEach((item) => {
              item.classList.remove("unread");
            });

          // Show empty state if needed
          const notificationList = document.querySelector(".notification-list");
          const noNotificationsMessage = document.querySelector(
            ".no-notifications-message"
          );

          if (
            notificationList &&
            noNotificationsMessage &&
            document.querySelectorAll(".notification-item.unread").length === 0
          ) {
            notificationList.classList.add("d-none");
            noNotificationsMessage.classList.remove("d-none");
          }

          // Call API to mark all as read if needed
          if (typeof markNotificationsAsRead === "function") {
            markNotificationsAsRead();
          }
        });
      }

      // Helper function to close notification panel
      function closeNotificationPanel() {
        notificationPanel.classList.remove("active");
        notificationOverlay.classList.remove("active");
        document.body.style.overflow = ""; // Restore scrolling
      }
    }
  };

  // Position notifications dropdown correctly
  const positionNotificationDropdown = () => {
    const bell = document.querySelector(".bell-icon-container");
    const dropdown = document.querySelector(".notification-dropdown");

    if (bell && dropdown && !dropdown.classList.contains("d-none")) {
      const bellRect = bell.getBoundingClientRect();
      const windowWidth = window.innerWidth;
      const hasAdminNavbar = document.querySelector(".admin-navbar") !== null;

      // Get main navbar height
      const mainNavbar = document.querySelector(".navbar");
      const mainNavbarHeight = mainNavbar ? mainNavbar.offsetHeight : 0;

      // Get admin navbar height if it exists
      const adminNavbar = document.querySelector(".admin-navbar");
      const adminNavbarHeight = adminNavbar ? adminNavbar.offsetHeight : 0;

      // Calculate total offset based on navbars
      const totalOffset = mainNavbarHeight + adminNavbarHeight + 10; // 10px extra padding

      // For mobile devices
      if (windowWidth <= 576) {
        dropdown.style.position = "fixed";
        dropdown.style.top = totalOffset + "px";
        dropdown.style.right = "10px";
        dropdown.style.left = "10px";
        dropdown.style.width = "auto";
        dropdown.style.maxHeight = `calc(80vh - ${totalOffset}px)`;
        dropdown.style.zIndex = "1050";
      } else {
        // For larger screens - calculate position relative to bell icon
        // First ensure the dropdown is properly positioned relative to the bell
        dropdown.style.position = "absolute";

        // Set initial top position beneath bell icon
        const initialTop = bellRect.bottom + window.scrollY;

        // Ensure dropdown is below all navbars
        const minTopPosition = window.scrollY + totalOffset;
        const topPosition = Math.max(initialTop, minTopPosition);

        dropdown.style.top = topPosition + "px";

        // Handle horizontal positioning
        if (bellRect.right + 350 > windowWidth) {
          const rightPosition = windowWidth - bellRect.right;
          dropdown.style.right = rightPosition + "px";
          dropdown.style.left = "auto";
        } else {
          dropdown.style.right = windowWidth - bellRect.right + "px";
          dropdown.style.left = "auto";
        }

        dropdown.style.width = "350px";
        dropdown.style.zIndex = "1050";
      }

      // Add a small arrow/pointer at the top of dropdown pointing to the notification bell
      if (windowWidth > 576) {
        // Only add pointer if dropdown isn't too far from bell (when not scrolled past navbars)
        const dropdownRect = dropdown.getBoundingClientRect();
        const shouldShowPointer =
          Math.abs(dropdownRect.top - bellRect.bottom) < 30;

        // Remove any existing pointer
        const existingPointer = dropdown.querySelector(".dropdown-pointer");
        if (existingPointer) {
          existingPointer.remove();
        }

        if (shouldShowPointer) {
          // Create and add pointer
          const pointer = document.createElement("div");
          pointer.className = "dropdown-pointer";
          pointer.style.position = "absolute";
          pointer.style.top = "-8px";
          pointer.style.right = "10px";
          pointer.style.width = "16px";
          pointer.style.height = "8px";
          pointer.style.overflow = "hidden";

          const pointerInner = document.createElement("div");
          pointerInner.style.position = "absolute";
          pointerInner.style.top = "3px";
          pointerInner.style.left = "0";
          pointerInner.style.width = "16px";
          pointerInner.style.height = "16px";
          pointerInner.style.backgroundColor = "white";
          pointerInner.style.border = "1px solid rgba(0,0,0,0.1)";
          pointerInner.style.borderRight = "none";
          pointerInner.style.borderBottom = "none";
          pointerInner.style.transform = "rotate(45deg)";

          pointer.appendChild(pointerInner);
          dropdown.appendChild(pointer);
        }
      }

      // Ensure the dropdown is within viewport bounds
      const dropdownRect = dropdown.getBoundingClientRect();
      if (dropdownRect.bottom > window.innerHeight) {
        const overflowAmount = dropdownRect.bottom - window.innerHeight;
        dropdown.style.maxHeight =
          dropdownRect.height - overflowAmount - 10 + "px"; // 10px buffer
      }
    }
  };

  // Function to mark all notifications as read via API
  const markNotificationsAsRead = () => {
    // Get the anti-forgery token
    const token = document.querySelector(
      'input[name="__RequestVerificationToken"]'
    )?.value;

    if (token) {
      // Create form data
      const formData = new FormData();
      formData.append("__RequestVerificationToken", token);

      // Make the API call
      fetch("/Notifications/MarkAllAsRead", {
        method: "POST",
        body: formData,
        headers: {
          "X-Requested-With": "XMLHttpRequest",
        },
      })
        .then((response) => response.json())
        .then((data) => {
          if (data.success) {
            console.log("All notifications marked as read");
            // Update UI if needed
            updateNotificationCount();
          }
        })
        .catch((error) => {
          console.error("Error marking notifications as read:", error);
        });
    }
  };

  // Function to update notification count in navbar
  const updateNotificationCount = () => {
    const counter = document.querySelector(".notification-counter");

    // Get the current count from the server
    fetch("/Notifications/GetUnreadCount", {
      method: "GET",
      headers: {
        "X-Requested-With": "XMLHttpRequest",
      },
    })
      .then((response) => response.json())
      .then((data) => {
        if (counter) {
          const count = data.count || 0;
          counter.textContent = count.toString();

          // Hide counter if zero
          if (count === 0) {
            counter.style.display = "none";
          } else {
            counter.style.display = "";
          }
        }
      })
      .catch((error) => {
        console.error("Error updating notification count:", error);
      });
  };

  // Execute initialization
  init();

  // Expose utilities to global scope if needed
  window.FutureTechUtils = {
    debounce: debounce,
    throttle: throttle,
    deferResource: deferResource,
    positionNotificationDropdown: positionNotificationDropdown,
  };
})();
