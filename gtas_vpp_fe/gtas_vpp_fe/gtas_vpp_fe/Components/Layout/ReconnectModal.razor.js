const reconnectModal = document.getElementById("components-reconnect-modal");
const retryButton = document.getElementById("components-reconnect-button");
const resumeButton = document.getElementById("components-resume-button");

reconnectModal.addEventListener("components-reconnect-state-changed", handleReconnectStateChanged);
retryButton.addEventListener("click", retry);
resumeButton.addEventListener("click", resume);

function handleReconnectStateChanged(event) {
    const state = event.detail.state;
    reconnectModal.dataset.reconnectState = state;
    reconnectModal.setAttribute("aria-busy", state === "show" || state === "retrying" ? "true" : "false");

    if (state === "hide") {
        document.removeEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
        retryButton.disabled = false;
        resumeButton.disabled = false;
        if (reconnectModal.open) {
            reconnectModal.close();
        }
        return;
    }

    if (state === "rejected") {
        location.reload();
        return;
    }

    if (!reconnectModal.open) {
        reconnectModal.showModal();
    }

    if (state === "failed") {
        document.removeEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
        document.addEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
        focusAction(retryButton);
    } else if (state === "paused" || state === "resume-failed") {
        focusAction(resumeButton);
    }
}

function focusAction(button) {
    requestAnimationFrame(() => button.focus({ preventScroll: true }));
}

async function retry() {
    document.removeEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
    retryButton.disabled = true;

    try {
        const successful = await Blazor.reconnect();
        if (!successful) {
            const resumeSuccessful = await Blazor.resumeCircuit();
            if (!resumeSuccessful) {
                location.reload();
            } else if (reconnectModal.open) {
                reconnectModal.close();
            }
        }
    } catch {
        document.addEventListener("visibilitychange", retryWhenDocumentBecomesVisible);
    } finally {
        retryButton.disabled = false;
    }
}

async function resume() {
    resumeButton.disabled = true;

    try {
        const successful = await Blazor.resumeCircuit();
        if (!successful) {
            location.reload();
        }
    } catch {
        reconnectModal.classList.replace("components-reconnect-paused", "components-reconnect-resume-failed");
        reconnectModal.dataset.reconnectState = "resume-failed";
        focusAction(resumeButton);
    } finally {
        resumeButton.disabled = false;
    }
}

async function retryWhenDocumentBecomesVisible() {
    if (document.visibilityState === "visible") {
        await retry();
    }
}
