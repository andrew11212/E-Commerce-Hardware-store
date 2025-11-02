// Performance-optimized JavaScript for Future Technology E-Commerce
document.addEventListener('DOMContentLoaded', function () {
    // Language direction handling
    const htmlElement = document.documentElement;
    if (htmlElement.lang.startsWith('ar')) {
        htmlElement.dir = 'rtl';
    } else {
        htmlElement.dir = 'ltr';
    }

    // Optimize images - lazy loading
    const images = document.querySelectorAll('img[data-src]');
    const imageObserver = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const img = entry.target;
                img.src = img.dataset.src;
                img.classList.remove('lazy');
                imageObserver.unobserve(img);
            }
        });
    });

    images.forEach(img => imageObserver.observe(img));

    // Debounce function for performance
    function debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    // Optimized search functionality
    const searchInput = document.querySelector('input[name="searchString"]');
    if (searchInput) {
        searchInput.addEventListener('input', debounce(function (e) {
            const searchTerm = e.target.value.trim();
            if (searchTerm.length > 2) {
                // Implement search suggestions if needed
                console.log('Searching for:', searchTerm);
            }
        }, 300));
    }

    // Simplified cart functionality
    const addToCartForms = document.querySelectorAll('.add-to-cart-form');
    addToCartForms.forEach(form => {
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            const button = form.querySelector('button[type="submit"]');
            const productId = form.querySelector('input[name="productId"]').value;

            // Show loading state
            button.disabled = true;
            button.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Adding...';

            // Simulate API call
            setTimeout(() => {
                button.disabled = false;
                button.innerHTML = 'Add to Cart';
                // Show success message
                showNotification('Product added to cart!', 'success');
            }, 1000);
        });
    });

    // Simple notification system
    function showNotification(message, type = 'info') {
        const notification = document.createElement('div');
        notification.className = `alert alert-${type} position-fixed top-0 end-0 m-3`;
        notification.style.zIndex = '9999';
        notification.textContent = message;

        document.body.appendChild(notification);

        setTimeout(() => {
            notification.remove();
        }, 3000);
    }

    // Remove unused CSS and optimize performance
    const unusedElements = document.querySelectorAll('.animate__animated, .fade-in');
    unusedElements.forEach(element => {
        element.classList.remove('animate__animated', 'fade-in');
    });

    // Preload critical resources
    const criticalImages = [
        '/images/ASUS TUF Gaming F15 Gaming Laptop.jpeg',
        '/images/lab.jpg'
    ];

    criticalImages.forEach(src => {
        const link = document.createElement('link');
        link.rel = 'preload';
        link.as = 'image';
        link.href = src;
        document.head.appendChild(link);
    });
});

// Simplified carousel without heavy animations
function initSimpleCarousel() {
    const carousels = document.querySelectorAll('.simple-carousel');
    carousels.forEach(carousel => {
        const items = carousel.querySelectorAll('.carousel-item');
        const prevBtn = carousel.querySelector('.carousel-prev');
        const nextBtn = carousel.querySelector('.carousel-next');
        let currentIndex = 0;

        function showItem(index) {
            items.forEach((item, i) => {
                item.style.display = i === index ? 'block' : 'none';
            });
        }

        if (prevBtn) {
            prevBtn.addEventListener('click', () => {
                currentIndex = (currentIndex - 1 + items.length) % items.length;
                showItem(currentIndex);
            });
        }

        if (nextBtn) {
            nextBtn.addEventListener('click', () => {
                currentIndex = (currentIndex + 1) % items.length;
                showItem(currentIndex);
            });
        }

        showItem(0);
    }
}

// Initialize optimized components
document.addEventListener('DOMContentLoaded', initSimpleCarousel);
