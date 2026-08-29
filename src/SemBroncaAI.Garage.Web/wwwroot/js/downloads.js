window.sbgDownloadFile = (fileName, contentType, base64) => {
    const bytes = Uint8Array.from(atob(base64), character => character.charCodeAt(0));
    const url = URL.createObjectURL(new Blob([bytes], { type: contentType }));
    const anchor = document.createElement("a");
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    URL.revokeObjectURL(url);
};

window.sbgCopyText = async text => {
    try {
        if (navigator.clipboard?.writeText) {
            await navigator.clipboard.writeText(text);
            return true;
        }
    } catch {
        // Some mobile browsers deny Clipboard API even in a secure context.
    }

    if (typeof document.execCommand !== "function") return false;

    const input = document.createElement("textarea");
    try {
        input.value = text;
        input.setAttribute("readonly", "");
        input.style.position = "fixed";
        input.style.opacity = "0";
        document.body.appendChild(input);
        input.select();
        input.setSelectionRange(0, input.value.length);
        return document.execCommand("copy");
    } catch {
        return false;
    } finally {
        input.remove();
    }
};

window.sbgOpenUrl = url => {
    const opened = window.open(url, "_blank", "noopener,noreferrer");
    return opened !== null;
};
