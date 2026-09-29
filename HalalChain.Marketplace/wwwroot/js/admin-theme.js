/**
 * ============================================================================
 * ADMIN DASHBOARD THEME MODULE — hcTheme
 * ============================================================================
 * 
 * Manages dark/light theme persistence, system preference detection,
 * and theme-related UI interactions for the admin dashboard.
 * 
 * Usage:
 *   hcTheme.init()           // Initialize on page load
 *   hcTheme.set('dark')      // Set theme to dark
 *   hcTheme.set('light')     // Set theme to light
 *   hcTheme.toggle()         // Toggle between dark and light
 *   hcTheme.current()        // Get current theme
 *   hcTheme.onThemeChange(callback)  // Listen for theme changes
 * ============================================================================
 */

const hcTheme = (() => {
  'use strict';

  // Configuration
  const CONFIG = {
    storageKey: 'hc-theme-preference',
    darkTheme: 'dark',
    lightTheme: 'light',
    systemPrefersDark: '(prefers-color-scheme: dark)',
    attributeName: 'data-theme',
    transitionDuration: 300, // ms
  };

  // State
  let currentTheme = CONFIG.darkTheme;
  let themeChangeListeners = [];
  let mediaQueryList = null;

  /**
   * Get the stored theme preference from localStorage
   */
  function getStoredTheme() {
    try {
      return localStorage.getItem(CONFIG.storageKey);
    } catch (e) {
      console.warn('[hcTheme] localStorage unavailable:', e);
      return null;
    }
  }

  /**
   * Save theme preference to localStorage
   */
  function saveTheme(theme) {
    try {
      localStorage.setItem(CONFIG.storageKey, theme);
    } catch (e) {
      console.warn('[hcTheme] Could not save theme to localStorage:', e);
    }
  }

  /**
   * Detect system color scheme preference
   */
  function getSystemTheme() {
    if (window.matchMedia && window.matchMedia(CONFIG.systemPrefersDark).matches) {
      return CONFIG.darkTheme;
    }
    return CONFIG.lightTheme;
  }

  /**
   * Get the effective theme (stored > system > default)
   */
  function getEffectiveTheme() {
    const stored = getStoredTheme();
    if (stored) return stored;
    return getSystemTheme();
  }

  /**
   * Apply theme to DOM
   */
  function applyTheme(theme) {
    const shell = document.querySelector('[data-theme]');
    if (!shell) {
      console.warn('[hcTheme] No element with data-theme attribute found');
      return;
    }

    // Add transition class temporarily for smooth color change
    shell.style.transition = `background-color ${CONFIG.transitionDuration}ms, color ${CONFIG.transitionDuration}ms`;
    shell.setAttribute(CONFIG.attributeName, theme);

    // Remove transition after completion to avoid interference with animations
    setTimeout(() => {
      shell.style.transition = '';
    }, CONFIG.transitionDuration);

    currentTheme = theme;
  }

  /**
   * Initialize theme system
   * - Detect stored preference or system preference
   * - Apply theme to DOM
   * - Set up system preference listener
   */
  function init() {
    // Determine initial theme
    const initialTheme = getEffectiveTheme();
    applyTheme(initialTheme);

    // Listen for system theme changes
    if (window.matchMedia) {
      mediaQueryList = window.matchMedia(CONFIG.systemPrefersDark);

      // Handle both old and new matchMedia API
      if (mediaQueryList.addEventListener) {
        mediaQueryList.addEventListener('change', handleSystemThemeChange);
      } else if (mediaQueryList.addListener) {
        mediaQueryList.addListener(handleSystemThemeChange);
      }
    }

    // Listen for Ctrl+K (Cmd+K) global search shortcut
    document.addEventListener('keydown', handleGlobalKeydown);

    console.log('[hcTheme] Initialized with theme:', initialTheme);
  }

  /**
   * Handle system theme changes (e.g., OS dark mode toggle)
   */
  function handleSystemThemeChange(e) {
    // Only apply system change if user hasn't set a preference
    const stored = getStoredTheme();
    if (!stored) {
      const newTheme = e.matches ? CONFIG.darkTheme : CONFIG.lightTheme;
      applyTheme(newTheme);
      notifyListeners(newTheme);
    }
  }

  /**
   * Handle global keyboard shortcuts
   */
  function handleGlobalKeydown(e) {
    // Ctrl+K or Cmd+K for search
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
      e.preventDefault();
      const searchInput = document.querySelector('.cmd-search input');
      if (searchInput) {
        searchInput.focus();
      }
    }
  }

  /**
   * Notify all registered listeners of theme change
   */
  function notifyListeners(theme) {
    themeChangeListeners.forEach(callback => {
      try {
        callback(theme);
      } catch (e) {
        console.error('[hcTheme] Listener error:', e);
      }
    });
  }

  /**
   * Set theme explicitly (and store preference)
   */
  function setTheme(theme) {
    if (theme !== CONFIG.darkTheme && theme !== CONFIG.lightTheme) {
      console.warn(`[hcTheme] Invalid theme: ${theme}. Use 'dark' or 'light'.`);
      return;
    }

    applyTheme(theme);
    saveTheme(theme);
    notifyListeners(theme);

    console.log('[hcTheme] Theme set to:', theme);
  }

  /**
   * Toggle between dark and light themes
   */
  function toggleTheme() {
    const newTheme = currentTheme === CONFIG.darkTheme ? CONFIG.lightTheme : CONFIG.darkTheme;
    setTheme(newTheme);
  }

  /**
   * Get current theme
   */
  function getCurrentTheme() {
    return currentTheme;
  }

  /**
   * Register a listener for theme changes
   */
  function onThemeChange(callback) {
    if (typeof callback === 'function') {
      themeChangeListeners.push(callback);
    }
  }

  /**
   * Clean up theme system
   */
  function destroy() {
    if (mediaQueryList) {
      if (mediaQueryList.removeEventListener) {
        mediaQueryList.removeEventListener('change', handleSystemThemeChange);
      } else if (mediaQueryList.removeListener) {
        mediaQueryList.removeListener(handleSystemThemeChange);
      }
    }

    document.removeEventListener('keydown', handleGlobalKeydown);
    themeChangeListeners = [];
  }

  // Public API
  return {
    init,
    set: setTheme,
    toggle: toggleTheme,
    current: getCurrentTheme,
    onThemeChange,
    destroy,
  };
})();

// Auto-initialize on DOM ready
if (document.readyState === 'loading') {
  document.addEventListener('DOMContentLoaded', () => {
    hcTheme.init();
  });
} else {
  // DOM is already ready
  hcTheme.init();
}
