const historyViewportObservers = new WeakMap();
const historyChartLabelTimers = new WeakMap();
const historyChartLabelConfigs = new WeakMap();
const transientSurfaceSelector = [
    '.vpp-history-range-popover',
    '.vpp-history-kpi-popover',
    '.vpp-history-select-menu',
    '.vpp-history-code-popover',
    '.vpp-history-note-popover',
    '.vpp-history-detail-code-popover'
].join(',');

function updateHistoryScrollGutters(root) {
    if (!root) return;

    root.querySelectorAll('.rz-data-grid-data').forEach(surface => {
        const hasVerticalOverflow = surface.scrollHeight > surface.clientHeight + 1;
        surface.classList.toggle('has-vertical-overflow', hasVerticalOverflow);

        const detailRegion = surface.closest('.vpp-history-detail-grid-region');
        if (detailRegion) {
            const scrollbarWidth = hasVerticalOverflow
                ? Math.max(0, surface.offsetWidth - surface.clientWidth)
                : 0;
            detailRegion.style.setProperty('--vpp-history-detail-scrollbar-width', `${scrollbarWidth}px`);
        }
    });
}

function formatHistoryChartValue(value) {
    const locale = document.documentElement.lang || navigator.language || 'vi-VN';
    const formatter = Math.abs(value) >= 10_000
        ? new Intl.NumberFormat(locale, { notation: 'compact', maximumFractionDigits: 1 })
        : new Intl.NumberFormat(locale, { maximumFractionDigits: 0 });
    return formatter.format(value);
}

function drawHistoryChartLabels(root) {
    const config = historyChartLabelConfigs.get(root);
    const svg = root?.querySelector('.vpp-history-chart svg');
    if (!config || !svg) return;

    svg.querySelectorAll('.vpp-history-chart-value-label').forEach(label => label.remove());
    const series = [
        { selector: '.rz-series-0', values: config.regularValues, visible: config.showRegular, labelColor: '#ffffff' },
        { selector: '.rz-series-1', values: config.additionalValues, visible: config.showAdditional, labelColor: 'var(--vpp-text-primary)' }
    ];

    series.forEach(({ selector, values, visible, labelColor }) => {
        if (!visible) return;
        const group = svg.querySelector(selector);
        if (!group) return;
        const paths = [...group.querySelectorAll('path')];

        paths.forEach((path, index) => {
            const value = Number(values[index] ?? 0);
            if (!Number.isFinite(value) || value <= 0) return;

            const bounds = path.getBBox();
            if (bounds.width < 18 || bounds.height < 12) return;
            const text = formatHistoryChartValue(value);
            const widthLimitedSize = (bounds.width - 8) / Math.max(1, text.length * .62);
            const heightLimitedSize = bounds.height - 4;
            const fontSize = Math.min(12, widthLimitedSize, heightLimitedSize);
            if (fontSize < 8.5) return;

            const label = document.createElementNS('http://www.w3.org/2000/svg', 'foreignObject');
            label.classList.add('vpp-history-chart-value-label');
            label.setAttribute('x', `${bounds.x}`);
            label.setAttribute('y', `${bounds.y}`);
            label.setAttribute('width', `${bounds.width}`);
            label.setAttribute('height', `${bounds.height}`);
            label.setAttribute('aria-hidden', 'true');
            label.style.pointerEvents = 'none';
            const valueLabel = document.createElementNS('http://www.w3.org/1999/xhtml', 'div');
            valueLabel.style.display = 'flex';
            valueLabel.style.width = '100%';
            valueLabel.style.height = '100%';
            valueLabel.style.alignItems = 'center';
            valueLabel.style.justifyContent = 'center';
            valueLabel.style.overflow = 'hidden';
            valueLabel.style.color = labelColor;
            valueLabel.style.fontFamily = 'var(--vpp-font-sidebar)';
            valueLabel.style.fontSize = `${fontSize}px`;
            valueLabel.style.fontWeight = '600';
            valueLabel.style.lineHeight = '1';
            valueLabel.style.whiteSpace = 'nowrap';
            valueLabel.textContent = text;
            label.appendChild(valueLabel);
            group.appendChild(label);
        });
    });
}

function scheduleHistoryChartLabels(root) {
    const previousTimer = historyChartLabelTimers.get(root);
    if (previousTimer) window.clearTimeout(previousTimer);
    const timer = window.setTimeout(() => {
        window.requestAnimationFrame(() => drawHistoryChartLabels(root));
        historyChartLabelTimers.delete(root);
    }, 240);
    historyChartLabelTimers.set(root, timer);
}

export function renderHistoryChartLabels(root, regularValues, additionalValues, showRegular, showAdditional) {
    if (!root) return;
    historyChartLabelConfigs.set(root, {
        regularValues: regularValues ?? [],
        additionalValues: additionalValues ?? [],
        showRegular,
        showAdditional
    });
    scheduleHistoryChartLabels(root);
}

