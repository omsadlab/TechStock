window.printIframe = (id) => {
    const iframe = document.getElementById(id);
    if (!iframe) return;
    iframe.style.display = 'block';
    iframe.contentWindow.focus();
    iframe.contentWindow.print();
    iframe.style.display = 'none';
};

window.downloadFile = (filename, contentType, data) => {
    const blob = new Blob([data], { type: contentType });
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};
