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
    let scroller = null;
    const scheduleRefresh = () => {
        window.cancelAnimationFrame(frame);
        frame = window.requestAnimationFrame(() => refreshRows(root));
    };
    // Scroll phải cập nhật đồng bộ trước lần paint kế tiếp. Nếu đợi thêm một rAF,
    // cell hai dòng (tên + mã) có thể lộ một frame phía trên sticky header.
    const refreshImmediately = () => {
        window.cancelAnimationFrame(frame);
        refreshRows(root);
    };
    const connectScroller = () => {
        const nextScroller = [...root.querySelectorAll('*')]
            .find(element => element.scrollHeight > element.clientHeight + 1
                && ['auto', 'scroll'].includes(getComputedStyle(element).overflowY)) ?? null;
        if (nextScroller === scroller) return;
        scroller?.removeEventListener('scroll', refreshImmediately);
        scroller = nextScroller;
        scroller?.addEventListener('scroll', refreshImmediately, { passive: true });
    };
    const mutationObserver = new MutationObserver(() => {
        connectScroller();
        scheduleRefresh();
    });
    const resizeObserver = new ResizeObserver(scheduleRefresh);
    mutationObserver.observe(root, { childList: true, subtree: true });
    resizeObserver.observe(root);
    connectScroller();
    window.addEventListener('resize', scheduleRefresh, { passive: true });
    observers.set(root, () => {
        window.cancelAnimationFrame(frame);
        mutationObserver.disconnect();
        resizeObserver.disconnect();
        scroller?.removeEventListener('scroll', refreshImmediately);
        window.removeEventListener('resize', scheduleRefresh);
    });
    refreshImmediately();
}

export function disposeVirtualRows(root) {
    observers.get(root)?.();
    observers.delete(root);
}
