(function () {
    if (window.vppInteractionsInitialized) {
        return;
    }

    window.vppInteractionsInitialized = true;

    // RadzenDataGrid 11.x đặt role="grid" trên div wrapper trong khi rowgroup thật
    // nằm sâu dưới một div cuộn không có role. Axe xem cấu trúc đó là
    // aria-required-children. Các grid opt-in giữ semantics bảng native bên trong,
    // còn wrapper trở thành region có tên để screen reader điều hướng ổn định.
    function normalizeGridRegions(root) {
        var selector = "[data-vpp-grid-region='true']";
        var grids = [];
        if (root instanceof Element && root.matches(selector)) {
            grids.push(root);
        }
        if (root.querySelectorAll) {
            grids.push.apply(grids, root.querySelectorAll(selector));
        }

        grids.forEach(function (grid) {
            grid.setAttribute("role", "region");
            grid.removeAttribute("aria-rowcount");
            grid.querySelectorAll("table[role='presentation']").forEach(function (table) {
                table.setAttribute("role", "table");
            });
            grid.querySelectorAll(".rz-data-grid-data").forEach(function (scrollRegion) {
                scrollRegion.setAttribute("tabindex", "0");
            });
        });
    }

    function normalizeRadzenAriaValues(root) {
        var selector = "[aria-disabled*='ToString']";
        var elements = [];
        if (root instanceof Element && root.matches(selector)) {
            elements.push(root);
        }
        if (root.querySelectorAll) {
            elements.push.apply(elements, root.querySelectorAll(selector));
        }

        elements.forEach(function (element) {
            var disabled = element.matches(":disabled, .rz-state-disabled")
                || element.closest("[disabled], .rz-state-disabled") !== null;
            element.setAttribute("aria-disabled", disabled ? "true" : "false");
        });
    }

    function updateRadzenDropdownDirection(panel) {
        if (!(panel instanceof Element) || !panel.matches(".rz-dropdown-panel")) {
            return;
        }

        var triggerId = panel.id && panel.id.indexOf("popup-") === 0
            ? panel.id.substring("popup-".length)
            : "";
        var trigger = triggerId ? document.getElementById(triggerId) : null;
        var isPageSize = trigger instanceof Element
            && trigger.closest(".rz-paginator, .rz-pager") !== null;
        panel.classList.toggle("vpp-page-size-panel", isPageSize);
        var isOpen = panel.classList.contains("rz-open")
            && panel.getClientRects().length > 0;
        if (!trigger || !isOpen) {
            panel.classList.remove("vpp-transient-surface--above");
            delete panel.dataset.vppDropdownPositioned;
            return false;
        }

        var panelRect = panel.getBoundingClientRect();
        var triggerRect = trigger.getBoundingClientRect();
        var openAbove = panelRect.top < triggerRect.top;

        panel.classList.toggle("vpp-transient-surface--above", openAbove);
        panel.dataset.vppDropdownPositioned = "true";
        return true;
    }

    function scheduleRadzenDropdownDirection(root) {
        var panels = [];
        if (root instanceof Element) {
            if (root.matches(".rz-dropdown-panel")) {
                panels.push(root);
            }
            if (root.closest(".rz-dropdown-panel")) {
                panels.push(root.closest(".rz-dropdown-panel"));
            }
        }
        if (root.querySelectorAll) {
            panels.push.apply(panels, root.querySelectorAll(".rz-dropdown-panel"));
        }

        Array.from(new Set(panels)).forEach(function (panel) {
            var attempts = 0;
            var settle = function () {
                if (!(panel instanceof Element) || !panel.isConnected) {
                    return;
                }
                if (updateRadzenDropdownDirection(panel)) {
                    return;
                }
                attempts += 1;
                if (attempts < 24) {
                    window.requestAnimationFrame(settle);
                }
            };
            settle();
        });
    }

    normalizeGridRegions(document);
    normalizeRadzenAriaValues(document);
    scheduleRadzenDropdownDirection(document);

    document.addEventListener("click", function (event) {
        var trigger = event.target instanceof Element
            ? event.target.closest(".rz-dropdown")
            : null;
        if (!trigger) {
            return;
        }

        window.requestAnimationFrame(function () {
            var panel = document.getElementById("popup-" + trigger.id);
            if (panel) {
                scheduleRadzenDropdownDirection(panel);
            }
        });
    }, true);

    document.addEventListener("keydown", function (event) {
        if (!["Enter", " ", "ArrowDown", "ArrowUp"].includes(event.key)) {
            return;
        }

        var trigger = event.target instanceof Element
            ? event.target.closest(".rz-dropdown")
            : null;
        if (!trigger) {
            return;
        }

        window.requestAnimationFrame(function () {
            var panel = document.getElementById("popup-" + trigger.id);
            if (panel) {
                scheduleRadzenDropdownDirection(panel);
            }
        });
    }, true);

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

            applyThemeClass();
        }
    };

    window.vppViewport = {
        isDesktop: function () {
            return window.matchMedia("(min-width: 769px)").matches;
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
        fromStream: async function (fileName, contentType, contentStreamReference) {
            var arrayBuffer = await contentStreamReference.arrayBuffer();
            var blob = new Blob([arrayBuffer], {
                type: contentType || "application/octet-stream"
            });
            var url = URL.createObjectURL(blob);
            var anchor = document.createElement("a");
            anchor.href = url;
            anchor.download = fileName || "download";
            anchor.style.display = "none";
            document.body.appendChild(anchor);
            anchor.click();
            anchor.remove();
            window.setTimeout(function () {
                URL.revokeObjectURL(url);
            }, 1000);
            return arrayBuffer.byteLength;
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

    function readCssTimeMilliseconds(value, fallback) {
        var trimmed = String(value || "").trim();
        var amount = parseFloat(trimmed);
        if (!Number.isFinite(amount)) {
            return fallback;
        }

        if (trimmed.endsWith("ms")) {
            return amount;
        }

        if (trimmed.endsWith("s")) {
            return amount * 1000;
        }

        return fallback;
    }

    var rootMotionStyles = window.getComputedStyle(document.documentElement);
    var navigationMotionDuration = readCssTimeMilliseconds(
        rootMotionStyles.getPropertyValue("--vpp-navigation-motion-duration"),
        180
    );
    var navigationMotionEasing = rootMotionStyles
        .getPropertyValue("--vpp-navigation-motion-easing").trim()
        || "cubic-bezier(0.2, 0, 0, 1)";
    var tabListSelector = ".rz-tabview-nav, .vpp-header-tabs, .vpp-local-header-tabs";
    var tabTargetSelector = ".rz-tabview-nav-link, .rz-tabs-item, [role='tab'], .vpp-header-tab";
    var activeTabSelector = ".rz-tabview-selected .rz-tabview-nav-link, "
        + ".rz-tabview-nav-link.rz-state-active, "
        + ".rz-tabs-item.rz-state-active, "
        + "[role='tab'][aria-selected='true'], "
        + ".vpp-header-tab.is-active, "
        + ".vpp-header-tab[aria-current='page']";
    var tabIndicatorDuration = navigationMotionDuration;
    var sidebarNavSelector = ".vpp-sidebar-nav";
    var sidebarIndicatorDuration = navigationMotionDuration;
    var sidebarLayoutFollowDuration = navigationMotionDuration + 40;

    function resolveCssPixelLength(element, value, fallback) {
        var trimmed = String(value || "").trim();
        if (!trimmed) {
            return fallback;
        }

        var amount = parseFloat(trimmed);
        if (!Number.isFinite(amount)) {
            return fallback;
        }

        if (trimmed.endsWith("px")) {
            return amount;
        }

        if (trimmed.endsWith("rem")) {
            var rootFontSize = parseFloat(window.getComputedStyle(document.documentElement).fontSize);
            return Number.isFinite(rootFontSize) ? amount * rootFontSize : fallback;
        }

        if (trimmed.endsWith("em")) {
            var elementFontSize = parseFloat(window.getComputedStyle(element).fontSize);
            return Number.isFinite(elementFontSize) ? amount * elementFontSize : fallback;
        }

        return fallback;
    }

    function readPrimaryTabIndicatorWidth(tabList) {
        var primaryTabs = tabList.closest(".vpp-admin-tabs");
        if (!primaryTabs || tabList.closest(".vpp-secondary-tabs")) {
            return null;
        }

        var preferredWidth = parseFloat(window.getComputedStyle(primaryTabs)
            .getPropertyValue("--vpp-primary-tab-indicator-preferred-width"));
        var labelWidths = Array.from(tabList.querySelectorAll(".rz-tabview-title"))
            .map(function (title) { return title.getBoundingClientRect().width; })
            .filter(function (width) { return Number.isFinite(width) && width > 0; });

        if (!labelWidths.length) {
            return Number.isFinite(preferredWidth) ? preferredWidth : null;
        }

        var shortestLabelWidth = Math.min.apply(Math, labelWidths);
        return Number.isFinite(preferredWidth)
            ? Math.min(preferredWidth, shortestLabelWidth)
            : shortestLabelWidth;
    }

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
        var title = target.querySelector(".rz-tabview-title");
        var scrollOffset = host === tabList ? tabList.scrollLeft : 0;

        if (title) {
            var titleRect = title.getBoundingClientRect();
            var commonIndicatorWidth = readPrimaryTabIndicatorWidth(tabList);
            var indicatorWidth = Number.isFinite(commonIndicatorWidth)
                ? Math.min(commonIndicatorWidth, titleRect.width)
                : titleRect.width;

            return {
                left: titleRect.left - hostRect.left + scrollOffset
                    + (titleRect.width - indicatorWidth) / 2,
                width: indicatorWidth
            };
        }

        var rootStyles = window.getComputedStyle(document.documentElement);
        var targetStyles = window.getComputedStyle(target);
        var fallbackInset = resolveCssPixelLength(
            target,
            rootStyles.getPropertyValue("--vpp-nav-indicator-inset"),
            8
        );
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
            tabList.vppIndicatorObserver = new MutationObserver(function (mutations) {
                var hasExternalChange = mutations.some(function (mutation) {
                    return !mutation.target.classList
                        || !mutation.target.classList.contains("vpp-tab-shared-indicator");
                });
                if (!hasExternalChange) {
                    return;
                }

                scheduleTabIndicatorSync(tabList, true);
            });
            tabList.vppIndicatorObserver.observe(tabList, {
                attributes: true,
                attributeFilter: ["class", "aria-selected"],
                childList: true,
                subtree: true
            });

            tabList.vppIndicatorScrollHandler = function () {
                scheduleTabIndicatorSync(tabList, false);
            };
            tabList.addEventListener("scroll", tabList.vppIndicatorScrollHandler, { passive: true });

            if (window.ResizeObserver) {
                tabList.vppIndicatorResizeObserver = new ResizeObserver(function () {
                    scheduleTabIndicatorSync(tabList, false);
                });
                tabList.vppIndicatorResizeObserver.observe(tabList);
            }
        }

        return { host: host, indicator: indicator };
    }

    function scheduleTabIndicatorSync(tabList, shouldAnimate) {
        tabList.vppIndicatorShouldAnimate = tabList.vppIndicatorShouldAnimate || shouldAnimate;
        if (tabList.vppIndicatorSyncFrame) {
            return;
        }

        tabList.vppIndicatorSyncFrame = window.requestAnimationFrame(function () {
            var animate = tabList.vppIndicatorShouldAnimate;
            tabList.vppIndicatorSyncFrame = null;
            tabList.vppIndicatorShouldAnimate = false;
            if (!tabList.isConnected) {
                return;
            }
            moveTabIndicator(tabList, findActiveTab(tabList), animate);
        });
    }

    function moveTabIndicator(tabList, target, shouldAnimate, forceTarget) {
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

        if (indicator.vppAnimation) {
            if (indicator.vppTarget === target || !forceTarget) {
                return;
            }

            indicator.vppAnimation.cancel();
            indicator.vppAnimation = null;
            indicator.vppTarget = null;
        }

        setTabIndicatorGeometry(indicator, next);

        if (!shouldAnimate || !isReady || isSamePosition || prefersReducedMotion()) {
            return;
        }

        indicator.vppTarget = target;
        indicator.vppAnimation = indicator.animate([
            {
                transform: "translate3d(" + current.left + "px, 0, 0)",
                width: current.width + "px"
            },
            {
                transform: "translate3d(" + next.left + "px, 0, 0)",
                width: next.width + "px"
            }
        ], {
            duration: tabIndicatorDuration,
            easing: navigationMotionEasing,
            fill: "none"
        });

        indicator.vppAnimation.addEventListener("finish", function () {
            indicator.vppAnimation = null;
            indicator.vppTarget = null;
            moveTabIndicator(tabList, findActiveTab(tabList), false, false);
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

        if (tabList.vppIndicatorScrollHandler) {
            tabList.removeEventListener("scroll", tabList.vppIndicatorScrollHandler);
            tabList.vppIndicatorScrollHandler = null;
        }

        if (tabList.vppIndicatorSyncFrame) {
            window.cancelAnimationFrame(tabList.vppIndicatorSyncFrame);
            tabList.vppIndicatorSyncFrame = null;
        }
        tabList.vppIndicatorShouldAnimate = false;

        var indicator = tabList.closest(".vpp-tab-indicator-host")
            ?.querySelector(":scope > .vpp-tab-shared-indicator");
        if (indicator?.vppAnimation) {
            indicator.vppAnimation.cancel();
            indicator.vppAnimation = null;
            indicator.vppTarget = null;
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

    function findTopLevelSidebarItem(item) {
        var current = item;
        while (current && current.parentElement
            && !current.parentElement.classList.contains("rz-panel-menu")) {
            current = current.parentElement.closest(".rz-navigation-item");
        }
        return current;
    }

    function findActiveSidebarLink(nav) {
        return nav.querySelector(
            ".rz-navigation-item-link[aria-current='page'], "
            + ".rz-navigation-item-link.active, "
            + ".rz-navigation-item-link-active"
        );
    }

    function sidebarTargetFromLink(nav, link) {
        if (!link) {
            return null;
        }

        var sidebar = nav.closest(".vpp-sidebar");
        var item = link.closest(".rz-navigation-item");
        if (sidebar && sidebar.classList.contains("sidebar-collapsed")) {
            item = findTopLevelSidebarItem(item);
        }

        return item
            ? item.querySelector(":scope > .rz-navigation-item-wrapper")
            : null;
    }

    function hasVisibleAreaWithin(element, boundary) {
        if (!element || !boundary || !element.isConnected) {
            return false;
        }

        var rect = element.getBoundingClientRect();
        if (rect.width <= 0 || rect.height <= 0) {
            return false;
        }

        var visibleLeft = rect.left;
        var visibleRight = rect.right;
        var visibleTop = rect.top;
        var visibleBottom = rect.bottom;
        var ancestor = element.parentElement;

        while (ancestor) {
            var styles = window.getComputedStyle(ancestor);
            if (styles.display === "none" || styles.visibility === "hidden"
                || styles.visibility === "collapse" || ancestor.getAttribute("aria-hidden") === "true") {
                return false;
            }

            var clipsContent = ancestor === boundary
                || ancestor.classList.contains("rz-navigation-menu")
                || styles.overflowX !== "visible"
                || styles.overflowY !== "visible";
            if (clipsContent) {
                var ancestorRect = ancestor.getBoundingClientRect();
                visibleLeft = Math.max(visibleLeft, ancestorRect.left);
                visibleRight = Math.min(visibleRight, ancestorRect.right);
                visibleTop = Math.max(visibleTop, ancestorRect.top);
                visibleBottom = Math.min(visibleBottom, ancestorRect.bottom);
                if (visibleRight - visibleLeft <= 0.5 || visibleBottom - visibleTop <= 0.5) {
                    return false;
                }
            }

            if (ancestor === boundary) {
                break;
            }
            ancestor = ancestor.parentElement;
        }

        return ancestor === boundary;
    }

    function findActiveSidebarTarget(nav) {
        var activeLink = findActiveSidebarLink(nav);
        if (activeLink) {
            nav.vppLastActiveLink = activeLink;
            var target = sidebarTargetFromLink(nav, activeLink);
            return hasVisibleAreaWithin(target, nav) ? target : null;
        }

        if (nav.vppLastActiveLink
            && (!nav.contains(nav.vppLastActiveLink)
                || !hasVisibleAreaWithin(nav.vppLastActiveLink, nav))) {
            return null;
        }

        var activeWrapper = nav.querySelector(".rz-navigation-item-wrapper-active");
        if (!activeWrapper) {
            return null;
        }

        var sidebar = nav.closest(".vpp-sidebar");
        if (sidebar && sidebar.classList.contains("sidebar-collapsed")) {
            var rootItem = findTopLevelSidebarItem(activeWrapper.closest(".rz-navigation-item"));
            return rootItem
                ? rootItem.querySelector(":scope > .rz-navigation-item-wrapper")
                : null;
        }

        return hasVisibleAreaWithin(activeWrapper, nav) ? activeWrapper : null;
    }

    function sidebarItemDepth(target) {
        var item = target.closest(".rz-navigation-item");
        var depth = 0;
        var parentMenu = item ? item.parentElement : null;

        while (parentMenu && !parentMenu.classList.contains("rz-panel-menu")) {
            if (parentMenu.classList.contains("rz-navigation-menu")) {
                depth++;
            }
            var parentItem = parentMenu.closest(".rz-navigation-item");
            parentMenu = parentItem ? parentItem.parentElement : null;
        }

        return depth;
    }

    function readSidebarIndicatorGeometry(nav, target) {
        if (!hasVisibleAreaWithin(target, nav)) {
            return null;
        }

        var navRect = nav.getBoundingClientRect();
        var targetRect = target.getBoundingClientRect();
        var rootStyles = window.getComputedStyle(document.documentElement);
        var navStyles = window.getComputedStyle(nav);
        var inset = resolveCssPixelLength(
            nav,
            rootStyles.getPropertyValue("--vpp-nav-indicator-inset"),
            8
        );
        var left = 0;

        if (sidebarItemDepth(target) > 0) {
            var icon = target.querySelector(":scope > .rz-navigation-item-link > .rz-navigation-item-icon");
            var iconGap = resolveCssPixelLength(
                nav,
                navStyles.getPropertyValue("--vpp-sidebar-indicator-icon-gap"),
                12
            );
            if (icon) {
                left = icon.getBoundingClientRect().left - navRect.left + nav.scrollLeft - iconGap;
            }
        }

        return {
            left: left,
            top: targetRect.top - navRect.top + nav.scrollTop + inset,
            height: Math.max(0, targetRect.height - (inset * 2))
        };
    }

    function setSidebarIndicatorGeometry(indicator, geometry) {
        indicator.style.transform = "translate3d(" + geometry.left + "px, " + geometry.top + "px, 0)";
        indicator.style.height = geometry.height + "px";
        if (!indicator.classList.contains("is-ready")) {
            indicator.classList.add("is-ready");
        }
    }

    function hideSidebarIndicator(indicator) {
        if (!indicator) {
            return;
        }

        if (indicator.vppAnimation) {
            indicator.vppAnimation.cancel();
            indicator.vppAnimation = null;
        }

        indicator.vppTarget = null;
        indicator.classList.remove("is-ready");
    }

    function followSidebarIndicatorLayout(nav) {
        var indicator = ensureSidebarIndicator(nav);
        if (!indicator) {
            return;
        }

        if (indicator.vppAnimation) {
            indicator.vppAnimation.cancel();
            indicator.vppAnimation = null;
            indicator.vppTarget = null;
        }

        nav.vppSidebarLayoutFollowUntil = performance.now() + sidebarLayoutFollowDuration;
        if (nav.vppSidebarLayoutFollowFrame) {
            return;
        }

        function followFrame(timestamp) {
            nav.vppSidebarLayoutFollowFrame = null;
            moveSidebarIndicator(nav, findActiveSidebarTarget(nav), false, true);

            if (timestamp < nav.vppSidebarLayoutFollowUntil) {
                nav.vppSidebarLayoutFollowFrame = window.requestAnimationFrame(followFrame);
                return;
            }

            nav.vppSidebarLayoutFollowUntil = 0;
            moveSidebarIndicator(nav, findActiveSidebarTarget(nav), false, true);
        }

        nav.vppSidebarLayoutFollowFrame = window.requestAnimationFrame(followFrame);
    }

    function ensureSidebarIndicator(nav) {
        if (!(nav instanceof Element)) {
            return null;
        }

        var indicator = Array.from(nav.children).find(function (child) {
            return child.classList && child.classList.contains("vpp-sidebar-shared-indicator");
        });

        if (!indicator) {
            indicator = document.createElement("span");
            indicator.className = "vpp-sidebar-shared-indicator";
            indicator.setAttribute("aria-hidden", "true");
            nav.appendChild(indicator);
        }

        if (!nav.vppSidebarIndicatorObserver) {
            nav.vppSidebarIndicatorObserver = new MutationObserver(function (mutations) {
                var hasExternalChange = mutations.some(function (mutation) {
                    return !mutation.target.classList
                        || !mutation.target.classList.contains("vpp-sidebar-shared-indicator");
                });
                if (!hasExternalChange) {
                    return;
                }

                var hasExpansionChange = mutations.some(function (mutation) {
                    return mutation.type === "attributes"
                        && mutation.attributeName === "aria-expanded";
                });
                if (hasExpansionChange) {
                    followSidebarIndicatorLayout(nav);
                    return;
                }

                scheduleSidebarIndicatorSync(nav, true);
            });
            nav.vppSidebarIndicatorObserver.observe(nav, {
                attributes: true,
                attributeFilter: ["class", "aria-current", "aria-expanded", "style"],
                childList: true,
                subtree: true
            });

            nav.vppSidebarScrollHandler = function () {
                scheduleSidebarIndicatorSync(nav, false);
            };
            nav.addEventListener("scroll", nav.vppSidebarScrollHandler, { passive: true });

            if (window.ResizeObserver) {
                nav.vppSidebarIndicatorResizeObserver = new ResizeObserver(function () {
                    moveSidebarIndicator(nav, findActiveSidebarTarget(nav), false);
                });
                nav.vppSidebarIndicatorResizeObserver.observe(nav);
            }

            nav.vppSidebarTransitionHandler = function (event) {
                if (event.target instanceof Element
                    && event.target.closest(".rz-navigation-menu")) {
                    scheduleSidebarIndicatorSync(nav, false);
                }
            };
            nav.addEventListener("transitionend", nav.vppSidebarTransitionHandler, true);
        }

        return indicator;
    }

    function scheduleSidebarIndicatorSync(nav, shouldAnimate) {
        nav.vppSidebarShouldAnimate = nav.vppSidebarShouldAnimate || shouldAnimate;
        if (!nav.vppSidebarSyncFrame) {
            nav.vppSidebarSyncFrame = window.requestAnimationFrame(function () {
                var animate = nav.vppSidebarShouldAnimate;
                nav.vppSidebarSyncFrame = null;
                nav.vppSidebarShouldAnimate = false;
                if (!nav.isConnected) {
                    return;
                }
                moveSidebarIndicator(nav, findActiveSidebarTarget(nav), animate, false);
            });
        }

        if (nav.vppSidebarSettleTimer) {
            window.clearTimeout(nav.vppSidebarSettleTimer);
        }
        nav.vppSidebarSettleTimer = window.setTimeout(function () {
            nav.vppSidebarSettleTimer = null;
            moveSidebarIndicator(nav, findActiveSidebarTarget(nav), false, false);
        }, sidebarIndicatorDuration + 60);
    }

    function moveSidebarIndicator(nav, target, shouldAnimate, forceTarget) {
        var indicator = ensureSidebarIndicator(nav);
        if (!indicator) {
            return;
        }

        if (!target || !nav.contains(target) || !hasVisibleAreaWithin(target, nav)) {
            hideSidebarIndicator(indicator);
            return;
        }

        var next = readSidebarIndicatorGeometry(nav, target);
        if (!next) {
            hideSidebarIndicator(indicator);
            return;
        }

        var navRect = nav.getBoundingClientRect();
        var indicatorRect = indicator.getBoundingClientRect();
        var current = {
            left: indicatorRect.left - navRect.left + nav.scrollLeft,
            top: indicatorRect.top - navRect.top + nav.scrollTop,
            height: indicatorRect.height
        };
        var isReady = indicator.classList.contains("is-ready");
        var isSamePosition = Math.abs(current.left - next.left) < 0.5
            && Math.abs(current.top - next.top) < 0.5
            && Math.abs(current.height - next.height) < 0.5;

        if (indicator.vppAnimation) {
            if (indicator.vppTarget === target || !forceTarget) {
                return;
            }

            indicator.vppAnimation.cancel();
            indicator.vppAnimation = null;
            indicator.vppTarget = null;
        }

        setSidebarIndicatorGeometry(indicator, next);

        if (!shouldAnimate || !isReady || isSamePosition || prefersReducedMotion()) {
            return;
        }

        indicator.vppTarget = target;
        indicator.vppAnimation = indicator.animate([
            {
                transform: "translate3d(" + current.left + "px, " + current.top + "px, 0)",
                height: current.height + "px"
            },
            {
                transform: "translate3d(" + next.left + "px, " + next.top + "px, 0)",
                height: next.height + "px"
            }
        ], {
            duration: sidebarIndicatorDuration,
            easing: navigationMotionEasing,
            fill: "none"
        });

        indicator.vppAnimation.addEventListener("finish", function () {
            indicator.vppAnimation = null;
            indicator.vppTarget = null;
            moveSidebarIndicator(nav, findActiveSidebarTarget(nav), false, false);
        }, { once: true });
    }

    function initializeSidebarIndicators(root) {
        if (root instanceof Element && root.matches(sidebarNavSelector)) {
            moveSidebarIndicator(root, findActiveSidebarTarget(root), false);
        }

        if (root.querySelectorAll) {
            root.querySelectorAll(sidebarNavSelector).forEach(function (nav) {
                moveSidebarIndicator(nav, findActiveSidebarTarget(nav), false);
            });
        }
    }

    function disposeSidebarNav(nav) {
        if (nav.vppSidebarIndicatorObserver) {
            nav.vppSidebarIndicatorObserver.disconnect();
            nav.vppSidebarIndicatorObserver = null;
        }

        if (nav.vppSidebarIndicatorResizeObserver) {
            nav.vppSidebarIndicatorResizeObserver.disconnect();
            nav.vppSidebarIndicatorResizeObserver = null;
        }

        if (nav.vppSidebarTransitionHandler) {
            nav.removeEventListener("transitionend", nav.vppSidebarTransitionHandler, true);
            nav.vppSidebarTransitionHandler = null;
        }

        if (nav.vppSidebarScrollHandler) {
            nav.removeEventListener("scroll", nav.vppSidebarScrollHandler);
            nav.vppSidebarScrollHandler = null;
        }

        if (nav.vppSidebarSettleTimer) {
            window.clearTimeout(nav.vppSidebarSettleTimer);
            nav.vppSidebarSettleTimer = null;
        }

        if (nav.vppSidebarLayoutFollowFrame) {
            window.cancelAnimationFrame(nav.vppSidebarLayoutFollowFrame);
            nav.vppSidebarLayoutFollowFrame = null;
        }
        if (nav.vppSidebarSyncFrame) {
            window.cancelAnimationFrame(nav.vppSidebarSyncFrame);
            nav.vppSidebarSyncFrame = null;
        }
        nav.vppSidebarShouldAnimate = false;
        nav.vppSidebarLayoutFollowUntil = 0;

        var indicator = Array.from(nav.children).find(function (child) {
            return child.classList?.contains("vpp-sidebar-shared-indicator");
        });
        if (indicator?.vppAnimation) {
            indicator.vppAnimation.cancel();
            indicator.vppAnimation = null;
            indicator.vppTarget = null;
        }
    }

    function disposeSidebarIndicators(root) {
        if (!(root instanceof Element)) {
            return;
        }

        if (root.matches(sidebarNavSelector)) {
            disposeSidebarNav(root);
        }

        root.querySelectorAll(sidebarNavSelector).forEach(disposeSidebarNav);
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

        moveTabIndicator(tabList, target, true, true);
    }, true);

    document.addEventListener("click", function (event) {
        var link = event.target instanceof Element
            ? event.target.closest(".vpp-sidebar .rz-navigation-item-link")
            : null;
        var nav = link ? link.closest(sidebarNavSelector) : null;

        if (!link || !nav || link.getAttribute("aria-disabled") === "true"
            || link.tagName !== "A" || !link.getAttribute("href")) {
            return;
        }

        moveSidebarIndicator(nav, sidebarTargetFromLink(nav, link), true, true);
    }, true);

    var interactionHostSelector = tabListSelector + ", " + sidebarNavSelector;
    var normalizationTargetSelector = "[data-vpp-grid-region='true'], "
        + "[aria-disabled*='ToString'], .rz-dropdown-panel";

    function isDataRowMutationRoot(node) {
        return node instanceof Element
            && node.matches("tr, td, tbody, .rz-data-row, .vpp-order-builder-virtual-row");
    }

    function containsInteractionHost(node) {
        if (!(node instanceof Element)) {
            return false;
        }

        if (isDataRowMutationRoot(node)) {
            return false;
        }

        return node.matches(interactionHostSelector)
            || node.querySelector(interactionHostSelector) !== null;
    }

    function containsNormalizationTarget(node) {
        if (!(node instanceof Element)) {
            return false;
        }

        if (node.matches(normalizationTargetSelector)) {
            return true;
        }

        return !isDataRowMutationRoot(node)
            && node.querySelector(normalizationTargetSelector) !== null;
    }

    function addMinimalMutationRoot(roots, node) {
        if (!(node instanceof Element)) {
            return;
        }

        for (var existing of roots) {
            if (existing.contains(node)) {
                return;
            }
            if (node.contains(existing)) {
                roots.delete(existing);
            }
        }
        roots.add(node);
    }

    var pendingAddedRoots = new Set();
    var pendingRemovedRoots = new Set();
    var mutationFlushFrame = null;

    function flushInteractionTreeMutations() {
        mutationFlushFrame = null;

        pendingRemovedRoots.forEach(function (root) {
            if (!containsInteractionHost(root)) {
                return;
            }
            disposeTabIndicators(root);
            disposeSidebarIndicators(root);
        });
        pendingRemovedRoots.clear();

        pendingAddedRoots.forEach(function (root) {
            if (containsNormalizationTarget(root)) {
                normalizeGridRegions(root);
                normalizeRadzenAriaValues(root);
                scheduleRadzenDropdownDirection(root);
            }
            if (containsInteractionHost(root)) {
                initializeTabIndicators(root);
                initializeSidebarIndicators(root);
            }
        });
        pendingAddedRoots.clear();
    }

    function scheduleInteractionTreeFlush() {
        if (mutationFlushFrame !== null) {
            return;
        }
        mutationFlushFrame = window.requestAnimationFrame(flushInteractionTreeMutations);
    }

    var interactionTreeObserver = new MutationObserver(function (mutations) {
        mutations.forEach(function (mutation) {
            mutation.removedNodes.forEach(function (node) {
                addMinimalMutationRoot(pendingRemovedRoots, node);
            });
            mutation.addedNodes.forEach(function (node) {
                addMinimalMutationRoot(pendingAddedRoots, node);
                if (node instanceof Element && containsNormalizationTarget(node)) {
                    scheduleRadzenDropdownDirection(node);
                }
            });
        });
        scheduleInteractionTreeFlush();
    });

    function startTabIndicators() {
        initializeTabIndicators(document);
        initializeSidebarIndicators(document);
        interactionTreeObserver.observe(document.documentElement, {
            childList: true,
            subtree: true
        });

        if (document.fonts && document.fonts.ready) {
            document.fonts.ready.then(function () {
                initializeTabIndicators(document);
                initializeSidebarIndicators(document);
            });
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", startTabIndicators, { once: true });
    } else {
        startTabIndicators();
    }

})();
