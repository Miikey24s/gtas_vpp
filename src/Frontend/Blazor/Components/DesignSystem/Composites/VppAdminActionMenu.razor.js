let activePlacement;
const toggleProbes = new WeakMap();
const openingClass = 'vpp-admin-action-menu-opening';

function finishOpening() {
    document.documentElement.classList.remove(openingClass);
}

function findVisibleContextMenu() {
    return [...document.querySelectorAll('.rz-context-menu')]
        .filter(menu => menu instanceof HTMLElement
            && menu.isConnected
            && menu.getClientRects().length > 0)
        .at(-1);
}

function positionMenu(anchor, menu) {
    if (!(anchor instanceof HTMLElement) || !(menu instanceof HTMLElement)) return;

    const viewportGap = 8;
    const surfaceGap = 6;
    const anchorRect = anchor.getBoundingClientRect();

    menu.classList.add('vpp-admin-action-menu-surface');
    menu.style.setProperty('position', 'fixed', 'important');
    menu.style.setProperty('left', `${viewportGap}px`, 'important');
    menu.style.setProperty('top', `${viewportGap}px`, 'important');
    menu.style.setProperty('right', 'auto', 'important');
    menu.style.setProperty('bottom', 'auto', 'important');
    menu.style.setProperty('max-width', `calc(100vw - ${viewportGap * 2}px)`, 'important');

    const menuRect = menu.getBoundingClientRect();
    const spaceBelow = window.innerHeight - anchorRect.bottom - viewportGap;
    const spaceAbove = anchorRect.top - viewportGap;
    const openAbove = menuRect.height > spaceBelow && spaceAbove > spaceBelow;
    const preferredTop = openAbove
        ? anchorRect.top - menuRect.height - surfaceGap
        : anchorRect.bottom + surfaceGap;
    const preferredLeft = anchorRect.right - menuRect.width;
    const maxLeft = Math.max(viewportGap, window.innerWidth - menuRect.width - viewportGap);
    const maxTop = Math.max(viewportGap, window.innerHeight - menuRect.height - viewportGap);
    const targetLeft = Math.min(Math.max(viewportGap, preferredLeft), maxLeft);
    const targetTop = Math.min(Math.max(viewportGap, preferredTop), maxTop);

    menu.style.setProperty('left', `${targetLeft}px`, 'important');
    menu.style.setProperty('top', `${targetTop}px`, 'important');
    menu.classList.toggle('is-above', openAbove);
    menu.dataset.vppAdminActionMenuPositioned = 'true';
    finishOpening();
}

function clearActivePlacement(anchor) {
    if (anchor && activePlacement?.anchor !== anchor) return;
    const activeAnchor = activePlacement?.anchor;
    activePlacement?.dispose();
    activePlacement = undefined;
    removeToggleProbe(activeAnchor);
    finishOpening();
}

function ensureToggleProbe(anchor) {
    if (!(anchor instanceof HTMLElement) || toggleProbes.has(anchor)) return;

    const onPointerDown = () => {
        const menu = findVisibleContextMenu();
        anchor.dataset.vppAdminActionMenuWasOpen = activePlacement?.anchor === anchor
            && menu instanceof HTMLElement
            ? 'true'
            : 'false';
    };

    anchor.addEventListener('pointerdown', onPointerDown, true);
    toggleProbes.set(anchor, onPointerDown);
}

function removeToggleProbe(anchor) {
    if (!(anchor instanceof HTMLElement)) return;

    const onPointerDown = toggleProbes.get(anchor);
    if (onPointerDown) {
        anchor.removeEventListener('pointerdown', onPointerDown, true);
        toggleProbes.delete(anchor);
    }
    delete anchor.dataset.vppAdminActionMenuWasOpen;
}

export function placeAdminActionMenu(anchor, openedByPointer) {
    if (!(anchor instanceof HTMLElement)) return;

    clearActivePlacement();
    ensureToggleProbe(anchor);
    let frame = 0;
    let attempts = 0;
    let menu;

    const refresh = () => {
        if (frame) return;
        frame = window.requestAnimationFrame(() => {
            frame = 0;
            menu = findVisibleContextMenu();
            if (menu instanceof HTMLElement) {
                positionMenu(anchor, menu);
                if (openedByPointer) {
                    const focusedItem = menu.querySelector(':focus');
                    if (focusedItem instanceof HTMLElement) focusedItem.blur();
                }
                return;
            }

            attempts += 1;
            if (attempts < 12) {
                refresh();
            } else {
                finishOpening();
            }
        });
    };
    const onViewportChanged = () => {
        if (!(menu instanceof HTMLElement) || !menu.isConnected || menu.getClientRects().length === 0) {
            clearActivePlacement(anchor);
            return;
        }

        positionMenu(anchor, menu);
    };

    window.addEventListener('resize', onViewportChanged, { passive: true });
    window.addEventListener('scroll', onViewportChanged, { passive: true, capture: true });
    activePlacement = {
        anchor,
        dispose: () => {
            if (frame) window.cancelAnimationFrame(frame);
            window.removeEventListener('resize', onViewportChanged);
            window.removeEventListener('scroll', onViewportChanged, true);
        }
    };
    refresh();
}

export function prepareAdminActionMenu(anchor) {
    if (!(anchor instanceof HTMLElement)) return;
    document.documentElement.classList.add(openingClass);
}

export function consumeAdminActionMenuToggle(anchor) {
    if (!(anchor instanceof HTMLElement)) return false;

    const wasOpen = anchor.dataset.vppAdminActionMenuWasOpen === 'true';
    delete anchor.dataset.vppAdminActionMenuWasOpen;
    return wasOpen;
}

export function disposeAdminActionMenu(anchor) {
    clearActivePlacement(anchor);
    removeToggleProbe(anchor);
}