function positionHistoryTransientSurfaces(root) {
    if (!root) return;

    window.requestAnimationFrame(() => {
        root.querySelectorAll(transientSurfaceSelector).forEach(surface => {
            const anchor = surface.parentElement;
            if (!anchor) return;

            const isViewportSurface = surface.matches('.vpp-history-select-menu, .vpp-history-code-popover, .vpp-history-note-popover, .vpp-history-detail-code-popover');
            if (isViewportSurface) {
                surface.classList.add('is-viewport-surface');
                surface.style.removeProperty('width');
                surface.style.removeProperty('min-width');
                surface.style.setProperty('left', '8px', 'important');
                surface.style.setProperty('top', '8px', 'important');
                surface.style.setProperty('right', 'auto', 'important');
                surface.style.setProperty('bottom', 'auto', 'important');
                const trigger = surface.matches('.vpp-history-select-menu')
                    ? anchor.querySelector('.vpp-history-select-trigger')
                    : null;
                const anchorRect = (trigger ?? anchor).getBoundingClientRect();
                const viewportGap = 8;
                const surfaceGap = 4;
                surface.style.setProperty('max-width', `calc(100vw - ${viewportGap * 2}px)`, 'important');
                if (surface.matches('.vpp-history-select-menu')) {
                    surface.style.setProperty('width', 'max-content', 'important');
                    surface.style.setProperty('min-width', `${Math.ceil(anchorRect.width)}px`, 'important');
                }
                let surfaceRect = surface.getBoundingClientRect();
                if (surfaceRect.width > window.innerWidth - viewportGap * 2) {
                    surface.style.setProperty('width', `${Math.max(160, window.innerWidth - viewportGap * 2)}px`, 'important');
                    surfaceRect = surface.getBoundingClientRect();
                }
                const spaceBelow = window.innerHeight - anchorRect.bottom - viewportGap;
                const spaceAbove = anchorRect.top - viewportGap;
                const openAbove = surfaceRect.height > spaceBelow && spaceAbove > spaceBelow;
                const top = openAbove
                    ? Math.max(viewportGap, anchorRect.top - surfaceRect.height - surfaceGap)
                    : Math.min(window.innerHeight - surfaceRect.height - viewportGap, anchorRect.bottom + surfaceGap);
                const prefersEnd = surface.matches('.vpp-history-note-popover, .vpp-history-select-menu');
                const preferredLeft = prefersEnd
                    ? anchorRect.right - surfaceRect.width
                    : anchorRect.left;
                const left = Math.min(
                    Math.max(viewportGap, preferredLeft),
                    Math.max(viewportGap, window.innerWidth - surfaceRect.width - viewportGap));
                const targetTop = Math.max(viewportGap, top);
                surface.style.setProperty('left', `${left}px`, 'important');
                surface.style.setProperty('top', `${targetTop}px`, 'important');

                // A transformed ancestor can make a fixed element use a local
                // containing block. Correct the local coordinates against the
                // actual rendered rectangle, not only when it crosses an edge,
                // so the surface remains exactly anchored to its trigger.
                const renderedRect = surface.getBoundingClientRect();
                surface.style.setProperty('left', `${left + left - renderedRect.left}px`, 'important');
                surface.style.setProperty('top', `${targetTop + targetTop - renderedRect.top}px`, 'important');

                const correctedRect = surface.getBoundingClientRect();
                const horizontalCorrection = correctedRect.left < viewportGap
                    ? viewportGap - correctedRect.left
                    : correctedRect.right > window.innerWidth - viewportGap
                        ? window.innerWidth - viewportGap - correctedRect.right
                        : 0;
                const verticalCorrection = correctedRect.top < viewportGap
                    ? viewportGap - correctedRect.top
                    : correctedRect.bottom > window.innerHeight - viewportGap
                        ? window.innerHeight - viewportGap - correctedRect.bottom
                        : 0;
                if (horizontalCorrection !== 0) {
                    const currentLeft = Number.parseFloat(surface.style.left) || 0;
                    surface.style.setProperty('left', `${currentLeft + horizontalCorrection}px`, 'important');
                }
                if (verticalCorrection !== 0) {
                    const currentTop = Number.parseFloat(surface.style.top) || 0;
                    surface.style.setProperty('top', `${currentTop + verticalCorrection}px`, 'important');
                }
                surface.classList.toggle('is-above', openAbove);
                return;
            }

            surface.classList.remove('is-above', 'is-align-start', 'is-align-end');
            const anchorRect = anchor.getBoundingClientRect();
            let surfaceRect = surface.getBoundingClientRect();
            const viewportGap = 8;
            const spaceBelow = window.innerHeight - anchorRect.bottom - viewportGap;
            const spaceAbove = anchorRect.top - viewportGap;

            if (surfaceRect.height > spaceBelow && spaceAbove > spaceBelow) {
                surface.classList.add('is-above');
                surfaceRect = surface.getBoundingClientRect();
            }

            if (surfaceRect.right > window.innerWidth - viewportGap) {
                surface.classList.add('is-align-end');
                surfaceRect = surface.getBoundingClientRect();
            }

            if (surfaceRect.left < viewportGap) {
                surface.classList.remove('is-align-end');
                surface.classList.add('is-align-start');
            }
        });
    });
}

