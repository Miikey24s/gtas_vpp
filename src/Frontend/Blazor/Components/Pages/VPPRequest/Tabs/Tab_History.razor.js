const historyViewportObservers = new WeakMap();
const historyChartLabelTimers = new WeakMap();
const historyChartLabelFrames = new WeakMap();
const historyChartLabelConfigs = new WeakMap();
const historyPositionFrames = new WeakMap();
const historyChartLabelInitialDelayMs = 240;
const historyChartLabelRetryDelayMs = 100;
const historyChartLabelRetryLimit = 12;
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
    if (!config || !root?.isConnected) return true;
    if (!svg) return false;

    const series = [
        { selector: '.rz-series-0', values: config.regularValues, visible: config.showRegular, labelColor: '#ffffff' },
        { selector: '.rz-series-1', values: config.additionalValues, visible: config.showAdditional, labelColor: 'var(--vpp-text-primary)' }
    ];
    const labelsToRender = [];

    for (const { selector, values, visible, labelColor } of series) {
        if (!visible) continue;
        const positiveValues = values
            .map((value, index) => ({ index, value: Number(value ?? 0) }))
            .filter(({ value }) => Number.isFinite(value) && value > 0);
        if (positiveValues.length === 0) continue;

        const group = svg.querySelector(selector);
        if (!group) return false;
        const paths = [...group.querySelectorAll('path')];

        for (const { index, value } of positiveValues) {
            const path = paths[index];
            if (!path) return false;
            let bounds;
            try {
                bounds = path.getBBox();
            } catch {
                return false;
            }
            if (![bounds.x, bounds.y, bounds.width, bounds.height].every(Number.isFinite)
                || bounds.width <= 0
                || bounds.height <= 0) return false;
            if (bounds.width < 18 || bounds.height < 12) continue;
            const text = formatHistoryChartValue(value);
            const widthLimitedSize = (bounds.width - 8) / Math.max(1, text.length * .62);
            const heightLimitedSize = bounds.height - 4;
            const fontSize = Math.min(12, widthLimitedSize, heightLimitedSize);
            if (fontSize < 8.5) continue;

            labelsToRender.push({ group, bounds, text, fontSize, labelColor });
        }
    }

    svg.querySelectorAll('.vpp-history-chart-value-label').forEach(label => label.remove());
    labelsToRender.forEach(({ group, bounds, text, fontSize, labelColor }) => {
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

    return true;
}

function cancelHistoryChartLabelSchedule(root) {
    const previousTimer = historyChartLabelTimers.get(root);
    if (previousTimer !== undefined) window.clearTimeout(previousTimer);
    const previousFrame = historyChartLabelFrames.get(root);
    if (previousFrame !== undefined) window.cancelAnimationFrame(previousFrame);
    historyChartLabelTimers.delete(root);
    historyChartLabelFrames.delete(root);
}

function scheduleHistoryChartLabels(root, attempt = 0) {
    cancelHistoryChartLabelSchedule(root);
    const delay = attempt === 0
        ? historyChartLabelInitialDelayMs
        : historyChartLabelRetryDelayMs;
    const timer = window.setTimeout(() => {
        historyChartLabelTimers.delete(root);
        const frame = window.requestAnimationFrame(() => {
            historyChartLabelFrames.delete(root);
            const labelsReady = drawHistoryChartLabels(root);
            // Radzen có thể hoàn tất SVG sau lifecycle của component cha; chỉ thử lại trong giới hạn.
            if (!labelsReady && attempt < historyChartLabelRetryLimit) {
                scheduleHistoryChartLabels(root, attempt + 1);
            }
        });
        historyChartLabelFrames.set(root, frame);
    }, delay);
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

function positionHistoryTransientSurfacesNow(root) {
    if (!root?.isConnected) return;

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
}

function scheduleHistoryTransientSurfaces(root) {
    if (!root || historyPositionFrames.has(root)) return;
    const frame = window.requestAnimationFrame(() => {
        historyPositionFrames.delete(root);
        positionHistoryTransientSurfacesNow(root);
    });
    historyPositionFrames.set(root, frame);
}

export function observeHistoryViewport(root, dotNetReference) {
    if (!root || historyViewportObservers.has(root)) return;

    let debounceId = 0;
    const onResize = () => {
        window.clearTimeout(debounceId);
        debounceId = window.setTimeout(() => {
            scheduleHistoryTransientSurfaces(root);
            scheduleHistoryChartLabels(root);
        }, 140);
    };
    const onScroll = () => scheduleHistoryTransientSurfaces(root);
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
        scheduleHistoryTransientSurfaces(root);
    });
    surfaceObserver.observe(root, { childList: true, subtree: true });
    scheduleHistoryTransientSurfaces(root);
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
    if (dispose) dispose();
    if (!root) return;
    cancelHistoryChartLabelSchedule(root);
    const positionFrame = historyPositionFrames.get(root);
    if (positionFrame) window.cancelAnimationFrame(positionFrame);
    historyPositionFrames.delete(root);
    historyChartLabelConfigs.delete(root);
    historyViewportObservers.delete(root);
}

export function focusHistoryDrawer(drawer) {
    drawer?.focus({ preventScroll: true });
}
