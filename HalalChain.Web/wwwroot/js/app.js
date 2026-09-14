// Theme management — single source of truth on <html> for the class.
// Stores the value in localStorage as a plain string. Uses BOTH a
// class (`dark-theme`) and a `data-theme` attribute so CSS can target
// either: `.dark-theme .x` AND `[data-theme="dark"] .x`.
window.getStoredTheme = () => {
    try { return localStorage.getItem('theme') || 'light'; }
    catch { return 'light'; }
};
window.storeTheme = (theme) => {
    try { localStorage.setItem('theme', theme); } catch { /* ignore */ }
};
window.applyTheme = (theme) => {
    const t = (theme === 'dark') ? 'dark' : 'light';
    const root = document.documentElement;
    root.classList.toggle('dark-theme', t === 'dark');
    root.setAttribute('data-theme', t);
    root.style.colorScheme = t; // native form controls + scrollbar
    window.applyRadzenTheme();
    // Notify Blazor (if circuit ready) so the icon/state can update
    if (window.__blazorThemeCallback) {
        try { window.__blazorThemeCallback(t); } catch { /* ignore */ }
    }
    // Fire a DOM event other components can listen to
    try { root.dispatchEvent(new CustomEvent('themechange', { detail: { theme: t } })); } catch {}
    return t;
};

// Swap the active Radzen/UI-framework stylesheet variant. We do not
// bundle every *-base / *-dark-base / *-wcag file because they declare
// the same CSS custom properties and would conflict; the active variant
// is chosen from localStorage preferences.
window.applyRadzenTheme = () => {
    try {
        const framework = (localStorage.getItem('uiFramework') || 'fluent').toLowerCase();
        const theme = document.documentElement.classList.contains('dark-theme') ? 'dark' : 'light';
        const wcag = localStorage.getItem('wcag') === '1';
        const expressive = localStorage.getItem('materialVariant') === 'expressive';
        const name = framework === 'material3'
            ? (expressive ? 'material3expressive' : 'material3')
            : 'fluent';
        const variant = theme === 'dark' ? '-dark' : '';
        const suffix = wcag ? '-wcag' : '-base';
        const baseHref = `css/${name}${variant}${suffix}.css`;
        const wcagHref = `css/${name}${variant}-wcag.css`;
        const base = document.getElementById('radzen-theme-link');
        if (base && base.getAttribute('href') !== baseHref) base.setAttribute('href', baseHref);
        const wcagLink = document.getElementById('radzen-theme-wcag');
        if (wcagLink && wcagLink.getAttribute('href') !== wcagHref) wcagLink.setAttribute('href', wcagHref);
    } catch { /* ignore */ }
};
window.toggleTheme = () => {
    const current = document.documentElement.classList.contains('dark-theme') ? 'dark' : 'light';
    const next = current === 'dark' ? 'light' : 'dark';
    window.storeTheme(next);
    return window.applyTheme(next);
};
// Apply the stored theme immediately (IIFE) and re-apply on Blazor
// enhanced navigation so the class survives re-renders.
(function () {
    const t = window.getStoredTheme();
    window.applyTheme(t);
})();
document.addEventListener('enhancedload', () => window.applyTheme(window.getStoredTheme()));
// Blazor Server uses a `pageshow` / circuit reconnect path; also
// re-apply on visibility change to recover from any drift.
document.addEventListener('visibilitychange', () => {
    if (!document.hidden) window.applyTheme(window.getStoredTheme());
});

// ── Blazor bridge: lets the native theme handler call back into
//   Blazor to update the icon and ThemeService state. The Blazor
//   layout calls this in OnAfterRenderAsync and stores the reference
//   here.
window.__themeDotNetRef = null;
window.registerThemeCallback = (ref) => {
    window.__themeDotNetRef = ref;
};

// ── Native click handler for the theme toggle button ──────────────
// This is now the SINGLE source of truth for the toggle. The button
// in Blazor does NOT have @onclick (we removed it) so there's no
// double-fire path. The handler:
//   1. Calls window.toggleTheme() to flip the DOM class and persist
//   2. Calls back into Blazor via the registered DotNetObjectReference
//      so the icon re-renders
// It also runs during the pre-Blazor window (before blazor.server.js
// loads) so clicking works immediately on first paint.
(function () {
    let lastToggleAt = 0;
    document.addEventListener('click', (e) => {
        const btn = e.target.closest('[data-theme-toggle]');
        if (!btn) return;
        const now = Date.now();
        if (now - lastToggleAt < 200) return; // debounce rapid double-clicks
        lastToggleAt = now;
        e.preventDefault();
        e.stopPropagation();
        const t = window.toggleTheme();
        btn.setAttribute('aria-pressed', t === 'dark' ? 'true' : 'false');
        // Notify Blazor so the icon (fa-sun / fa-moon) and ThemeService
        // state update. Safe to call even if Blazor hasn't registered yet
        // (the ref is null) — Blazor catches up on its next render.
        try {
            const ref = window.__themeDotNetRef;
            if (ref) ref.invokeMethodAsync('OnNativeThemeChanged', t);
        } catch (_) { /* ignore */ }
    });
})();

// LocalStorage helpers
window.localStorageHelper = {
    getItem: (key) => {
        try { return JSON.parse(localStorage.getItem(key)); } catch { return null; }
    },
    setItem: (key, value) => {
        localStorage.setItem(key, JSON.stringify(value));
    }
};

// Scroll listener for sticky header shadow
window.addScrollListener = (dotNetHelper) => {
    window.addEventListener('scroll', () => {
        dotNetHelper.invokeMethodAsync('OnScroll', window.scrollY);
    });
};

// Keyboard shortcut for search (Ctrl+K)
document.addEventListener('keydown', (e) => {
    if ((e.ctrlKey || e.metaKey) && e.key === 'k') {
        e.preventDefault();
        const searchInput = document.getElementById('global-search');
        if (searchInput) searchInput.focus();
    }
});

// Close dropdowns on outside click (handled by Blazor stopPropagation)
document.addEventListener('click', (e) => {
    if (!e.target.closest('.notification-wrapper') && !e.target.closest('.user-profile-wrapper')) {
        document.querySelectorAll('.notification-dropdown, .profile-dropdown, .sf-profile-dropdown, .sf-cart-dropdown').forEach(el => {
            el.style.display = 'none';
        });
    }
});

// Focus element by ID
window.focusElement = (id) => {
    const el = document.getElementById(id);
    if (el) el.focus();
};

// Download file helper for CSV export
window.downloadFile = (filename, contentType, base64Data) => {
    const bytes = Uint8Array.from(atob(base64Data), c => c.charCodeAt(0));
    const blob = new Blob([bytes], { type: contentType });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};
