window.sbgLoginSubmitting = function (form) {
    if (form.classList.contains("is-submitting")) return false;
    form.classList.add("is-submitting");
    form.setAttribute("aria-busy", "true");
    form.querySelector("button[type='submit']").disabled = true;
    return true;
};
