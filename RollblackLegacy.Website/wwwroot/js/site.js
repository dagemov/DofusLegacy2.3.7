document.addEventListener("htmx:afterSwap", function (event) {
    if (event.target && event.target.id === "register-panel-shell") {
        var firstInput = event.target.querySelector("input, textarea, select");
        if (firstInput && typeof firstInput.focus === "function") {
            firstInput.focus();
        }
    }
});
