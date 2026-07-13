(function () {
    if (window.vppInteractionsInitialized) {
        return;
    }

    window.vppInteractionsInitialized = true;

    function prefersReducedMotion() {
        return window.matchMedia
            && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
    }

    window.vppTheme = {
        apply: function (theme) {
            var nextTheme = theme || "material3";
            var shouldUseDark = nextTheme.indexOf("dark") !== -1;

            function applyThemeClass() {
                document.documentElement.classList.toggle("rz-theme-dark", shouldUseDark);
                document.cookie = "VPPTheme=" + encodeURIComponent(nextTheme) + "; path=/; max-age=31536000";
            }

            if (document.startViewTransition && !prefersReducedMotion()) {
                document.startViewTransition(applyThemeClass);
                return;
            }

            applyThemeClass();
        }
    };

    window.vppLanguage = {
        prepareSwitch: function () {
            if (prefersReducedMotion()) {
                return Promise.resolve();
            }

            document.documentElement.classList.add("vpp-culture-changing");
            window.setTimeout(function () {
                document.documentElement.classList.remove("vpp-culture-changing");
            }, 500);

            return new Promise(function (resolve) {
                window.setTimeout(resolve, 140);
            });
        }
    };

    window.vppDownload = {
        fromStream: async function (fileName, contentStreamReference) {
            var arrayBuffer = await contentStreamReference.arrayBuffer();
            var blob = new Blob([arrayBuffer]);
            var url = URL.createObjectURL(blob);
            var anchor = document.createElement("a");
            anchor.href = url;
            anchor.download = fileName || "download";
            anchor.click();
            anchor.remove();
            URL.revokeObjectURL(url);
        }
    };

    function closeOrderCodePopovers(exceptCell) {
        document.querySelectorAll(".vpp-order-code-cell.is-open").forEach(function (cell) {
            if (cell === exceptCell) {
                return;
            }

            cell.classList.remove("is-open");
            var trigger = cell.querySelector(".vpp-order-code-trigger");
            if (trigger) {
                trigger.setAttribute("aria-expanded", "false");
            }
        });
    }

    document.addEventListener("click", function (event) {
        if (!(event.target instanceof Element)) {
            closeOrderCodePopovers();
            return;
        }

        var trigger = event.target.closest(".vpp-order-code-trigger");
        if (trigger) {
            var cell = trigger.closest(".vpp-order-code-cell");
            if (!cell) {
                return;
            }

            var shouldOpen = !cell.classList.contains("is-open");
            closeOrderCodePopovers(cell);
            cell.classList.toggle("is-open", shouldOpen);
            trigger.setAttribute("aria-expanded", shouldOpen ? "true" : "false");
            event.stopPropagation();
            return;
        }

        if (event.target.closest(".vpp-order-code-popover")) {
            return;
        }

        closeOrderCodePopovers();
    });

    document.addEventListener("keydown", function (event) {
        var trigger = event.target instanceof Element
            ? event.target.closest(".vpp-order-code-trigger")
            : null;
        if (trigger && (event.key === "Enter" || event.key === " ")) {
            event.preventDefault();
            trigger.click();
            return;
        }

        if (event.key === "Escape") {
            closeOrderCodePopovers();
        }
    });
})();
