const observers = new WeakMap();
const positionFrames = new WeakMap();

function positionPanelNow(root) {
    if (!(root instanceof HTMLElement) || !root.isConnected) return;

    const trigger = root.querySelector('.vpp-cell-value-popover-trigger');
    const panel = root.querySelector('.vpp-cell-value-popover-panel');
    if (!(trigger instanceof HTMLElement) || !(panel instanceof HTMLElement)) return;

    const viewportGap = 8;
    const surfaceGap = 4;
    panel.classList.add('is-viewport-surface');
    panel.style.removeProperty('width');
    panel.style.setProperty('left', `${viewportGap}px`, 'important');
    panel.style.setProperty('top', `${viewportGap}px`, 'important');
    panel.style.setProperty('right', 'auto', 'important');
    panel.style.setProperty('bottom', 'auto', 'important');
    panel.style.setProperty('max-width', `calc(100vw - ${viewportGap * 2}px)`, 'important');

    const anchorRect = trigger.getBoundingClientRect();
    let panelRect = panel.getBoundingClientRect();
    if (panelRect.width > window.innerWidth - viewportGap * 2) {
        panel.style.setProperty('width', `${Math.max(160, window.innerWidth - viewportGap * 2)}px`, 'important');
        panelRect = panel.getBoundingClientRect();
    }

    const spaceBelow = window.innerHeight - anchorRect.bottom - viewportGap;
    const spaceAbove = anchorRect.top - viewportGap;
    const openAbove = panelRect.height > spaceBelow && spaceAbove > spaceBelow;
    const targetTop = openAbove
        ? Math.max(viewportGap, anchorRect.top - panelRect.height - surfaceGap)
        : Math.min(window.innerHeight - panelRect.height - viewportGap, anchorRect.bottom + surfaceGap);
    const prefersEnd = root.classList.contains('vpp-cell-value-popover-note');
    const preferredLeft = prefersEnd ? anchorRect.right - panelRect.width : anchorRect.left;
    const targetLeft = Math.min(
        Math.max(viewportGap, preferredLeft),
        Math.max(viewportGap, window.innerWidth - panelRect.width - viewportGap));

    panel.style.setProperty('left', `${targetLeft}px`, 'important');
    panel.style.setProperty('top', `${Math.max(viewportGap, targetTop)}px`, 'important');

    // Virtualized rows thường có transform; bù lại containing block để giữ panel theo viewport thật.
    const renderedRect = panel.getBoundingClientRect();
    panel.style.setProperty('left', `${targetLeft + targetLeft - renderedRect.left}px`, 'important');
    panel.style.setProperty('top', `${targetTop + targetTop - renderedRect.top}px`, 'important');

    const correctedRect = panel.getBoundingClientRect();
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
        panel.style.setProperty('left', `${(Number.parseFloat(panel.style.left) || 0) + horizontalCorrection}px`, 'important');
    }
    if (verticalCorrection !== 0) {
        panel.style.setProperty('top', `${(Number.parseFloat(panel.style.top) || 0) + verticalCorrection}px`, 'important');
    }
    panel.classList.toggle('is-above', openAbove);
}

function schedulePositionPanel(root) {
    if (!(root instanceof HTMLElement) || positionFrames.has(root)) return;
    const frame = window.requestAnimationFrame(() => {
        positionFrames.delete(root);
        positionPanelNow(root);
    });
    positionFrames.set(root, frame);
}

export function refreshCellValuePopover(root) {
    schedulePositionPanel(root);
}

export function observeCellValuePopover(root) {
    if (!(root instanceof HTMLElement) || observers.has(root)) return;

    const refresh = () => schedulePositionPanel(root);
    const mutationObserver = new MutationObserver(refresh);
    mutationObserver.observe(root, { childList: true, subtree: true });
    window.addEventListener('resize', refresh, { passive: true });
    window.addEventListener('scroll', refresh, { passive: true, capture: true });
    observers.set(root, () => {
        mutationObserver.disconnect();
        window.removeEventListener('resize', refresh);
        window.removeEventListener('scroll', refresh, true);
    });
}

export function disposeCellValuePopover(root) {
    observers.get(root)?.();
    observers.delete(root);
    const frame = positionFrames.get(root);
    if (frame) window.cancelAnimationFrame(frame);
    positionFrames.delete(root);
}
