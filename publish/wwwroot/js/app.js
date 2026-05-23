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
  }
};