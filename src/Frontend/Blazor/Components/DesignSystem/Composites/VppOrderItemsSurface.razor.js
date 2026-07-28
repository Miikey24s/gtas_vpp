const orderItemsObservers = new WeakMap();
const transientSurfaceSelector = [
    '.vpp-history-select-menu',
    '.vpp-history-note-popover',
    '.vpp-history-detail-code-popover'
].join(',');

function updateScrollGutter(root) {
    const region = root?.querySelector('.vpp-order-items-grid-region');
    const surface = region?.querySelector('.vpp-order-items-grid .rz-data-grid-data');
    if (!(region instanceof HTMLElement) || !(surface instanceof HTMLElement)) return;

    const hasVerticalOverflow = surface.scrollHeight > surface.clientHeight + 1;
    surface.classList.toggle('has-vertical-overflow', hasVerticalOverflow);
    const scrollbarWidth = hasVerticalOverflow
        ? Math.max(0, surface.offsetWidth - surface.clientWidth)
        : 0;
    region.style.setProperty('--vpp-history-detail-scrollbar-width', `${scrollbarWidth}px`);
}

function positionTransientSurfaces(root) {
    if (!root) return;

    window.requestAnimationFrame(() => {
        root.querySelectorAll(transientSurfaceSelector).forEach(surface => {
            const anchor = surface.parentElement;
            if (!(surface instanceof HTMLElement) || !(anchor instanceof HTMLElement)) return;

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
            const preferredLeft = prefersEnd ? anchorRect.right - surfaceRect.width : anchorRect.left;
            const left = Math.min(
                Math.max(viewportGap, preferredLeft),
                Math.max(viewportGap, window.innerWidth - surfaceRect.width - viewportGap));
            const targetTop = Math.max(viewportGap, top);
            surface.style.setProperty('left', `${left}px`, 'important');
            surface.style.setProperty('top', `${targetTop}px`, 'important');

            // Một ancestor có transform có thể đổi containing block của phần tử fixed.
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
        });
    });
}

export function refreshOrderItemsSurface(root) {
    updateScrollGutter(root);
    positionTransientSurfaces(root);
}

export function observeOrderItemsSurface(root) {
    if (!root || orderItemsObservers.has(root)) return;

    const refresh = () => refreshOrderItemsSurface(root);
    const mutationObserver = new MutationObserver(refresh);
    mutationObserver.observe(root, { childList: true, subtree: true });
    const resizeObserver = new ResizeObserver(refresh);
    resizeObserver.observe(root);
    window.addEventListener('resize', refresh, { passive: true });
    window.addEventListener('scroll', refresh, { passive: true, capture: true });
    root.addEventListener('scroll', refresh, { passive: true, capture: true });
    orderItemsObservers.set(root, () => {
        mutationObserver.disconnect();
        resizeObserver.disconnect();
        window.removeEventListener('resize', refresh);
        window.removeEventListener('scroll', refresh, true);
        root.removeEventListener('scroll', refresh, true);
    });
    refresh();
}

export function disposeOrderItemsSurface(root) {
    orderItemsObservers.get(root)?.();
    orderItemsObservers.delete(root);
}
