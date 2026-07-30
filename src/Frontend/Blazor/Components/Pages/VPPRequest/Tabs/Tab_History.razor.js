const historyViewportObservers = new WeakMap();
const historyChartLabelTimers = new WeakMap();
const historyChartLabelConfigs = new WeakMap();
const transientSurfaceSelector = [
    '.vpp-history-kpi-popover'
].join(',');

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

export function observeHistoryViewport(root, dotNetReference) {
    if (!root || historyViewportObservers.has(root)) return;

    let debounceId = 0;
    const onResize = () => {
        window.clearTimeout(debounceId);
        debounceId = window.setTimeout(() => {
            positionHistoryTransientSurfaces(root);
            scheduleHistoryChartLabels(root);
        }, 140);
    };
    const onScroll = () => positionHistoryTransientSurfaces(root);
    const onDocumentPointerDown = event => {
        const target = event.target;
        if (target instanceof Element && target.closest('.vpp-segmented-selector, .vpp-period-picker-popover, .vpp-history-kpi-card, .vpp-cell-value-popover')) return;
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
    });
    surfaceObserver.observe(root, { childList: true, subtree: true });
    positionHistoryTransientSurfaces(root);
    historyViewportObservers.set(root, () => {
        window.clearTimeout(debounceId);
        window.removeEventListener('resize', onResize);
        window.removeEventListener('scroll', onScroll, true);
        document.removeEventListener('pointerdown', onDocumentPointerDown, true);
        document.removeEventListener('keydown', onDocumentKeyDown, true);
        surfaceObserver.disconnect();
    });
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
