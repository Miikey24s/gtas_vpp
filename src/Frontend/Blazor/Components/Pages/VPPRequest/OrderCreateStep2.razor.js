const observers = new WeakMap();

function refreshRows(root) {
    if (!(root instanceof HTMLElement)) return;

    const header = root.querySelector('.rz-grid-table thead');
    if (!(header instanceof HTMLElement)) return;
    const headerBottom = header.getBoundingClientRect().bottom;

    root.querySelectorAll('.rz-grid-table tbody > tr').forEach(row => {
        const rowRect = row.getBoundingClientRect();
        row.classList.toggle('vpp-virtual-row-under-header', rowRect.top < headerBottom - 0.5);
    });
}

export function observeVirtualRows(root) {
    if (!(root instanceof HTMLElement) || observers.has(root)) return;

    let frame = 0;
    const refresh = () => {
        window.cancelAnimationFrame(frame);
        frame = window.requestAnimationFrame(() => refreshRows(root));
    };
    const scroller = [...root.querySelectorAll('*')]
        .find(element => element.scrollHeight > element.clientHeight + 1
            && ['auto', 'scroll'].includes(getComputedStyle(element).overflowY));
    const mutationObserver = new MutationObserver(refresh);
    const resizeObserver = new ResizeObserver(refresh);
    mutationObserver.observe(root, { childList: true, subtree: true });
    resizeObserver.observe(root);
    scroller?.addEventListener('scroll', refresh, { passive: true });
    window.addEventListener('resize', refresh, { passive: true });
    observers.set(root, () => {
        window.cancelAnimationFrame(frame);
        mutationObserver.disconnect();
        resizeObserver.disconnect();
        scroller?.removeEventListener('scroll', refresh);
        window.removeEventListener('resize', refresh);
    });
    refresh();
}

export function disposeVirtualRows(root) {
    observers.get(root)?.();
    observers.delete(root);
}
