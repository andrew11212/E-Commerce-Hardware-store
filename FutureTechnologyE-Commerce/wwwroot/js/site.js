// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.
document.addEventListener('DOMContentLoaded', function () {
    // Language direction handling
    const htmlElement = document.documentElement;
    if (htmlElement.lang.startsWith('ar')) {
        htmlElement.dir = 'rtl';
    } else {
        htmlElement.dir = 'ltr';
    }
});
// Write your JavaScript code.