function getPageSize(root) {
    const height = window.innerHeight;
    const width = window.innerWidth;

    if (width < 768) return 4;
    if (width < 1280) {
        if (height >= 1100) return 8;
        if (height >= 800) return 6;
        if (height >= 680) return 4;
        return 3;
    }

    const card = root?.querySelector('.vpp-history-orders-card');
    if (!(card instanceof HTMLElement)) return 6;

    const style = getComputedStyle(card);
    const readSize = (name, fallback) => {
        const value = Number.parseFloat(style.getPropertyValue(name));
        return Number.isFinite(value) && value > 0 ? value : fallback;
    };
    const toolbar = card.querySelector('.vpp-history-orders-toolbar');
    const toolbarHeight = toolbar?.getBoundingClientRect().height ?? 42;
    const headerHeight = readSize('--vpp-history-grid-header-height', 40);
    const pagerHeight = readSize('--vpp-history-grid-pager-height', 42);
    const rowHeight = readSize('--vpp-history-grid-row-height', 40);
    const availableHeight = card.clientHeight - toolbarHeight - headerHeight - pagerHeight - 2;

    return Math.max(3, Math.min(20, Math.floor(availableHeight / rowHeight)));
}

export function observeHistoryViewport(root, dotNetReference) {
    if (!root || historyViewportObservers.has(root)) return;

    let lastPageSize = 0;
    let debounceId = 0;
    const notify = () => {
        const pageSize = getPageSize(root);
        if (pageSize === lastPageSize) return;
        lastPageSize = pageSize;
        dotNetReference.invokeMethodAsync('SetHistoryViewport', pageSize).catch(() => {});
    };
    const onResize = () => {
        window.clearTimeout(debounceId);
        debounceId = window.setTimeout(() => {
            notify();
            positionHistoryTransientSurfaces(root);
            updateHistoryScrollGutters(root);
            scheduleHistoryChartLabels(root);
        }, 140);
    };
    const onScroll = () => positionHistoryTransientSurfaces(root);
    const onDocumentPointerDown = event => {
        const target = event.target;
        if (target instanceof Element && target.closest('.vpp-history-scope, .vpp-history-select, .vpp-history-kpi-card, .vpp-history-note-cell, .vpp-history-code-cell, .vpp-history-detail-code, .vpp-history-detail-code-popover')) return;
        dotNetReference.invokeMethodAsync('CloseHistoryFilterMenuAsync').catch(() => {});
    };
    const onDocumentKeyDown = event => {
        if (event.key !== 'Escape') return;
        dotNetReference.invokeMethodAsync('CloseHistoryFilterMenuAsync').catch(() => {});
    };

    window.addEventListener('resize', onResize, { passive: true });
    window.addEventListener('scroll', onScroll, { passive: true, capture: true });
    document.addEventListener('pointerdown', onDocumentPointerDown, true);
    document.addEventListener('keydown', onDocumentKeyDown, true);
    const surfaceObserver = new MutationObserver(() => {
        positionHistoryTransientSurfaces(root);
        updateHistoryScrollGutters(root);
    });
    surfaceObserver.observe(root, { childList: true, subtree: true });
    const ordersCard = root.querySelector('.vpp-history-orders-card');
    const sizeObserver = ordersCard instanceof HTMLElement
        ? new ResizeObserver(() => {
            notify();
            updateHistoryScrollGutters(root);
        })
        : null;
    sizeObserver?.observe(ordersCard);
    updateHistoryScopeIndicator(root);
    positionHistoryTransientSurfaces(root);
    updateHistoryScrollGutters(root);
    historyViewportObservers.set(root, () => {
        window.clearTimeout(debounceId);
        window.removeEventListener('resize', onResize);
        window.removeEventListener('scroll', onScroll, true);
        document.removeEventListener('pointerdown', onDocumentPointerDown, true);
        document.removeEventListener('keydown', onDocumentKeyDown, true);
        surfaceObserver.disconnect();
        sizeObserver?.disconnect();
    });
    notify();
}

export function updateHistoryScopeIndicator(root) {
    const scope = root?.querySelector('.vpp-history-scope');
    const active = scope?.querySelector('button.is-active');
    const indicator = scope?.querySelector('.vpp-history-scope-indicator');
    if (!scope || !active || !indicator) return;

    const scopeRect = scope.getBoundingClientRect();
    const activeRect = active.getBoundingClientRect();
    scope.style.setProperty('--vpp-history-scope-indicator-x', `${activeRect.left - scopeRect.left}px`);
    scope.style.setProperty('--vpp-history-scope-indicator-width', `${activeRect.width}px`);
}

export function disposeHistoryViewport(root) {
    const dispose = root ? historyViewportObservers.get(root) : null;
    if (!dispose) return;
    dispose();
    const chartLabelTimer = historyChartLabelTimers.get(root);
    if (chartLabelTimer) window.clearTimeout(chartLabelTimer);
    historyChartLabelTimers.delete(root);
    historyChartLabelConfigs.delete(root);
    historyViewportObservers.delete(root);
}

export function focusHistoryDrawer(drawer) {
    drawer?.focus({ preventScroll: true });
}
