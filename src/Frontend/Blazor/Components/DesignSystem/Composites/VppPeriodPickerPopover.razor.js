const observers = new WeakMap();

function position(root) {
    if (!(root instanceof HTMLElement)) return;
    const panel = root.querySelector('.vpp-period-picker-popover');
    const anchor = root.parentElement;
    if (!(panel instanceof HTMLElement) || !(anchor instanceof HTMLElement)) return;

    panel.classList.remove('is-above', 'is-align-end');
    const viewportGap = 8;
    const anchorRect = anchor.getBoundingClientRect();
    let panelRect = panel.getBoundingClientRect();
    const spaceBelow = window.innerHeight - anchorRect.bottom - viewportGap;
    const spaceAbove = anchorRect.top - viewportGap;
    if (panelRect.height > spaceBelow && spaceAbove > spaceBelow) {
        panel.classList.add('is-above');
        panelRect = panel.getBoundingClientRect();
    }

    if (panelRect.right > window.innerWidth - viewportGap) {
        panel.classList.add('is-align-end');
    }
}

export function observe(root, dotNetReference) {
    if (!(root instanceof HTMLElement) || observers.has(root)) return;

    const onPointerDown = event => {
        if (event.target instanceof Node && root.parentElement?.contains(event.target)) return;
        dotNetReference.invokeMethodAsync('DismissFromJsAsync').catch(() => {});
    };
    const onKeyDown = event => {
        if (event.key !== 'Escape') return;
        dotNetReference.invokeMethodAsync('DismissFromJsAsync').catch(() => {});
    };
    const refresh = () => window.requestAnimationFrame(() => position(root));

    document.addEventListener('pointerdown', onPointerDown, true);
    document.addEventListener('keydown', onKeyDown, true);
    window.addEventListener('resize', refresh, { passive: true });
    window.addEventListener('scroll', refresh, { passive: true, capture: true });
    refresh();

    observers.set(root, () => {
        document.removeEventListener('pointerdown', onPointerDown, true);
        document.removeEventListener('keydown', onKeyDown, true);
        window.removeEventListener('resize', refresh);
        window.removeEventListener('scroll', refresh, true);
    });
}

export function dispose(root) {
    observers.get(root)?.();
    observers.delete(root);
}
