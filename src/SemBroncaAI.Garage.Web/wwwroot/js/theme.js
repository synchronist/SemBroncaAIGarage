window.sbgTheme = {
    getPreference: function () {
        const saved = localStorage.getItem("sbg-theme");
        return {
            isManual: saved === "light" || saved === "dark",
            isDark: saved === "dark"
        };
    },
    setPreference: function (isDark) {
        const value = isDark ? "dark" : "light";
        localStorage.setItem("sbg-theme", value);
        document.documentElement.dataset.sbgTheme = value;
    },
    applySystemPreference: function () {
        if (localStorage.getItem("sbg-theme") !== null) return;

        document.documentElement.dataset.sbgTheme =
            window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light";
    }
};

(function () {
    const saved = localStorage.getItem("sbg-theme");
    document.documentElement.dataset.sbgTheme = saved === "light" || saved === "dark"
        ? saved
        : (window.matchMedia("(prefers-color-scheme: dark)").matches ? "dark" : "light");

    window.matchMedia("(prefers-color-scheme: dark)").addEventListener("change", function (event) {
        if (localStorage.getItem("sbg-theme") === null)
            document.documentElement.dataset.sbgTheme = event.matches ? "dark" : "light";
    });
})();
