/**
 * Gujarat Farmers Portal - Admin Dashboard JavaScript
 * Interactive dashboard with charts, real-time updates, and animations
 */

window.AdminDashboard = {
    // Configuration
    config: {
        refreshInterval: 300000, // 5 minutes
        animationDuration: 300,
        chartColors: {
            primary: '#667eea',
            secondary: '#764ba2',
            success: '#27ae60',
            warning: '#f39c12',
            danger: '#e74c3c',
            info: '#3498db'
        },
        gradients: {},
        charts: {}
    },

    // Initialize the dashboard
    init: function () {
        this.initializeGradients();
        this.setupEventListeners();
        this.initializeCounters();
        this.startPeriodicRefresh();
        console.log('Admin Dashboard initialized successfully');
    },

    // Initialize chart gradients
    initializeGradients: function () {
        // This will be called when charts are created
        this.config.gradients = {
            primary: null,
            success: null,
            warning: null,
            info: null
        };
    },

    // Create gradient for charts
    createGradient: function (ctx, color1, color2) {
        const gradient = ctx.createLinearGradient(0, 0, 0, 400);
        gradient.addColorStop(0, color1);
        gradient.addColorStop(1, color2);
        return gradient;
    },

    // Setup event listeners
    setupEventListeners: function () {
        // Refresh button clicks
        document.addEventListener('click', (e) => {
            if (e.target.matches('[onclick*="refreshChart"]')) {
                const chartType = e.target.getAttribute('onclick').match(/refreshChart\('(.+)'\)/)[1];
                this.refreshChart(chartType);
            }
        });

        // Stats card hover effects
        document.querySelectorAll('.stats-card').forEach(card => {
            card.addEventListener('mouseenter', this.animateStatsCard);
            card.addEventListener('mouseleave', this.resetStatsCard);
        });

        // Activity item interactions
        document.querySelectorAll('.activity-item').forEach(item => {
            item.addEventListener('click', this.handleActivityClick);
        });

        // Auto-refresh toggle
        this.setupAutoRefreshToggle();
    },

    // Initialize counter animations
    initializeCounters: function () {
        const counters = document.querySelectorAll('.stats-number');

        const observerOptions = {
            threshold: 0.5,
            rootMargin: '0px 0px -50px 0px'
        };

        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    this.animateCounter(entry.target);
                    observer.unobserve(entry.target);
                }
            });
        }, observerOptions);

        counters.forEach(counter => observer.observe(counter));
    },

    // Animate counter numbers
    animateCounter: function (element) {
        const target = parseInt(element.textContent.replace(/,/g, ''));
        const duration = 2000;
        const step = target / (duration / 16);
        let current = 0;

        const timer = setInterval(() => {
            current += step;
            if (current >= target) {
                current = target;
                clearInterval(timer);
            }
            element.textContent = Math.floor(current).toLocaleString('gu-IN');
        }, 16);
    },

    // Animate stats card on hover
    animateStatsCard: function (e) {
        const card = e.currentTarget;
        const icon = card.querySelector('.stats-icon');

        if (icon) {
            icon.style.transform = 'scale(1.1) rotate(5deg)';
            icon.style.transition = 'all 0.3s ease';
        }
    },

    // Reset stats card animation
    resetStatsCard: function (e) {
        const card = e.currentTarget;
        const icon = card.querySelector('.stats-icon');

        if (icon) {
            icon.style.transform = 'scale(1) rotate(0deg)';
        }
    },

    // Handle activity item clicks
    handleActivityClick: function (e) {
        const item = e.currentTarget;
        const link = item.querySelector('.activity-actions a');

        if (link && !e.target.closest('.activity-actions')) {
            window.location.href = link.href;
        }
    },

    // Initialize User Registration Chart
    initUserRegistrationChart: function (data) {
        const ctx = document.getElementById('userRegistrationChart');
        if (!ctx) return;

        // Process data for chart
        const chartData = this.processUserRegistrationData(data);

        // Create gradient
        const gradient = this.createGradient(ctx.getContext('2d'),
            this.config.chartColors.primary + '80',
            this.config.chartColors.primary + '20');

        const config = {
            type: 'line',
            data: {
                labels: chartData.labels,
                datasets: [{
                    label: 'નવા વપરાશકર્તાઓ',
                    data: chartData.values,
                    borderColor: this.config.chartColors.primary,
                    backgroundColor: gradient,
                    borderWidth: 3,
                    fill: true,
                    tension: 0.4,
                    pointBackgroundColor: this.config.chartColors.primary,
                    pointBorderColor: '#ffffff',
                    pointBorderWidth: 2,
                    pointRadius: 6,
                    pointHoverRadius: 8,
                    pointHoverBackgroundColor: this.config.chartColors.secondary,
                    pointHoverBorderColor: '#ffffff',
                    pointHoverBorderWidth: 3
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        display: false
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0, 0, 0, 0.8)',
                        titleColor: '#ffffff',
                        bodyColor: '#ffffff',
                        borderColor: this.config.chartColors.primary,
                        borderWidth: 1,
                        cornerRadius: 8,
                        displayColors: false,
                        callbacks: {
                            title: function (context) {
                                return context[0].label;
                            },
                            label: function (context) {
                                return `${context.parsed.y} નવા વપરાશકર્તાઓ`;
                            }
                        }
                    }
                },
                scales: {
                    x: {
                        display: true,
                        grid: {
                            display: false
                        },
                        ticks: {
                            color: '#7f8c8d',
                            font: {
                                size: 12
                            }
                        }
                    },
                    y: {
                        display: true,
                        grid: {
                            color: 'rgba(0, 0, 0, 0.05)',
                            drawBorder: false
                        },
                        ticks: {
                            color: '#7f8c8d',
                            font: {
                                size: 12
                            },
                            callback: function (value) {
                                return value.toLocaleString('gu-IN');
                            }
                        }
                    }
                },
                interaction: {
                    intersect: false,
                    mode: 'index'
                },
                animation: {
                    duration: 2000,
                    easing: 'easeInOutQuart'
                }
            }
        };

        this.config.charts.userChart = new Chart(ctx, config);
    },

    // Process user registration data
    processUserRegistrationData: function (data) {
        if (!data || data.length === 0) {
            // Default data if no data available
            const currentDate = new Date();
            const labels = [];
            const values = [];

            for (let i = 11; i >= 0; i--) {
                const date = new Date(currentDate.getFullYear(), currentDate.getMonth() - i, 1);
                const monthName = date.toLocaleDateString('gu-IN', { month: 'short' });
                labels.push(monthName);
                values.push(Math.floor(Math.random() * 50) + 10);
            }

            return { labels, values };
        }

        const labels = data.map(item => {
            const monthNames = ['જાન', 'ફેબ', 'માર', 'એપ્ર', 'મે', 'જૂન', 'જુલ', 'ઓગ', 'સેપ', 'ઓક્ટ', 'નવે', 'ડિસે'];
            return monthNames[item.Month - 1] || 'અજ્ઞાત';
        });

        const values = data.map(item => item.UserCount || 0);

        return { labels, values };
    },

    // Initialize Category Performance Chart
    initCategoryChart: function (data) {
        const ctx = document.getElementById('categoryChart');
        if (!ctx) return;

        const chartData = this.processCategoryData(data);

        const config = {
            type: 'doughnut',
            data: {
                labels: chartData.labels,
                datasets: [{
                    data: chartData.values,
                    backgroundColor: [
                        this.config.chartColors.primary,
                        this.config.chartColors.success,
                        this.config.chartColors.warning,
                        this.config.chartColors.info,
                        this.config.chartColors.secondary,
                        this.config.chartColors.danger
                    ],
                    borderColor: '#ffffff',
                    borderWidth: 3,
                    hoverOffset: 10
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                plugins: {
                    legend: {
                        position: 'bottom',
                        labels: {
                            usePointStyle: true,
                            pointStyle: 'circle',
                            padding: 20,
                            font: {
                                size: 12
                            },
                            color: '#7f8c8d'
                        }
                    },
                    tooltip: {
                        backgroundColor: 'rgba(0, 0, 0, 0.8)',
                        titleColor: '#ffffff',
                        bodyColor: '#ffffff',
                        borderColor: this.config.chartColors.primary,
                        borderWidth: 1,
                        cornerRadius: 8,
                        callbacks: {
                            label: function (context) {
                                const total = context.dataset.data.reduce((a, b) => a + b, 0);
                                const percentage = ((context.parsed * 100) / total).toFixed(1);
                                return `${context.label}: ${context.parsed} (${percentage}%)`;
                            }
                        }
                    }
                },
                animation: {
                    animateRotate: true,
                    animateScale: false,
                    duration: 2000,
                    easing: 'easeInOutQuart'
                },
                cutout: '60%'
            }
        };

        this.config.charts.categoryChart = new Chart(ctx, config);
    },

    // Process category data
    processCategoryData: function (data) {
        if (!data || data.length === 0) {
            // Default data if no data available
            return {
                labels: ['ખેત પેદાશ', 'પશુધન', 'જમીન', 'વાહનો', 'સાધનો'],
                values: [45, 25, 15, 10, 5]
            };
        }

        const labels = data.map(item => item.CategoryNameGuj || item.CategoryName || 'અજ્ઞાત');
        const values = data.map(item => item.PostCount || 0);

        return { labels, values };
    },

    // Refresh specific chart
    refreshChart: function (chartType) {
        const button = event.target;
        const originalHtml = button.innerHTML;

        // Show loading state
        button.innerHTML = '<i class="fas fa-spinner fa-spin"></i> લોડ થઈ રહ્યું...';
        button.disabled = true;

        // Simulate API call
        setTimeout(() => {
            switch (chartType) {
                case 'userChart':
                    this.refreshUserChart();
                    break;
                case 'categoryChart':
                    this.refreshCategoryChart();
                    break;
            }

            // Reset button
            button.innerHTML = originalHtml;
            button.disabled = false;

            // Show success message
            this.showNotification('ચાર્ટ અપડેટ થઈ ગયો', 'success');
        }, 1500);
    },

    // Refresh user chart data
    refreshUserChart: function () {
        if (!this.config.charts.userChart) return;

        // Simulate new data
        const newData = this.generateRandomUserData();
        this.config.charts.userChart.data.datasets[0].data = newData.values;
        this.config.charts.userChart.update('active');
    },

    // Refresh category chart data
    refreshCategoryChart: function () {
        if (!this.config.charts.categoryChart) return;

        // Simulate new data
        const newData = this.generateRandomCategoryData();
        this.config.charts.categoryChart.data.datasets[0].data = newData.values;
        this.config.charts.categoryChart.update('active');
    },

    // Generate random user data for demo
    generateRandomUserData: function () {
        const values = [];
        for (let i = 0; i < 12; i++) {
            values.push(Math.floor(Math.random() * 50) + 10);
        }
        return { values };
    },

    // Generate random category data for demo
    generateRandomCategoryData: function () {
        const values = [];
        for (let i = 0; i < 5; i++) {
            values.push(Math.floor(Math.random() * 40) + 10);
        }
        return { values };
    },

    // Setup auto-refresh functionality
    setupAutoRefreshToggle: function () {
        // Add auto-refresh toggle if needed
        this.autoRefreshEnabled = true;
    },

    // Start periodic refresh
    startPeriodicRefresh: function () {
        if (this.refreshTimer) {
            clearInterval(this.refreshTimer);
        }

        this.refreshTimer = setInterval(() => {
            if (this.autoRefreshEnabled && !document.hidden) {
                this.refreshStats();
            }
        }, this.config.refreshInterval);
    },

    // Refresh dashboard stats
    refreshStats: function () {
        // Make API call to refresh dashboard data
        fetch('/api/admin/dashboard')
            .then(response => response.json())
            .then(data => {
                if (data.success) {
                    this.updateDashboardStats(data.data);
                    this.showNotification('ડેશબોર્ડ અપડેટ થઈ ગયો', 'success');
                }
            })
            .catch(error => {
                console.error('Error refreshing dashboard:', error);
                this.showNotification('ડેશબોર્ડ અપડેટ કરવામાં ભૂલ આવી', 'error');
            });
    },

    // Update dashboard statistics
    updateDashboardStats: function (data) {
        // Update stats numbers with animation
        this.updateStatCard('.users-card .stats-number', data.totalUsers);
        this.updateStatCard('.posts-card .stats-number', data.totalPosts);
        this.updateStatCard('.pending-card .stats-number', data.pendingPosts);
        this.updateStatCard('.categories-card .stats-number', data.totalCategories);

        // Update change badges
        this.updateChangeBadge('.users-card .change-badge', `+${data.todayUsers} આજે`);
        this.updateChangeBadge('.posts-card .change-badge', `+${data.todayPosts} આજે`);
    },

    // Update individual stat card
    updateStatCard: function (selector, newValue) {
        const element = document.querySelector(selector);
        if (element) {
            const currentValue = parseInt(element.textContent.replace(/,/g, ''));

            if (currentValue !== newValue) {
                // Animate the change
                this.animateValueChange(element, currentValue, newValue);
            }
        }
    },

    // Update change badge
    updateChangeBadge: function (selector, newText) {
        const element = document.querySelector(selector);
        if (element) {
            element.innerHTML = `<i class="fas fa-arrow-up"></i> ${newText}`;
        }
    },

    // Animate value change
    animateValueChange: function (element, fromValue, toValue) {
        const duration = 1000;
        const startTime = performance.now();

        const animate = (currentTime) => {
            const elapsed = currentTime - startTime;
            const progress = Math.min(elapsed / duration, 1);

            const currentValue = Math.floor(fromValue + (toValue - fromValue) * progress);
            element.textContent = currentValue.toLocaleString('gu-IN');

            if (progress < 1) {
                requestAnimationFrame(animate);
            }
        };

        requestAnimationFrame(animate);
    },

    // Show notification
    showNotification: function (message, type = 'info') {
        // Use the global showToast function from master-layout.js
        if (typeof window.showToast === 'function') {
            window.showToast(message, type);
        } else {
            console.log(`${type.toUpperCase()}: ${message}`);
        }
    },

    // Real-time updates via WebSocket (for future implementation)
    initWebSocket: function () {
        // Future implementation for real-time updates
        // const ws = new WebSocket('ws://localhost:5000/adminHub');
        // ws.onmessage = (event) => {
        //     const data = JSON.parse(event.data);
        //     this.handleRealtimeUpdate(data);
        // };
    },

    // Handle real-time updates
    handleRealtimeUpdate: function (data) {
        switch (data.type) {
            case 'new_user':
                this.updateStatCard('.users-card .stats-number', data.totalUsers);
                this.showNotification('નવો વપરાશકર્તા જોડાયો', 'success');
                break;
            case 'new_post':
                this.updateStatCard('.posts-card .stats-number', data.totalPosts);
                this.showNotification('નવી પોસ્ટ આવી', 'info');
                break;
            case 'pending_approval':
                this.updateStatCard('.pending-card .stats-number', data.pendingPosts);
                this.showNotification('નવી પોસ્ટ મંજૂરી માટે', 'warning');
                break;
        }
    },

    // Export dashboard data
    exportDashboard: function () {
        const dashboardData = {
            timestamp: new Date().toISOString(),
            stats: {
                totalUsers: document.querySelector('.users-card .stats-number')?.textContent,
                totalPosts: document.querySelector('.posts-card .stats-number')?.textContent,
                pendingPosts: document.querySelector('.pending-card .stats-number')?.textContent,
                totalCategories: document.querySelector('.categories-card .stats-number')?.textContent
            },
            charts: {
                userRegistration: this.config.charts.userChart?.data,
                categoryPerformance: this.config.charts.categoryChart?.data
            }
        };

        const dataStr = JSON.stringify(dashboardData, null, 2);
        const dataBlob = new Blob([dataStr], { type: 'application/json' });

        const link = document.createElement('a');
        link.href = URL.createObjectURL(dataBlob);
        link.download = `dashboard-data-${new Date().toISOString().split('T')[0]}.json`;
        link.click();

        this.showNotification('ડેશબોર્ડ ડેટા એક્સપોર્ટ થઈ ગયો', 'success');
    },

    // Print dashboard
    printDashboard: function () {
        window.print();
    },

    // Destroy charts and cleanup
    destroy: function () {
        // Clear refresh timer
        if (this.refreshTimer) {
            clearInterval(this.refreshTimer);
        }

        // Destroy charts
        Object.values(this.config.charts).forEach(chart => {
            if (chart && typeof chart.destroy === 'function') {
                chart.destroy();
            }
        });

        console.log('Admin Dashboard destroyed');
    },

    // Utility functions
    utils: {
        // Format number in Gujarati locale
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

        // Get time ago in Gujarati
        getTimeAgo: function (date) {
            const now = new Date();
            const diff = now - new Date(date);
            const minutes = Math.floor(diff / 60000);

            if (minutes < 1) return 'હમણાં જ';
            if (minutes < 60) return `${minutes} મિનિટ પહેલા`;

            const hours = Math.floor(minutes / 60);
            if (hours < 24) return `${hours} કલાક પહેલા`;

            const days = Math.floor(hours / 24);
            if (days < 7) return `${days} દિવસ પહેલા`;

            return new Date(date).toLocaleDateString('gu-IN');
        },

        // Debounce function
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
        }
    }
};

// Global functions for easy access
window.refreshChart = function (chartType) {
    AdminDashboard.refreshChart(chartType);
};

window.exportDashboard = function () {
    AdminDashboard.exportDashboard();
};

window.printDashboard = function () {
    AdminDashboard.printDashboard();
};

// Initialize dashboard when DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', function () {
        AdminDashboard.init();
    });
} else {
    AdminDashboard.init();
}

// Cleanup on page unload
window.addEventListener('beforeunload', function () {
    AdminDashboard.destroy();
});