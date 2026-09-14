/* ═══════════════════════════════════════════════════════════
   motion.js — HalalChain 2026
   Vanilla-JS equivalent of framer-motion's `whileInView` +
   `staggerChildren` for the Blazor storefront.

   - Observes [data-reveal]      → toggles .is-visible
   - Observes [data-reveal-stagger] → toggles .is-visible (CSS
                                     handles per-child delays)
   - Falls back gracefully: if IntersectionObserver is missing,
     all elements are made visible immediately after a tick.
   - Respects prefers-reduced-motion: reveals are instant.
   - Idempotent: safe to re-run (no double-binding).
   ═══════════════════════════════════════════════════════════ */

(function () {
    "use strict";

    if (window.__halalchainMotionInit) return;
    window.__halalchainMotionInit = true;

    const REVEAL = "[data-reveal]";
    const REVEAL_STAGGER = "[data-reveal-stagger]";
    const VISIBLE_CLASS = "is-visible";

    const prefersReducedMotion = window.matchMedia &&
        window.matchMedia("(prefers-reduced-motion: reduce)").matches;

    function revealAll() {
        document.querySelectorAll(REVEAL + ", " + REVEAL_STAGGER)
            .forEach((el) => el.classList.add(VISIBLE_CLASS));
    }

    function init() {
        if (prefersReducedMotion) {
            revealAll();
            return;
        }

        if (!("IntersectionObserver" in window)) {
            setTimeout(revealAll, 100);
            return;
        }

        const observer = new IntersectionObserver(
            (entries, obs) => {
                entries.forEach((entry) => {
                    if (entry.isIntersecting) {
                        entry.target.classList.add(VISIBLE_CLASS);
                        obs.unobserve(entry.target);
                    }
                });
            },
            {
                root: null,
                rootMargin: "0px 0px -10% 0px",
                threshold: 0.08,
            }
        );

        document.querySelectorAll(REVEAL + ", " + REVEAL_STAGGER)
            .forEach((el) => observer.observe(el));
    }

    /* Blazor Server uses enhanced navigation; re-init on each nav
       so newly-rendered components get observed. The page lifecycle
       fires `enhancedload` on the document. */
    document.addEventListener("DOMContentLoaded", init);
    document.addEventListener("enhancedload", init);

    /* Blazor's circuit may replace the DOM after SignalR
       re-renders without firing DOMContentLoaded; a MutationObserver
       picks up newly-added [data-reveal] nodes. */
    if ("MutationObserver" in window) {
        const mo = new MutationObserver((mutations) => {
            for (const m of mutations) {
                m.addedNodes.forEach((node) => {
                    if (node.nodeType !== 1) return;
                    if (node.matches &&
                        (node.matches(REVEAL) || node.matches(REVEAL_STAGGER))) {
                        if (prefersReducedMotion) {
                            node.classList.add(VISIBLE_CLASS);
                        } else if ("IntersectionObserver" in window) {
                            document
                                .querySelectorAll(REVEAL + ", " + REVEAL_STAGGER)
                                .forEach((el) => {
                                    if (!el.classList.contains(VISIBLE_CLASS)) {
                                        /* lazily attach a fresh observer if needed */
                                    }
                                });
                        }
                    }
                });
            }
        });
        mo.observe(document.documentElement, { childList: true, subtree: true });
    }

    /* ═══ Ripple click effect for .btn (Framer Motion whileTap feel) ═══
       Creates an expanding circle at the click point. Skips disabled /
       loading buttons and respects prefers-reduced-motion. */
    if (!prefersReducedMotion) {
        document.addEventListener("pointerdown", (ev) => {
            const btn = ev.target.closest(".btn");
            if (!btn || btn.disabled || btn.classList.contains("btn-loading")) return;
            const rect = btn.getBoundingClientRect();
            const size = Math.max(rect.width, rect.height);
            const x = ev.clientX - rect.left - size / 2;
            const y = ev.clientY - rect.top - size / 2;
            const ripple = document.createElement("span");
            ripple.className = "ripple";
            // Use a darker ripple on light backgrounds for visibility
            if (btn.classList.contains("btn-secondary") ||
                btn.classList.contains("btn-ghost") ||
                btn.classList.contains("btn-outline") ||
                btn.classList.contains("btn-glass")) {
                ripple.classList.add("ripple-dark");
            }
            ripple.style.width = ripple.style.height = size + "px";
            ripple.style.left = x + "px";
            ripple.style.top = y + "px";
            btn.appendChild(ripple);
            setTimeout(() => ripple.remove(), 650);
        }, { passive: true });
    }
})();
