(function () {
    if (window.vppInteractionsInitialized) {
        return;
    }

    window.vppInteractionsInitialized = true;

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
