// app.js -- Theme persistence, smooth scrolling, and scrollspy
// How to extend: add new functions to window.portfolio object

window.portfolio = {

  // === THEME MANAGEMENT ===
  // Loads saved theme from localStorage, falls back to default
  loadTheme: function() {
    return localStorage.getItem('portfolio-theme') || 'vsdark';
  },

  // Applies theme by setting data-theme attribute on body
  // CSS uses [data-theme="name"] selectors for all color variables
  applyTheme: function(t) {
    document.body.dataset.theme = t;
    localStorage.setItem('portfolio-theme', t);
  },

  // === SMOOTH SCROLL ===
  // Scrolls to a section by its element ID with smooth behavior
  scrollTo: function(id) {
    var el = document.getElementById(id);
    if (el) el.scrollIntoView({ behavior: 'smooth' });
  },

  // === SCROLLSPY ===
  // Highlights active nav link based on which section is in view
  // Uses IntersectionObserver for performance
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
  }
};