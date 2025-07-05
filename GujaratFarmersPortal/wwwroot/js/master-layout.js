/**
 * Gujarat Farmers Portal - Master Layout JavaScript
 * Interactive and responsive functionality for the application
 */

// Application Object
window.App = {
    // Configuration
    config: {
        animationDuration: 300,
        debounceDelay: 300,
        scrollThreshold: 100,
        notificationTimeout: 5000,
        searchMinLength: 2,
        cacheExpiry: 300000 // 5 minutes
    },

    // State management
    state: {
        sidebarOpen: false,
        fabMenuOpen: false,
        currentTheme: 'light',
        currentLanguage: 'gu',
        notifications: [],
        searchCache: new Map()
    },

    // Initialize the application
    init: function () {
        this.initEventListeners();
        this.initTheme();
        this.initLanguage();
        this.initAnimations();
        this.loadNotifications();
        this.startPeriodicTasks();
        console.log('Gujarat Farmers Portal initialized successfully');
    },

    // Event Listeners
    initEventListeners: function () {
        // Sidebar toggle
        this.bindEvent('#sidebarToggle', 'click', this.toggleSidebar.bind(this));

        // Theme toggle
        this.bindEvent('#themeToggle', 'click', this.toggleTheme.bind(this));

        // Language toggle
        this.bindEvents('[data-lang]', 'click', this.changeLanguage.bind(this));

        // Search functionality
        this.bindEvent('.search-input', 'input', this.debounce(this.handleSearch.bind(this), this.config.debounceDelay));
        this.bindEvent('.search-input', 'focus', this.showSearchSuggestions.bind(this));
        this.bindEvent('.search-input', 'blur', this.hideSearchSuggestions.bind(this));

        // Floating Action Button
        this.bindEvent('#fabMain', 'click', this.toggleFabMenu.bind(this));

        // Back to top
        this.bindEvent('#backToTop', 'click', this.scrollToTop.bind(this));

        // Scroll events
        window.addEventListener('scroll', this.debounce(this.handleScroll.bind(this), 100));

        // Notification management
        this.bindEvent('.mark-all-read', 'click', this.markAllNotificationsRead.bind(this));
        this.bindEvents('.notification-item', 'click', this.markNotificationRead.bind(this));

        // Close dropdowns when clicking outside
        document.addEventListener('click', this.closeDropdowns.bind(this));

        // Keyboard shortcuts
        document.addEventListener('keydown', this.handleKeyboardShortcuts.bind(this));

        // Window resize
        window.addEventListener('resize', this.debounce(this.handleResize.bind(this), 250));

        // Page visibility change
        document.addEventListener('visibilitychange', this.handleVisibilityChange.bind(this));
    },

    // Theme Management
    initTheme: function () {
        const savedTheme = localStorage.getItem('theme') || 'light';
        this.setTheme(savedTheme);
    },

    toggleTheme: function () {
        const currentTheme = document.body.getAttribute('data-theme') || 'light';
        const newTheme = currentTheme === 'light' ? 'dark' : 'light';
        this.setTheme(newTheme);
    },

    setTheme: function (theme) {
        document.body.setAttribute('data-theme', theme);
        localStorage.setItem('theme', theme);
        this.state.currentTheme = theme;

        // Update theme icon
        const themeIcon = document.getElementById('themeIcon');
        if (themeIcon) {
            themeIcon.className = theme === 'light' ? 'fas fa-moon' : 'fas fa-sun';
        }

        // Animate theme change
        document.body.style.transition = 'background-color 0.3s ease, color 0.3s ease';
        setTimeout(() => {
            document.body.style.transition = '';
        }, 300);

        this.showToast(theme === 'light' ? 'લાઇટ થીમ સક્રિય કર્યું' : 'ડાર્ક થીમ સક્રિય કર્યું', 'success');
    },

    // Language Management
    initLanguage: function () {
        const savedLanguage = localStorage.getItem('language') || 'gu';
        this.setLanguage(savedLanguage);
    },

    changeLanguage: function (event) {
        event.preventDefault();
        const language = event.target.getAttribute('data-lang');
        this.setLanguage(language);
    },

    setLanguage: function (language) {
        localStorage.setItem('language', language);
        this.state.currentLanguage = language;

        // Update language indicators
        document.querySelectorAll('[data-lang]').forEach(element => {
            const icon = element.querySelector('i');
            if (element.getAttribute('data-lang') === language) {
                icon.className = 'fas fa-check text-success me-2';
            } else {
                icon.className = 'fas fa-circle me-2 text-muted';
            }
        });

        // Show language change notification
        const languageNames = {
            'gu': 'ગુજરાતી',
            'en': 'English',
            'hi': 'हिंदी'
        };

        this.showToast(`ભાષા બદલાઈ: ${languageNames[language]}`, 'info');

        // Here you would typically reload content in the new language
        // For now, we'll just update the UI indicators
    },

    // Sidebar Management
    toggleSidebar: function () {
        const sidebar = document.getElementById('sidebar');
        if (!sidebar) return;

        this.state.sidebarOpen = !this.state.sidebarOpen;

        if (this.state.sidebarOpen) {
            sidebar.classList.add('show');
            document.body.style.overflow = 'hidden'; // Prevent background scroll on mobile
        } else {
            sidebar.classList.remove('show');
            document.body.style.overflow = '';
        }
    },

    // Search Functionality
    handleSearch: function (event) {
        const query = event.target.value.trim();

        if (query.length < this.config.searchMinLength) {
            this.hideSearchSuggestions();
            return;
        }

        // Check cache first
        if (this.state.searchCache.has(query)) {
            const cached = this.state.searchCache.get(query);
            if (Date.now() - cached.timestamp < this.config.cacheExpiry) {
                this.displaySearchSuggestions(cached.data);
                return;
            }
        }

        // Fetch suggestions from server
        this.fetchSearchSuggestions(query);
    },

    fetchSearchSuggestions: function (query) {
        const searchSuggestions = document.getElementById('searchSuggestions');
        if (!searchSuggestions) return;

        // Show loading
        searchSuggestions.innerHTML = '<div class="search-loading">શોધી રહ્યા છીએ...</div>';
        searchSuggestions.style.display = 'block';

        // Simulate API call (replace with actual API endpoint)
        setTimeout(() => {
            const suggestions = this.generateMockSuggestions(query);

            // Cache the results
            this.state.searchCache.set(query, {
                data: suggestions,
                timestamp: Date.now()
            });

            this.displaySearchSuggestions(suggestions);
        }, 500);
    },

    generateMockSuggestions: function (query) {
        const mockData = [
            'ટ્રેક્ટર મહિન્દ્રા',
            'ગિર ગાય',
            'જમીન વેચાણ',
            'ભેંસ ખરીદી',
            'ખેતીના સાધનો'
        ];

        return mockData.filter(item =>
            item.toLowerCase().includes(query.toLowerCase())
        ).slice(0, 5);
    },

    displaySearchSuggestions: function (suggestions) {
        const searchSuggestions = document.getElementById('searchSuggestions');
        if (!searchSuggestions) return;

        if (suggestions.length === 0) {
            searchSuggestions.innerHTML = '<div class="search-no-results">કોઈ પરિણામ મળ્યું નથી</div>';
        } else {
            const html = suggestions.map(suggestion =>
                `<div class="search-suggestion-item" onclick="App.selectSuggestion('${suggestion}')">
                    <i class="fas fa-search"></i>
                    <span>${suggestion}</span>
                </div>`
            ).join('');

            searchSuggestions.innerHTML = html;
        }

        searchSuggestions.style.display = 'block';
    },

    selectSuggestion: function (suggestion) {
        const searchInput = document.querySelector('.search-input');
        if (searchInput) {
            searchInput.value = suggestion;
            this.hideSearchSuggestions();
            // Trigger search
            searchInput.closest('form').submit();
        }
    },

    showSearchSuggestions: function () {
        const searchSuggestions = document.getElementById('searchSuggestions');
        if (searchSuggestions && searchSuggestions.innerHTML.trim()) {
            searchSuggestions.style.display = 'block';
        }
    },

    hideSearchSuggestions: function () {
        setTimeout(() => {
            const searchSuggestions = document.getElementById('searchSuggestions');
            if (searchSuggestions) {
                searchSuggestions.style.display = 'none';
            }
        }, 200);
    },

    // Floating Action Button
    toggleFabMenu: function () {
        const fabContainer = document.querySelector('.floating-action-menu');
        if (!fabContainer) return;

        this.state.fabMenuOpen = !this.state.fabMenuOpen;

        if (this.state.fabMenuOpen) {
            fabContainer.classList.add('active');
        } else {
            fabContainer.classList.remove('active');
        }
    },

    // Scroll Management
    handleScroll: function () {
        const scrollTop = window.pageYOffset || document.documentElement.scrollTop;

        // Back to top button
        const backToTop = document.getElementById('backToTop');
        if (backToTop) {
            if (scrollTop > this.config.scrollThreshold) {
                backToTop.classList.add('show');
            } else {
                backToTop.classList.remove('show');
            }
        }

        // Header shadow on scroll
        const header = document.getElementById('main-header');
        if (header) {
            if (scrollTop > 10) {
                header.style.boxShadow = '0 4px 20px rgba(0, 0, 0, 0.1)';
            } else {
                header.style.boxShadow = '0 2px 8px rgba(0, 0, 0, 0.06)';
            }
        }
    },

    scrollToTop: function () {
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    },

    // Notification Management
    loadNotifications: function () {
        // Simulate loading notifications
        this.state.notifications = [
            {
                id: 1,
                title: 'નવો સંદેશ',
                message: 'તમને નવો સંદેશ મળ્યો છે',
                type: 'message',
                read: false,
                time: new Date()
            },
            {
                id: 2,
                title: 'પોસ્ટ લાઇક',
                message: 'કોઈએ તમારી પોસ્ટને લાઇક કરી',
                type: 'like',
                read: false,
                time: new Date(Date.now() - 600000)
            }
        ];

        this.updateNotificationCount();
    },

    updateNotificationCount: function () {
        const unreadCount = this.state.notifications.filter(n => !n.read).length;
        const badge = document.getElementById('notificationCount');

        if (badge) {
            if (unreadCount > 0) {
                badge.textContent = unreadCount > 99 ? '99+' : unreadCount;
                badge.style.display = 'flex';
            } else {
                badge.style.display = 'none';
            }
        }
    },

    markNotificationRead: function (event) {
        const notificationItem = event.currentTarget;
        const notificationId = parseInt(notificationItem.getAttribute('data-id'));

        if (notificationId) {
            const notification = this.state.notifications.find(n => n.id === notificationId);
            if (notification) {
                notification.read = true;
                notificationItem.classList.remove('unread');
                this.updateNotificationCount();
            }
        }
    },

    markAllNotificationsRead: function (event) {
        event.preventDefault();

        this.state.notifications.forEach(notification => {
            notification.read = true;
        });

        document.querySelectorAll('.notification-item.unread').forEach(item => {
            item.classList.remove('unread');
        });

        this.updateNotificationCount();
        this.showToast('બધા નોટિફિકેશન વાંચ્યા તરીકે માર્ક કર્યા', 'success');
    },

    // Toast Notifications
    showToast: function (message, type = 'info', duration = null) {
        const toastContainer = document.getElementById('toastContainer');
        if (!toastContainer) return;

        const toastId = 'toast-' + Date.now();
        const icons = {
            success: 'fas fa-check-circle',
            error: 'fas fa-exclamation-circle',
            warning: 'fas fa-exclamation-triangle',
            info: 'fas fa-info-circle'
        };

        const colors = {
            success: 'text-success',
            error: 'text-danger',
            warning: 'text-warning',
            info: 'text-info'
        };

        const toastHtml = `
            <div id="${toastId}" class="toast align-items-center border-0" role="alert">
                <div class="d-flex">
                    <div class="toast-body d-flex align-items-center">
                        <i class="${icons[type]} ${colors[type]} me-2"></i>
                        ${message}
                    </div>
                    <button type="button" class="btn-close me-2 m-auto" data-bs-dismiss="toast"></button>
                </div>
            </div>
        `;

        toastContainer.insertAdjacentHTML('beforeend', toastHtml);

        const toastElement = document.getElementById(toastId);
        const toast = new bootstrap.Toast(toastElement, {
            delay: duration || this.config.notificationTimeout
        });

        toast.show();

        // Remove from DOM after hiding
        toastElement.addEventListener('hidden.bs.toast', function () {
            toastElement.remove();
        });
    },

    // Animation Management
    initAnimations: function () {
        // Animate elements on page load
        this.observeElements();

        // Add entrance animations to main elements
        setTimeout(() => {
            document.querySelectorAll('.page-content').forEach(element => {
                element.classList.add('animate-fade-in');
            });
        }, 100);
    },

    observeElements: function () {
        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    const element = entry.target;

                    if (element.classList.contains('animate-on-scroll')) {
                        element.classList.add('animate-fade-in');
                    }

                    observer.unobserve(element);
                }
            });
        }, {
            threshold: 0.1,
            rootMargin: '0px 0px -50px 0px'
        });

        document.querySelectorAll('.animate-on-scroll').forEach(element => {
            observer.observe(element);
        });
    },

    // Utility Functions
    bindEvent: function (selector, event, handler) {
        const element = document.querySelector(selector);
        if (element) {
            element.addEventListener(event, handler);
        }
    },

    bindEvents: function (selector, event, handler) {
        document.querySelectorAll(selector).forEach(element => {
            element.addEventListener(event, handler);
        });
    },

    debounce: function (func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    },

    throttle: function (func, limit) {
        let inThrottle;
        return function () {
            const args = arguments;
            const context = this;
            if (!inThrottle) {
                func.apply(context, args);
                inThrottle = true;
                setTimeout(() => inThrottle = false, limit);
            }
        };
    },

    // Close dropdowns when clicking outside
    closeDropdowns: function (event) {
        if (!event.target.closest('.dropdown')) {
            document.querySelectorAll('.dropdown-menu.show').forEach(menu => {
                const dropdown = bootstrap.Dropdown.getInstance(menu.previousElementSibling);
                if (dropdown) {
                    dropdown.hide();
                }
            });
        }

        // Close FAB menu if clicking outside
        if (!event.target.closest('.floating-action-menu') && this.state.fabMenuOpen) {
            this.toggleFabMenu();
        }

        // Close sidebar on mobile if clicking outside
        if (window.innerWidth < 992 && this.state.sidebarOpen &&
            !event.target.closest('#sidebar') &&
            !event.target.closest('#sidebarToggle')) {
            this.toggleSidebar();
        }
    },

    // Keyboard shortcuts
    handleKeyboardShortcuts: function (event) {
        // Ctrl/Cmd + K for search
        if ((event.ctrlKey || event.metaKey) && event.key === 'k') {
            event.preventDefault();
            const searchInput = document.querySelector('.search-input');
            if (searchInput) {
                searchInput.focus();
            }
        }

        // ESC to close modals/dropdowns
        if (event.key === 'Escape') {
            if (this.state.sidebarOpen) {
                this.toggleSidebar();
            }
            if (this.state.fabMenuOpen) {
                this.toggleFabMenu();
            }
        }

        // Alt + T for theme toggle
        if (event.altKey && event.key === 't') {
            event.preventDefault();
            this.toggleTheme();
        }
    },

    // Handle window resize
    handleResize: function () {
        // Close sidebar on desktop if window is resized to mobile
        if (window.innerWidth >= 992 && this.state.sidebarOpen) {
            this.state.sidebarOpen = false;
            document.body.style.overflow = '';
        }

        // Update search suggestions position
        this.hideSearchSuggestions();
    },

    // Handle page visibility change
    handleVisibilityChange: function () {
        if (document.hidden) {
            // Page is hidden - pause animations, stop polling
            this.pausePeriodicTasks();
        } else {
            // Page is visible - resume activities
            this.resumePeriodicTasks();
            this.loadNotifications(); // Refresh notifications
        }
    },

    // Periodic tasks
    startPeriodicTasks: function () {
        // Check for new notifications every 30 seconds
        this.notificationInterval = setInterval(() => {
            if (!document.hidden) {
                this.checkNewNotifications();
            }
        }, 30000);

        // Update relative time stamps every minute
        this.timeUpdateInterval = setInterval(() => {
            this.updateTimeStamps();
        }, 60000);
    },

    pausePeriodicTasks: function () {
        if (this.notificationInterval) {
            clearInterval(this.notificationInterval);
        }
        if (this.timeUpdateInterval) {
            clearInterval(this.timeUpdateInterval);
        }
    },

    resumePeriodicTasks: function () {
        this.startPeriodicTasks();
    },

    checkNewNotifications: function () {
        // In a real app, this would make an API call
        // For demo, we'll simulate occasional new notifications
        if (Math.random() < 0.1) { // 10% chance
            const newNotification = {
                id: Date.now(),
                title: 'નવી જાહેરાત',
                message: 'તમારા વિસ્તારમાં નવી જાહેરાત ઉમેરાઈ છે',
                type: 'new_post',
                read: false,
                time: new Date()
            };

            this.state.notifications.unshift(newNotification);
            this.updateNotificationCount();
            this.showToast('નવું નોટિફિકેશન મળ્યું!', 'info');
        }
    },

    updateTimeStamps: function () {
        document.querySelectorAll('[data-timestamp]').forEach(element => {
            const timestamp = parseInt(element.getAttribute('data-timestamp'));
            const timeAgo = this.formatTimeAgo(timestamp);
            element.textContent = timeAgo;
        });
    },

    formatTimeAgo: function (timestamp) {
        const now = Date.now();
        const diff = now - timestamp;

        const minutes = Math.floor(diff / 60000);
        const hours = Math.floor(diff / 3600000);
        const days = Math.floor(diff / 86400000);

        if (minutes < 1) return 'હમણાં જ';
        if (minutes < 60) return `${minutes} મિનિટ પહેલા`;
        if (hours < 24) return `${hours} કલાક પહેલા`;
        if (days < 7) return `${days} દિવસ પહેલા`;

        return new Date(timestamp).toLocaleDateString('gu-IN');
    },

    // API Helper Functions
    makeApiCall: function (url, options = {}) {
        const defaultOptions = {
            headers: {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            },
            credentials: 'same-origin'
        };

        return fetch(url, { ...defaultOptions, ...options })
            .then(response => {
                if (!response.ok) {
                    throw new Error(`HTTP error! status: ${response.status}`);
                }
                return response.json();
            })
            .catch(error => {
                console.error('API call failed:', error);
                this.showToast('નેટવર્ક એરર: કૃપા કરી ફરી પ્રયાસ કરો', 'error');
                throw error;
            });
    },

    // Form validation helpers
    validateForm: function (formElement) {
        let isValid = true;
        const inputs = formElement.querySelectorAll('input[required], select[required], textarea[required]');

        inputs.forEach(input => {
            if (!input.value.trim()) {
                this.showFieldError(input, 'આ ફીલ્ડ આવશ્યક છે');
                isValid = false;
            } else {
                this.clearFieldError(input);
            }
        });

        return isValid;
    },

    showFieldError: function (field, message) {
        field.classList.add('is-invalid');

        let errorElement = field.parentNode.querySelector('.invalid-feedback');
        if (!errorElement) {
            errorElement = document.createElement('div');
            errorElement.className = 'invalid-feedback';
            field.parentNode.appendChild(errorElement);
        }

        errorElement.textContent = message;
    },

    clearFieldError: function (field) {
        field.classList.remove('is-invalid');
        const errorElement = field.parentNode.querySelector('.invalid-feedback');
        if (errorElement) {
            errorElement.remove();
        }
    },

    // Local storage helpers
    setStorage: function (key, value) {
        try {
            localStorage.setItem(key, JSON.stringify(value));
        } catch (error) {
            console.warn('Failed to save to localStorage:', error);
        }
    },

    getStorage: function (key, defaultValue = null) {
        try {
            const value = localStorage.getItem(key);
            return value ? JSON.parse(value) : defaultValue;
        } catch (error) {
            console.warn('Failed to read from localStorage:', error);
            return defaultValue;
        }
    },

    removeStorage: function (key) {
        try {
            localStorage.removeItem(key);
        } catch (error) {
            console.warn('Failed to remove from localStorage:', error);
        }
    },

    // Image loading with fallback
    loadImageWithFallback: function (img, fallbackSrc) {
        img.onerror = function () {
            this.src = fallbackSrc || '/images/default-placeholder.png';
            this.onerror = null; // Prevent infinite loop
        };
    },

    // Format numbers for Gujarati locale
    formatNumber: function (number) {
        return new Intl.NumberFormat('gu-IN').format(number);
    },

    // Format currency
    formatCurrency: function (amount) {
        return new Intl.NumberFormat('gu-IN', {
            style: 'currency',
            currency: 'INR',
            minimumFractionDigits: 0
        }).format(amount);
    },

    // Copy to clipboard
    copyToClipboard: function (text) {
        if (navigator.clipboard) {
            navigator.clipboard.writeText(text).then(() => {
                this.showToast('ક્લિપબોર્ડમાં કૉપિ કર્યું', 'success');
            }).catch(() => {
                this.fallbackCopyToClipboard(text);
            });
        } else {
            this.fallbackCopyToClipboard(text);
        }
    },

    fallbackCopyToClipboard: function (text) {
        const textArea = document.createElement('textarea');
        textArea.value = text;
        textArea.style.position = 'fixed';
        textArea.style.left = '-999999px';
        textArea.style.top = '-999999px';
        document.body.appendChild(textArea);
        textArea.focus();
        textArea.select();

        try {
            document.execCommand('copy');
            this.showToast('ક્લિપબોર્ડમાં કૉપિ કર્યું', 'success');
        } catch (err) {
            this.showToast('કૉપિ કરવામાં નિષ્ફળ', 'error');
        }

        document.body.removeChild(textArea);
    },

    // Cleanup function
    destroy: function () {
        this.pausePeriodicTasks();

        // Remove event listeners
        document.removeEventListener('keydown', this.handleKeyboardShortcuts);
        document.removeEventListener('visibilitychange', this.handleVisibilityChange);
        window.removeEventListener('scroll', this.handleScroll);
        window.removeEventListener('resize', this.handleResize);

        console.log('Gujarat Farmers Portal destroyed');
    }
};

// CSS loading helper
function loadCSS(href) {
    const link = document.createElement('link');
    link.rel = 'stylesheet';
    link.href = href;
    document.head.appendChild(link);
}

// Global utility functions
window.showToast = function (message, type = 'info') {
    App.showToast(message, type);
};

window.formatCurrency = function (amount) {
    return App.formatCurrency(amount);
};

window.formatNumber = function (number) {
    return App.formatNumber(number);
};

// Initialize when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function () {
        App.init();
    });
} else {
    App.init();
}

// Clean up before page unload
window.addEventListener('beforeunload', function () {
    App.destroy();
});