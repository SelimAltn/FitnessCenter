/* ========================================
   FitnessCenter Site JavaScript
   Toast Notifications, Modal Confirmations, UX Utilities
   ======================================== */

(function () {
    'use strict';

    // ========================================
    // Toast Notification System
    // ========================================
    const ToastManager = {
        container: null,

        init: function () {
            // Create toast container if not exists
            if (!this.container) {
                this.container = document.createElement('div');
                this.container.className = 'toast-container-fc';
                this.container.id = 'toast-container';
                document.body.appendChild(this.container);
            }

            // Check for TempData messages and show them as toasts
            this.processTempDataToasts();
        },

        show: function (message, type = 'info', duration = 5000) {
            const toast = document.createElement('div');
            toast.className = `toast-fc toast-fc-${type}`;

            const icon = this.getIcon(type);

            toast.innerHTML = `
                <span class="toast-icon">${icon}</span>
                <span class="toast-message">${message}</span>
                <button type="button" class="toast-fc-close" aria-label="Kapat">&times;</button>
            `;

            // Add close functionality
            const closeBtn = toast.querySelector('.toast-fc-close');
            closeBtn.addEventListener('click', () => this.dismiss(toast));

            // Add to container
            this.container.appendChild(toast);

            // Auto dismiss
            if (duration > 0) {
                setTimeout(() => this.dismiss(toast), duration);
            }

            return toast;
        },

        dismiss: function (toast) {
            toast.classList.add('toast-fc-exit');
            setTimeout(() => {
                if (toast.parentNode) {
                    toast.parentNode.removeChild(toast);
                }
            }, 300);
        },

        getIcon: function (type) {
            const icons = {
                success: '<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M22 11.08V12a10 10 0 1 1-5.93-9.14"></path><polyline points="22 4 12 14.01 9 11.01"></polyline></svg>',
                danger: '<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="15" y1="9" x2="9" y2="15"></line><line x1="9" y1="9" x2="15" y2="15"></line></svg>',
                warning: '<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><path d="M10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"></path><line x1="12" y1="9" x2="12" y2="13"></line><line x1="12" y1="17" x2="12.01" y2="17"></line></svg>',
                info: '<svg xmlns="http://www.w3.org/2000/svg" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round"><circle cx="12" cy="12" r="10"></circle><line x1="12" y1="16" x2="12" y2="12"></line><line x1="12" y1="8" x2="12.01" y2="8"></line></svg>'
            };
            return icons[type] || icons.info;
        },

        processTempDataToasts: function () {
            // Look for toast data elements
            const toastDataElements = document.querySelectorAll('[data-toast-message]');
            toastDataElements.forEach(el => {
                const message = el.dataset.toastMessage;
                const type = el.dataset.toastType || 'info';
                if (message) {
                    this.show(message, type);
                }
                el.remove();
            });

            // Also convert existing alert elements to toasts
            const alertMappings = {
                'alert-success': 'success',
                'alert-danger': 'danger',
                'alert-warning': 'warning',
                'alert-info': 'info'
            };

            Object.keys(alertMappings).forEach(alertClass => {
                const alerts = document.querySelectorAll(`.alert.${alertClass}[data-convert-toast="true"]`);
                alerts.forEach(alert => {
                    const message = alert.textContent.trim();
                    if (message) {
                        this.show(message, alertMappings[alertClass]);
                    }
                    alert.remove();
                });
            });
        }
    };

    // ========================================
    // Modal Confirmation System
    // ========================================
    const ModalManager = {
        modalElement: null,

        init: function () {
            this.createModalTemplate();
            this.bindConfirmButtons();
        },

        createModalTemplate: function () {
            // Check if modal already exists
            if (document.getElementById('confirmModal')) return;

            const modalHtml = `
                <div class="modal fade modal-fc" id="confirmModal" tabindex="-1" aria-hidden="true">
                    <div class="modal-dialog modal-dialog-centered">
                        <div class="modal-content">
                            <div class="modal-header">
                                <h5 class="modal-title" id="confirmModalTitle">Onay</h5>
                                <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Kapat"></button>
                            </div>
                            <div class="modal-body" id="confirmModalBody">
                                Bu işlemi gerçekleştirmek istediğinize emin misiniz?
                            </div>
                            <div class="modal-footer">
                                <button type="button" class="btn-fc btn-fc-secondary" data-bs-dismiss="modal">İptal</button>
                                <button type="button" class="btn-fc btn-fc-danger" id="confirmModalConfirmBtn">Onayla</button>
                            </div>
                        </div>
                    </div>
                </div>
            `;

            document.body.insertAdjacentHTML('beforeend', modalHtml);
            this.modalElement = new bootstrap.Modal(document.getElementById('confirmModal'));
        },

        show: function (options) {
            const defaults = {
                title: 'Onay',
                message: 'Bu işlemi gerçekleştirmek istediğinize emin misiniz?',
                confirmText: 'Onayla',
                confirmClass: 'btn-fc-danger',
                onConfirm: null
            };

            const settings = { ...defaults, ...options };

            const modal = document.getElementById('confirmModal');
            modal.querySelector('#confirmModalTitle').textContent = settings.title;
            modal.querySelector('#confirmModalBody').textContent = settings.message;

            const confirmBtn = modal.querySelector('#confirmModalConfirmBtn');
            confirmBtn.textContent = settings.confirmText;
            confirmBtn.className = `btn-fc ${settings.confirmClass}`;

            // Remove previous event listener
            const newConfirmBtn = confirmBtn.cloneNode(true);
            confirmBtn.parentNode.replaceChild(newConfirmBtn, confirmBtn);

            // Add new event listener
            newConfirmBtn.addEventListener('click', () => {
                if (settings.onConfirm) {
                    settings.onConfirm();
                }
                this.modalElement.hide();
            });

            this.modalElement.show();
        },

        bindConfirmButtons: function () {
            // Find all buttons/forms with data-confirm attribute
            document.addEventListener('click', (e) => {
                const target = e.target.closest('[data-confirm]');
                if (!target) return;

                e.preventDefault();

                const message = target.dataset.confirm || 'Bu işlemi gerçekleştirmek istediğinize emin misiniz?';
                const title = target.dataset.confirmTitle || 'Onay';

                this.show({
                    title: title,
                    message: message,
                    confirmText: target.dataset.confirmText || 'Onayla',
                    confirmClass: target.dataset.confirmClass || 'btn-fc-danger',
                    onConfirm: () => {
                        // If it's a form submit button, submit the form
                        const form = target.closest('form');
                        if (form) {
                            form.submit();
                        } else if (target.href) {
                            window.location.href = target.href;
                        }
                    }
                });
            });
        }
    };

    // ========================================
    // Form Utilities
    // ========================================
    const FormUtils = {
        init: function () {
            this.addLoadingState();
            this.enhanceValidation();
        },

        addLoadingState: function () {
            document.querySelectorAll('form').forEach(form => {
                form.addEventListener('submit', function (e) {
                    const submitBtn = form.querySelector('[type="submit"]');
                    if (submitBtn && !submitBtn.classList.contains('btn-fc-loading')) {
                        submitBtn.classList.add('btn-fc-loading');
                        submitBtn.disabled = true;

                        // Store original text
                        submitBtn.dataset.originalText = submitBtn.textContent;
                        submitBtn.innerHTML = '<span class="visually-hidden">Yükleniyor...</span>';
                    }
                });
            });
        },

        enhanceValidation: function () {
            // Add is-invalid class to inputs with validation errors
            document.querySelectorAll('.text-danger').forEach(errorSpan => {
                if (errorSpan.textContent.trim()) {
                    const input = errorSpan.previousElementSibling;
                    if (input && (input.tagName === 'INPUT' || input.tagName === 'SELECT' || input.tagName === 'TEXTAREA')) {
                        input.classList.add('is-invalid');
                    }
                }
            });

            // Also check for field-validation-error class (MVC validation)
            document.querySelectorAll('.field-validation-error').forEach(errorSpan => {
                const input = errorSpan.previousElementSibling;
                if (input && (input.tagName === 'INPUT' || input.tagName === 'SELECT' || input.tagName === 'TEXTAREA')) {
                    input.classList.add('is-invalid');
                }
            });
        }
    };

    // ========================================
    // Table Enhancements
    // ========================================
    const TableUtils = {
        init: function () {
            this.addHoverEffect();
        },

        addHoverEffect: function () {
            document.querySelectorAll('.table-fc tbody tr').forEach(row => {
                row.style.cursor = 'default';
            });
        }
    };

    // ========================================
    // Navbar Active State
    // ========================================
    const NavUtils = {
        init: function () {
            this.setActiveNavItem();
        },

        setActiveNavItem: function () {
            const currentPath = window.location.pathname.toLowerCase();
            document.querySelectorAll('.navbar-nav .nav-link').forEach(link => {
                const href = link.getAttribute('href');
                if (href && href.toLowerCase() === currentPath) {
                    link.classList.add('active');
                }
            });
        }
    };

    // ========================================
    // Scroll Reveal Animation System
    // ========================================
    const ScrollReveal = {
        observer: null,

        init: function () {
            // Check for reduced motion preference
            if (window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
                return;
            }

            this.createObserver();
            this.observeElements();
        },

        createObserver: function () {
            const options = {
                root: null,
                rootMargin: '0px 0px -50px 0px',
                threshold: 0.1
            };

            this.observer = new IntersectionObserver((entries) => {
                entries.forEach(entry => {
                    if (entry.isIntersecting) {
                        entry.target.classList.add('revealed');
                        // Stop observing once revealed (run only once)
                        this.observer.unobserve(entry.target);
                    }
                });
            }, options);
        },

        observeElements: function () {
            // Select elements to animate
            const selectors = [
                '.card',
                '.table',
                '.alert:not(.toast-fc)',
                '.hero-section',
                '.stat-card',
                '.service-card'
            ];

            const elements = document.querySelectorAll(selectors.join(', '));

            elements.forEach((el, index) => {
                // Add reveal class for animation
                el.classList.add('reveal-element');

                // Add stagger delay based on position in viewport row
                const rect = el.getBoundingClientRect();
                const siblings = Array.from(el.parentElement?.children || []);
                const siblingIndex = siblings.indexOf(el);

                if (siblingIndex >= 0 && siblingIndex < 6) {
                    el.style.transitionDelay = `${siblingIndex * 0.1}s`;
                }

                this.observer.observe(el);
            });
        }
    };

    // ========================================
    // Enhanced Button Loading State
    // ========================================
    const ButtonLoadingManager = {
        init: function () {
            this.enhanceFormSubmissions();
        },

        enhanceFormSubmissions: function () {
            document.querySelectorAll('form').forEach(form => {
                form.addEventListener('submit', (e) => {
                    const submitBtn = form.querySelector('[type="submit"]');
                    if (submitBtn && !submitBtn.classList.contains('btn-loading')) {
                        this.setLoadingState(submitBtn);
                    }
                });
            });
        },

        setLoadingState: function (btn) {
            // Store original state
            btn.dataset.originalText = btn.innerHTML;
            btn.dataset.originalWidth = btn.offsetWidth + 'px';

            // Set fixed width to prevent size change
            btn.style.minWidth = btn.dataset.originalWidth;

            // Add loading class and disable
            btn.classList.add('btn-loading');
            btn.disabled = true;

            // Set loading text (hidden by CSS, shown as spinner)
            btn.innerHTML = '<span class="btn-loading-text">Bekleniyor...</span>';
        },

        resetLoadingState: function (btn) {
            if (btn.dataset.originalText) {
                btn.innerHTML = btn.dataset.originalText;
                btn.classList.remove('btn-loading');
                btn.disabled = false;
                btn.style.minWidth = '';
            }
        }
    };

    // ========================================
    // Page Transition Effects
    // ========================================
    const PageTransitions = {
        init: function () {
            // Page is already visible via CSS animation
            // Add interactive enhancements
            this.enhanceLinks();
        },

        enhanceLinks: function () {
            // Add subtle feedback on navigation links
            document.querySelectorAll('a[href]:not([href^="#"]):not([href^="javascript"])').forEach(link => {
                // Skip external links and download links
                if (link.target === '_blank' || link.hasAttribute('download')) return;

                link.addEventListener('click', function (e) {
                    // Let the browser handle navigation naturally
                    // Just add a micro-interaction feedback
                    this.style.transform = 'scale(0.98)';
                    setTimeout(() => {
                        this.style.transform = '';
                    }, 100);
                });
            });
        }
    };

    // ========================================
    // Interactive Card Enhancements
    // ========================================
    const CardEnhancements = {
        init: function () {
            this.addClickableCards();
        },

        addClickableCards: function () {
            // Make cards with single links fully clickable
            document.querySelectorAll('.card').forEach(card => {
                const links = card.querySelectorAll('a');
                if (links.length === 1) {
                    card.style.cursor = 'pointer';
                    card.addEventListener('click', (e) => {
                        // Don't trigger if clicking on interactive elements
                        if (e.target.closest('a, button, input, select, textarea')) return;
                        links[0].click();
                    });
                }
            });
        }
    };

    // ========================================
    // Initialize on DOM Ready
    // ========================================
    document.addEventListener('DOMContentLoaded', function () {
        ToastManager.init();
        ModalManager.init();
        FormUtils.init();
        TableUtils.init();
        NavUtils.init();

        // New premium UX features
        ScrollReveal.init();
        ButtonLoadingManager.init();
        PageTransitions.init();
        CardEnhancements.init();
    });

    // Expose managers globally for manual use
    window.FitnessCenter = {
        toast: ToastManager,
        modal: ModalManager,
        buttonLoading: ButtonLoadingManager
    };

    // ========================================
    // TESTIMONIAL SLIDER
    // Auto-advances every 3 seconds
    // ========================================
    const TestimonialSlider = {
        slider: null,
        dots: null,
        slides: [],
        currentIndex: 0,
        interval: null,
        isPaused: false,

        init() {
            this.slider = document.getElementById('testimonialSlider');
            this.dots = document.getElementById('testimonialDots');

            if (!this.slider || !this.dots) return;

            this.slides = this.slider.querySelectorAll('.testimonial-slide');
            if (this.slides.length === 0) return;

            // Dot click handlers
            this.dots.querySelectorAll('.testimonial-dot').forEach((dot, index) => {
                dot.addEventListener('click', () => {
                    this.goToSlide(index);
                    this.resetInterval();
                });
            });

            // Pause on hover
            this.slider.addEventListener('mouseenter', () => {
                this.isPaused = true;
            });

            this.slider.addEventListener('mouseleave', () => {
                this.isPaused = false;
            });

            // Start auto-advance
            this.startInterval();
        },

        goToSlide(index) {
            // Remove active from current
            this.slides[this.currentIndex].classList.remove('active');
            this.dots.children[this.currentIndex].classList.remove('active');

            // Update index
            this.currentIndex = index;

            // Add active to new
            this.slides[this.currentIndex].classList.add('active');
            this.dots.children[this.currentIndex].classList.add('active');
        },

        nextSlide() {
            if (this.isPaused) return;
            const next = (this.currentIndex + 1) % this.slides.length;
            this.goToSlide(next);
        },

        startInterval() {
            this.interval = setInterval(() => this.nextSlide(), 3000);
        },

        resetInterval() {
            clearInterval(this.interval);
            this.startInterval();
        }
    };

    // Initialize testimonial slider when DOM is ready
    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', () => TestimonialSlider.init());
    } else {
        TestimonialSlider.init();
    }

})();
