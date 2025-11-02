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

    // Cart Management
    let cartCount = 0;
    
    function updateCartCount() {
        const cartBadge = document.querySelector('.navbar .badge.bg-danger');
        if (cartBadge) {
            cartBadge.textContent = cartCount;
            cartBadge.style.display = cartCount > 0 ? 'block' : 'none';
        }
    }

    // Add to Cart functionality
    const addToCartForms = document.querySelectorAll('.add-to-cart-form');
    addToCartForms.forEach(form => {
        form.addEventListener('submit', function (e) {
            e.preventDefault();
            const button = form.querySelector('button[type="submit"]');
            const productId = form.querySelector('input[name="productId"]').value;
            
            // Show loading state
            const originalContent = button.innerHTML;
            button.disabled = true;
            button.innerHTML = '<span class="spinner-border spinner-border-sm me-2"></span>Adding...';
            
            // Simulate API call
            setTimeout(() => {
                button.disabled = false;
                button.innerHTML = '<i class="bi bi-check-circle me-1"></i> Added!';
                cartCount++;
                updateCartCount();
                
                // Show success notification
                showNotification('Product added to cart successfully!', 'success');
                
                // Reset button after delay
                setTimeout(() => {
                    button.innerHTML = originalContent;
                }, 2000);
            }, 1000);
        });
    });

    // Wishlist functionality
    const wishlistButtons = document.querySelectorAll('[title="Add to Wishlist"]');
    wishlistButtons.forEach(button => {
        button.addEventListener('click', function() {
            const icon = this.querySelector('i');
            if (icon.classList.contains('bi-heart')) {
                icon.classList.remove('bi-heart');
                icon.classList.add('bi-heart-fill');
                this.classList.add('text-danger');
                showNotification('Added to wishlist!', 'success');
            } else {
                icon.classList.remove('bi-heart-fill');
                icon.classList.add('bi-heart');
                this.classList.remove('text-danger');
                showNotification('Removed from wishlist', 'info');
            }
        });
    });

    // Quick View functionality
    const quickViewButtons = document.querySelectorAll('[title="Quick View"]');
    quickViewButtons.forEach(button => {
        button.addEventListener('click', function() {
            // Implement quick view modal
            showNotification('Quick view feature coming soon!', 'info');
        });
    });

    // Search functionality with autocomplete
    const searchInput = document.querySelector('input[name="searchString"]');
    if (searchInput) {
        let searchTimeout;
        
        searchInput.addEventListener('input', debounce(function (e) {
            const searchTerm = e.target.value.trim();
            if (searchTerm.length > 2) {
                // Implement search suggestions
                console.log('Searching for:', searchTerm);
            }
        }, 300));

        searchInput.addEventListener('focus', function() {
            this.parentElement.classList.add('search-focused');
        });

        searchInput.addEventListener('blur', function() {
            setTimeout(() => {
                this.parentElement.classList.remove('search-focused');
            }, 200);
        });
    }

    // Product image zoom on hover
    const productImages = document.querySelectorAll('.product-card img');
    productImages.forEach(img => {
        img.addEventListener('mouseenter', function() {
            this.style.transform = 'scale(1.1)';
            this.style.transition = 'transform 0.3s ease';
        });

        img.addEventListener('mouseleave', function() {
            this.style.transform = 'scale(1)';
        });
    });

    // Smooth scroll for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({
                    behavior: 'smooth',
                    block: 'start'
                });
            }
        });
    });

    // Lazy loading for images
    const lazyImages = document.querySelectorAll('img[data-src]');
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

    lazyImages.forEach(img => imageObserver.observe(img));

    // Back to top button
    const backToTopButton = document.createElement('button');
    backToTopButton.innerHTML = '<i class="bi bi-arrow-up"></i>';
    backToTopButton.className = 'btn btn-primary back-to-top';
    backToTopButton.style.cssText = `
        position: fixed;
        bottom: 20px;
        right: 20px;
        z-index: 1000;
        display: none;
        border-radius: 50%;
        width: 50px;
        height: 50px;
        box-shadow: 0 4px 12px rgba(0,0,0,0.15);
    `;
    document.body.appendChild(backToTopButton);

    window.addEventListener('scroll', () => {
        if (window.pageYOffset > 300) {
            backToTopButton.style.display = 'block';
        } else {
            backToTopButton.style.display = 'none';
        }
    });

    backToTopButton.addEventListener('click', () => {
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    });

    // Newsletter subscription
    const newsletterForm = document.querySelector('.newsletter-form');
    if (newsletterForm) {
        newsletterForm.addEventListener('submit', function(e) {
            e.preventDefault();
            const email = this.querySelector('input[type="email"]').value;
            showNotification(`Successfully subscribed with ${email}!`, 'success');
            this.reset();
        });
    }

    // Price range slider (if present)
    const priceRange = document.querySelector('.price-range');
    if (priceRange) {
        const minPrice = priceRange.querySelector('.min-price');
        const maxPrice = priceRange.querySelector('.max-price');
        // Implement price range functionality
    }

    // Product comparison
    const compareButtons = document.querySelectorAll('[title="Compare"]');
    let compareList = [];
    
    compareButtons.forEach(button => {
        button.addEventListener('click', function() {
            const productId = this.dataset.productId;
            if (compareList.length < 3) {
                compareList.push(productId);
                this.classList.add('active');
                showNotification('Product added to comparison', 'success');
            } else {
                showNotification('Maximum 3 products can be compared', 'warning');
            }
        });
    });

    // Initialize
    updateCartCount();
});

// Utility Functions
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

function showNotification(message, type = 'info') {
    const notification = document.createElement('div');
    notification.className = `alert alert-${type} position-fixed top-0 end-0 m-3`;
    notification.style.cssText = `
        z-index: 9999;
        min-width: 300px;
        animation: slideInRight 0.3s ease;
    `;
    notification.innerHTML = `
        <div class="d-flex align-items-center">
            <i class="bi bi-${type === 'success' ? 'check-circle' : type === 'warning' ? 'exclamation-triangle' : 'info-circle'} me-2"></i>
            ${message}
        </div>
    `;
    
    document.body.appendChild(notification);
    
    setTimeout(() => {
        notification.style.animation = 'slideOutRight 0.3s ease';
        setTimeout(() => notification.remove(), 300);
    }, 3000);
}

// Add CSS animations
const style = document.createElement('style');
style.textContent = `
    @keyframes slideInRight {
        from {
            transform: translateX(100%);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }
    
    @keyframes slideOutRight {
        from {
            transform: translateX(0);
            opacity: 1;
        }
        to {
            transform: translateX(100%);
            opacity: 0;
        }
    }
    
    .search-focused {
        transform: scale(1.02);
        box-shadow: 0 4px 12px rgba(37, 99, 235, 0.15);
    }
    
    .back-to-top:hover {
        transform: translateY(-2px);
        box-shadow: 0 6px 16px rgba(0,0,0,0.2);
    }
`;
document.head.appendChild(style);

// Export functions for global use
window.EcommerceUtils = {
    showNotification,
    debounce,
    updateCartCount: () => {
        // Cart count update logic
    }
};

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
