// app.js -- Theme persistence, smooth scrolling, scrollspy, and scroll reveal
// How to extend: add new functions to window.portfolio object

window.portfolio = {

  // === THEME MANAGEMENT ===
  loadTheme: function() {
    return localStorage.getItem('portfolio-theme') || 'vsdark';
  },

  applyTheme: function(t) {
    document.body.dataset.theme = t;
    localStorage.setItem('portfolio-theme', t);
  },

  // === SMOOTH SCROLL ===
  scrollTo: function(id) {
    var el = document.getElementById(id);
    if (el) {
      el.scrollIntoView({ behavior: 'smooth' });
      // Update URL hash without jumping
      history.pushState(null, null, '#' + id);
    }
  },

  // === SCROLLSPY ===
  initScrollspy: function() {
    var navLinks = document.querySelectorAll('.nav-link');
    var sections = [];
    navLinks.forEach(function(link) {
      var href = link.getAttribute('href');
      if (href && href.startsWith('#')) sections.push(href.substring(1));
    });

    if (sections.length === 0) return;

    var observer = new IntersectionObserver(function(entries) {
      entries.forEach(function(entry) {
        if (entry.isIntersecting) {
          var id = entry.target.id;
          navLinks.forEach(function(link) {
            link.classList.toggle('active', link.getAttribute('href') === '#' + id);
          });
        }
      });
    }, { rootMargin: '-50% 0px -50% 0px' });

    sections.forEach(function(id) {
      var el = document.getElementById(id);
      if (el) observer.observe(el);
    });
  },

  // === SCROLL REVEAL (fade-in on scroll) ===
  initScrollReveal: function() {
    var els = document.querySelectorAll('.reveal');
    if (els.length === 0) return;

    var observer = new IntersectionObserver(function(entries) {
      entries.forEach(function(entry) {
        if (entry.isIntersecting) {
          entry.target.classList.add('revealed');
          observer.unobserve(entry.target);
        }
      });
    }, { threshold: 0.15 });

    els.forEach(function(el) { observer.observe(el); });
  },

  // === BACK TO TOP ===
  initBackToTop: function() {
    var btn = document.getElementById('back-to-top');
    if (!btn) return;

    window.addEventListener('scroll', function() {
      if (window.scrollY > 400) {
        btn.classList.add('visible');
      } else {
        btn.classList.remove('visible');
      }
    });
  },

  // === TERMINAL SCROLL ===
  /**
   * Scrolls a terminal/console element to its bottom.
   * Used by TerminalHero to auto-scroll output as new lines are appended.
   * @param {HTMLElement} el - The element to scroll to its maximum scroll extent.
   */
  scrollElementToBottom: function(el) {
    if (el) {
      el.scrollTop = el.scrollHeight;
    }
  },

  // === UPDATE ELEMENT TEXT ===
  /**
   * Replaces the text content of an element by ID.
   * Used by the platformer game to update the score display from C#.
   * @param {string} id - The element ID.
   * @param {string} text - The new text content.
   */
  updateElementText: function(id, text) {
    var el = document.getElementById(id);
    if (el) {
      el.textContent = text;
    }
  },

  // === FOCUS ELEMENT ===
  /**
   * Focuses a DOM element.
   * @param {HTMLElement} el - The element to focus.
   */
  focusElement: function(el) {
    if (el) {
      el.focus();
    }
  },

  // === SCROLL-UP FADE EFFECT ===
  /**
   * Fades in the content sections below the hero as the user scrolls down.
   *
   * On page load, the .fade-scroll-content container starts at opacity: 0 so only
   * the terminal hero is visible. As the user scrolls beyond the hero section,
   * opacity increases linearly until it reaches 1 (fully visible) when the hero
   * is completely scrolled out of view.
   *
   * This creates a smooth "reveal from below the fold" transition effect.
   */
  initScrollFade: function() {
    var fadeEl = document.querySelector('.fade-scroll-content');
    var heroEl = document.getElementById('home');
    if (!fadeEl || !heroEl) return;

    var updateFade = function() {
      // Get the full height of the hero section
      var heroHeight = heroEl.offsetHeight;
      var scrollY = window.scrollY;

      // Calculate fade progress: 0 at top of page, 1 once hero is fully scrolled past
      // progress = scrollY / heroHeight, clamped to max 1
      var progress = Math.min(scrollY / heroHeight, 1);

      // Apply calculated opacity to the content container
      fadeEl.style.opacity = progress;
    };

    window.addEventListener('scroll', updateFade);
    updateFade();
  }
};