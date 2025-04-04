// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.
document.addEventListener("DOMContentLoaded", function () {
  // Language direction handling
  const htmlElement = document.documentElement;
  if (htmlElement.lang.startsWith("ar")) {
    htmlElement.dir = "rtl";
  } else {
    htmlElement.dir = "ltr";
  }

  // Lazy load all images for better performance
  if ("loading" in HTMLImageElement.prototype) {
    // Browser supports native lazy loading
    document.querySelectorAll("img").forEach((img) => {
      if (!img.hasAttribute("loading")) {
        img.setAttribute("loading", "lazy");
      }
    });
  } else {
    // Fallback for browsers that don't support lazy loading
    // Consider adding a lazy loading library like lozad.js if needed
  }
});
