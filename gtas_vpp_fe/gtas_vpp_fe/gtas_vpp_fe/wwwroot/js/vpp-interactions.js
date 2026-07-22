(function () {
    if (window.vppInteractionsInitialized) {
        return;
    }

    window.vppInteractionsInitialized = true;

    function prefersReducedMotion() {
        return window.matchMedia
            && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    }

    window.vppTheme = {
        current: function () {
            var match = document.cookie.match(/(?:^|;\s*)VPPTheme=([^;]*)/);
            return match ? decodeURIComponent(match[1]) : "material3";
        },
        apply: function (theme) {
            var nextTheme = theme || "material3";
            var shouldUseDark = nextTheme.indexOf("dark") !== -1;

            function applyThemeClass() {
                document.documentElement.classList.toggle("rz-theme-dark", shouldUseDark);
                document.cookie = "VPPTheme=" + encodeURIComponent(nextTheme) + "; path=/; max-age=31536000";
            }

            if (document.startViewTransition && !prefersReducedMotion()) {
                document.startViewTransition(applyThemeClass);
                return;
            }

            applyThemeClass();
        }
    };

    window.vppLanguage = {
        prepareSwitch: function () {
            if (prefersReducedMotion()) {
                return Promise.resolve();
            }

            document.documentElement.classList.add("vpp-culture-changing");
            window.setTimeout(function () {
                document.documentElement.classList.remove("vpp-culture-changing");
            }, 500);

            return new Promise(function (resolve) {
                window.setTimeout(resolve, 140);
            });
        }
    };

    window.vppDownload = {
        fromStream: async function (fileName, contentStreamReference) {
            var arrayBuffer = await contentStreamReference.arrayBuffer();
            var blob = new Blob([arrayBuffer]);
            var url = URL.createObjectURL(blob);
            var anchor = document.createElement("a");
            anchor.href = url;
            anchor.download = fileName || "download";
            anchor.click();
            anchor.remove();
            URL.revokeObjectURL(url);
        }
    };

    window.vppDrafts = {
        clearAll: function () {
            var prefix = "vpp.order.draft.";
            for (var index = localStorage.length - 1; index >= 0; index--) {
                var key = localStorage.key(index);
                if (key && key.indexOf(prefix) === 0) {
                    localStorage.removeItem(key);
                }
            }
        },
        clearUserExceptPeriod: function (userId, periodId) {
            var prefix = "vpp.order.draft." + String(userId || "").trim() + ".";
            var keepPrefix = prefix + String(periodId || "").toLowerCase() + ".";
            for (var index = localStorage.length - 1; index >= 0; index--) {
                var key = localStorage.key(index);
                if (key && key.indexOf(prefix) === 0 && key.indexOf(keepPrefix) !== 0) {
                    localStorage.removeItem(key);
                }
            }
        }
    };

    var tabListSelector = ".rz-tabview-nav";
    var tabTargetSelector = ".rz-tabview-nav-link, .rz-tabs-item, [role='tab']";
    var activeTabSelector = ".rz-tabview-selected .rz-tabview-nav-link, "
        + ".rz-tabview-nav-link.rz-state-active, "
        + ".rz-tabs-item.rz-state-active, "
        + "[role='tab'][aria-selected='true']";
    var tabIndicatorDuration = 180;

    function findTabHost(tabList) {
        return tabList.closest(".rz-tabview-nav-container") || tabList;
    }

    function findActiveTab(tabList) {
        return tabList.querySelector(activeTabSelector);
    }

    function readTabIndicatorGeometry(tabList, target, host) {
        if (!target || !host) {
            return null;
        }

        var targetRect = target.getBoundingClientRect();
        var hostRect = host.getBoundingClientRect();
        var rootStyles = window.getComputedStyle(document.documentElement);
        var targetStyles = window.getComputedStyle(target);
        var fallbackInset = parseFloat(rootStyles.getPropertyValue("--vpp-nav-indicator-inset")) || 8;
        var startInset = parseFloat(targetStyles.paddingLeft);
        var endInset = parseFloat(targetStyles.paddingRight);

        if (!Number.isFinite(startInset)) {
            startInset = fallbackInset;
        }

        if (!Number.isFinite(endInset)) {
            endInset = fallbackInset;
        }

        if (startInset + endInset >= targetRect.width) {
            startInset = fallbackInset;
            endInset = fallbackInset;
        }

        var scrollOffset = host === tabList ? tabList.scrollLeft : 0;

        return {
            left: targetRect.left - hostRect.left + scrollOffset + startInset,
            width: Math.max(0, targetRect.width - startInset - endInset)
        };
    }

    function setTabIndicatorGeometry(indicator, geometry) {
        indicator.style.transform = "translate3d(" + geometry.left + "px, 0, 0)";
        indicator.style.width = geometry.width + "px";
        if (!indicator.classList.contains("is-ready")) {
            indicator.classList.add("is-ready");
        }
    }

    function ensureTabIndicator(tabList) {
        if (!(tabList instanceof Element)) {
            return null;
        }

        var host = findTabHost(tabList);
        if (!host) {
            return null;
        }

        host.classList.add("vpp-tab-indicator-host");
        var indicator = Array.from(host.children).find(function (child) {
            return child.classList && child.classList.contains("vpp-tab-shared-indicator");
        });

        if (!indicator) {
            indicator = document.createElement("span");
            indicator.className = "vpp-tab-shared-indicator";
            indicator.setAttribute("aria-hidden", "true");
            host.appendChild(indicator);
        }

        if (!tabList.vppIndicatorObserver) {
            tabList.vppIndicatorObserver = new MutationObserver(function () {
                window.requestAnimationFrame(function () {
                    moveTabIndicator(tabList, findActiveTab(tabList), true);
                });
            });
            tabList.vppIndicatorObserver.observe(tabList, {
                attributes: true,
                attributeFilter: ["class", "aria-selected"],
                childList: true,
                subtree: true
            });

            tabList.addEventListener("scroll", function () {
                moveTabIndicator(tabList, findActiveTab(tabList), false);
            }, { passive: true });

            if (window.ResizeObserver) {
                tabList.vppIndicatorResizeObserver = new ResizeObserver(function () {
                    moveTabIndicator(tabList, findActiveTab(tabList), false);
                });
                tabList.vppIndicatorResizeObserver.observe(tabList);
            }
        }

        return { host: host, indicator: indicator };
    }

    function moveTabIndicator(tabList, target, shouldAnimate) {
        var elements = ensureTabIndicator(tabList);
        if (!elements || !target || !tabList.contains(target)) {
            return;
        }

        var next = readTabIndicatorGeometry(tabList, target, elements.host);
        if (!next) {
            return;
        }

        var indicator = elements.indicator;
        var hostRect = elements.host.getBoundingClientRect();
        var indicatorRect = indicator.getBoundingClientRect();
        var currentScrollOffset = elements.host === tabList ? tabList.scrollLeft : 0;
        var current = {
            left: indicatorRect.left - hostRect.left + currentScrollOffset,
            width: indicatorRect.width
        };
        var isReady = indicator.classList.contains("is-ready");
        var isSamePosition = Math.abs(current.left - next.left) < 0.5
            && Math.abs(current.width - next.width) < 0.5;

        if (shouldAnimate && indicator.vppAnimation && indicator.vppTarget === target) {
            return;
        }

        if (indicator.vppAnimation) {
            indicator.vppAnimation.cancel();
            indicator.vppAnimation = null;
            indicator.vppTarget = null;
        }

        setTabIndicatorGeometry(indicator, next);

        if (!shouldAnimate || !isReady || isSamePosition || prefersReducedMotion()) {
            return;
        }

        var currentRight = current.left + current.width;
        var nextRight = next.left + next.width;
        var movingRight = next.left >= current.left;
        var stretched = movingRight
            ? { left: current.left, width: Math.max(current.width, nextRight - current.left) }
            : { left: next.left, width: Math.max(next.width, currentRight - next.left) };
        var easing = "cubic-bezier(0.32, 0.72, 0, 1)";

        indicator.vppTarget = target;
        indicator.vppAnimation = indicator.animate([
            {
                transform: "translate3d(" + current.left + "px, 0, 0)",
                width: current.width + "px",
                offset: 0,
                easing: easing
            },
            {
                transform: "translate3d(" + stretched.left + "px, 0, 0)",
                width: stretched.width + "px",
                offset: 0.52,
                easing: easing
            },
            {
                transform: "translate3d(" + next.left + "px, 0, 0)",
                width: next.width + "px",
                offset: 1
            }
        ], {
            duration: tabIndicatorDuration,
            easing: "linear",
            fill: "none"
        });

        indicator.vppAnimation.addEventListener("finish", function () {
            indicator.vppAnimation = null;
            indicator.vppTarget = null;
        }, { once: true });
    }

    function initializeTabIndicators(root) {
        if (root instanceof Element && root.matches(tabListSelector)) {
            moveTabIndicator(root, findActiveTab(root), false);
        }

        if (root.querySelectorAll) {
            root.querySelectorAll(tabListSelector).forEach(function (tabList) {
                moveTabIndicator(tabList, findActiveTab(tabList), false);
            });
        }
    }

    function disposeTabList(tabList) {
        if (tabList.vppIndicatorObserver) {
            tabList.vppIndicatorObserver.disconnect();
            tabList.vppIndicatorObserver = null;
        }

        if (tabList.vppIndicatorResizeObserver) {
            tabList.vppIndicatorResizeObserver.disconnect();
            tabList.vppIndicatorResizeObserver = null;
        }
    }

    function disposeTabIndicators(root) {
        if (!(root instanceof Element)) {
            return;
        }

        if (root.matches(tabListSelector)) {
            disposeTabList(root);
        }

        root.querySelectorAll(tabListSelector).forEach(disposeTabList);
    }

    document.addEventListener("click", function (event) {
        var target = event.target instanceof Element
            ? event.target.closest(tabTargetSelector)
            : null;
        var tabList = target ? target.closest(tabListSelector) : null;

        if (!target || !tabList || target.getAttribute("aria-disabled") === "true"
            || target.classList.contains("rz-state-disabled")) {
            return;
        }

        moveTabIndicator(tabList, target, true);
    }, true);

    var tabTreeObserver = new MutationObserver(function (mutations) {
        mutations.forEach(function (mutation) {
            mutation.removedNodes.forEach(function (node) {
                disposeTabIndicators(node);
            });
            mutation.addedNodes.forEach(function (node) {
                if (node instanceof Element) {
                    initializeTabIndicators(node);
                }
            });
        });
    });

    function startTabIndicators() {
        initializeTabIndicators(document);
        tabTreeObserver.observe(document.body, { childList: true, subtree: true });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", startTabIndicators, { once: true });
    } else {
        startTabIndicators();
    }

    var pressSurfaceSelector = [
        ".vpp-sidebar .rz-navigation-item.ppjsidebarmenu > .rz-navigation-item-wrapper",
        ".vpp-sidebar-brand",
        ".vpp-sidebar-toggle",
        ".vpp-sidebar-user-menu .user-menu-trigger",
        ".rz-tabview .rz-tabview-nav-link",
        ".rz-tabview .rz-tabs-item",
        ".rz-tabview .rz-tabview-nav > li > button[role='tab']"
    ].join(", ");

    function playPressSurface(target) {
        if (prefersReducedMotion() || !(target instanceof Element)) {
            return;
        }

        var surface = target.closest(pressSurfaceSelector);
        if (!surface) {
            return;
        }

        surface.classList.remove("vpp-pressing");
        void surface.offsetWidth;
        surface.classList.add("vpp-pressing");

        window.clearTimeout(surface.vppPressTimer);
        surface.vppPressTimer = window.setTimeout(function () {
            surface.classList.remove("vpp-pressing");
        }, 380);
    }

    document.addEventListener("pointerdown", function (event) {
        playPressSurface(event.target);
    }, true);

    document.addEventListener("keydown", function (event) {
        if (!event.repeat && (event.key === "Enter" || event.key === " ")) {
            playPressSurface(event.target);
        }
    }, true);

    function closeOrderCodePopovers(exceptCell) {
        document.querySelectorAll(".vpp-order-code-cell.is-open").forEach(function (cell) {
            if (cell === exceptCell) {
                return;
            }

            cell.classList.remove("is-open");
            var trigger = cell.querySelector(".vpp-order-code-trigger");
            if (trigger) {
                trigger.setAttribute("aria-expanded", "false");
            }
        });
    }

    document.addEventListener("click", function (event) {
        if (!(event.target instanceof Element)) {
            closeOrderCodePopovers();
            return;
        }

        var trigger = event.target.closest(".vpp-order-code-trigger");
        if (trigger) {
            var cell = trigger.closest(".vpp-order-code-cell");
            if (!cell) {
                return;
            }

            var shouldOpen = !cell.classList.contains("is-open");
            closeOrderCodePopovers(cell);
            cell.classList.toggle("is-open", shouldOpen);
            trigger.setAttribute("aria-expanded", shouldOpen ? "true" : "false");
            event.stopPropagation();
            return;
        }

        if (event.target.closest(".vpp-order-code-popover")) {
            return;
        }

        closeOrderCodePopovers();
    });

    document.addEventListener("keydown", function (event) {
        var trigger = event.target instanceof Element
            ? event.target.closest(".vpp-order-code-trigger")
            : null;
        if (trigger && (event.key === "Enter" || event.key === " ")) {
            event.preventDefault();
            trigger.click();
            return;
        }

        if (event.key === "Escape") {
            closeOrderCodePopovers();
        }
    });
})();
