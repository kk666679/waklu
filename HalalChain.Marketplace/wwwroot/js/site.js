// HalalChain Marketplace — Blazor Progressive Enhancement
(function () {
  'use strict';

  var THEME_KEY = 'hc-theme';

  function applyTheme(t) {
    document.documentElement.setAttribute('data-theme', t);
    var meta = document.querySelector('meta[name="theme-color"]');
    if (meta) meta.content = t === 'dark' ? '#0b0d14' : '#fafaf8';
  }

  function currentTheme() {
    return document.documentElement.getAttribute('data-theme') ||
      (window.matchMedia && window.matchMedia('(prefers-color-scheme: dark)').matches ? 'dark' : 'light');
  }

  // Expose theme API for Blazor ThemeService interop
  window.hcTheme = {
    get: function () { return currentTheme(); },
    set: function (t) { localStorage.setItem(THEME_KEY, t); document.cookie = 'hc-theme=' + t + ';path=/;max-age=31536000;samesite=lax'; applyTheme(t); },
    toggle: function () { var next = currentTheme() === 'dark' ? 'light' : 'dark'; this.set(next); return next; }
  };

  // Bootstrap: apply stored theme on first load
  document.addEventListener('DOMContentLoaded', function () {
    var stored = localStorage.getItem(THEME_KEY);
    applyTheme(stored || currentTheme());
  });

  // Toast helper (used by NotificationService and wishlist)
  window.hcToast = function (msg) {
    var wrap = document.querySelector('.toast-wrap');
    if (!wrap) { wrap = document.createElement('div'); wrap.className = 'toast-wrap'; document.body.appendChild(wrap); }
    var t = document.createElement('div'); t.className = 'toast'; t.textContent = msg; wrap.appendChild(t);
    setTimeout(function () { t.style.opacity = '0'; t.style.transition = 'opacity .3s'; }, 2400);
    setTimeout(function () { t.remove(); }, 2800);
  };

  // Drawer helpers
  window.hcOpenDrawer = function (id) {
    var d = document.getElementById(id);
    if (!d) return;
    d.classList.add('open');
    var scrim = document.getElementById('hc-scrim'); if (scrim) scrim.classList.add('open');
    document.body.style.overflow = 'hidden';
  };
  window.hcCloseDrawer = function () {
    document.querySelectorAll('[data-drawer].open').forEach(function (m) { m.classList.remove('open'); });
    var scrim = document.getElementById('hc-scrim'); if (scrim) scrim.classList.remove('open');
    document.body.style.overflow = '';
  };

  // Bootstrap API for Blazor
  window.hcBootstrap = { init: function () { return true; } };
})();
