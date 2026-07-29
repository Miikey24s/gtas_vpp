export function sync(root, smooth) {
    if (!(root instanceof HTMLElement)) return;

    const active = root.querySelector('[data-vpp-segmented-active="true"]');
    if (!(active instanceof HTMLElement)) return;

    root.style.setProperty('--vpp-segmented-indicator-x', `${active.offsetLeft}px`);
    root.style.setProperty('--vpp-segmented-indicator-width', `${active.offsetWidth}px`);
    root.dataset.vppIndicatorReady = 'true';

    active.scrollIntoView({
        behavior: smooth && !window.matchMedia('(prefers-reduced-motion: reduce)').matches ? 'smooth' : 'auto',
        block: 'nearest',
        inline: 'nearest'
    });
}
