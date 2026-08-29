window.sbgLoginSubmitting = function (form) {
    if (form.classList.contains("is-submitting")) return false;
    form.classList.add("is-submitting");
    form.setAttribute("aria-busy", "true");
    const submit = form.querySelector("button[type='submit'], input[type='submit']");
    if (submit.dataset.loadingText) {
        if (submit instanceof HTMLInputElement) {
            submit.value = submit.dataset.loadingText;
        } else {
            submit.textContent = submit.dataset.loadingText;
        }
    }
    submit.disabled = true;
    return true;
};

window.sbgTogglePassword = function (inputId, button) {
    const input = document.getElementById(inputId);
    if (!input) return;
    const showing = input.type === "text";
    input.type = showing ? "password" : "text";
    const label = showing ? "Mostrar senha" : "Ocultar senha";
    button.setAttribute("aria-label", label);
    button.setAttribute("title", label);
};

window.sbgPreventImplicitLongFormSubmit = function (event) {
    if (event.key !== "Enter") return true;
    event.preventDefault();
    return false;
};

window.sbgReadPlatformGarageForm = function () {
    const value = id => document.getElementById(id)?.value ?? "";
    return {
        name: value("garage-name"), document: value("garage-document"),
        phone: value("garage-phone"), email: value("garage-email"),
        ownerName: value("owner-name"), ownerEmail: value("owner-email"),
        ownerUserName: value("owner-username")
    };
};

document.addEventListener("keydown", function (event) {
    if (!(event.target instanceof HTMLInputElement)) return;
    if (!event.target.form?.hasAttribute("data-prevent-implicit-submit")) return;
    window.sbgPreventImplicitLongFormSubmit(event);
}, true);

window.sbgValidateResetForm = function (form) {
    const password = form.elements.password.value;
    const confirmation = form.elements.confirmPassword.value;
    const error = document.getElementById("reset-form-error");
    if (password !== confirmation) {
        error.textContent = "As senhas não coincidem.";
        error.hidden = false;
        form.elements.confirmPassword.focus();
        return false;
    }
    error.hidden = true;
    return window.sbgLoginSubmitting(form);
};

window.sbgMaskBrazilianPhone = function (input) {
    const digits = input.value.replace(/\D/g, "").slice(0, 11);
    if (digits.length <= 2) input.value = digits ? `(${digits}` : "";
    else if (digits.length <= 6) input.value = `(${digits.slice(0, 2)}) ${digits.slice(2)}`;
    else if (digits.length <= 10) input.value = `(${digits.slice(0, 2)}) ${digits.slice(2, 6)}-${digits.slice(6)}`;
    else input.value = `(${digits.slice(0, 2)}) ${digits.slice(2, 7)}-${digits.slice(7)}`;
};

window.sbgMaskDocument = function (input) {
    const digits = input.value.replace(/\D/g, "").slice(0, 14);
    if (digits.length <= 11) {
        input.value = digits.replace(/^(\d{3})(\d)/, "$1.$2").replace(/^(\d{3})\.(\d{3})(\d)/, "$1.$2.$3").replace(/\.(\d{3})(\d)/, ".$1-$2");
        return;
    }
    input.value = digits.replace(/^(\d{2})(\d)/, "$1.$2").replace(/^(\d{2})\.(\d{3})(\d)/, "$1.$2.$3").replace(/\.(\d{3})(\d)/, ".$1/$2").replace(/(\/\d{4})(\d)/, "$1-$2");
};
