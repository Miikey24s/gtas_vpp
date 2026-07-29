export function ensureActiveVisible(root, animate) {
    if (!(root instanceof HTMLElement)) {
        return;
    }

    const activeStep = root.querySelector(".vpp-workflow-step.is-active");
    if (!(activeStep instanceof HTMLElement)) {
        return;
    }

    const rootRect = root.getBoundingClientRect();
    const activeRect = activeStep.getBoundingClientRect();
    const inset = 4;
    const isFullyVisible = activeRect.left >= rootRect.left + inset
        && activeRect.right <= rootRect.right - inset;
    if (isFullyVisible) {
        return;
    }

    const centeredOffset = activeRect.left - rootRect.left
        - ((rootRect.width - activeRect.width) / 2);
    const reducedMotion = window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    root.scrollTo({
        left: root.scrollLeft + centeredOffset,
        behavior: animate && !reducedMotion ? "smooth" : "auto"
    });
}
