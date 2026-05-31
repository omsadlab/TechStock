window.renderBarcode = (elementId, value) => {
    const el = document.getElementById(elementId);
    if (el && value && typeof JsBarcode !== 'undefined')
        JsBarcode(el, value, { format: 'CODE128', height: 40, displayValue: true, fontSize: 11 });
};

window.downloadFile = (filename, contentType, data) => {
    let blob;
    if (typeof data === 'string') {
        // byte[] from Blazor is JSON-serialized as base64
        const bin = atob(data);
        const buf = new Uint8Array(bin.length);
        for (let i = 0; i < bin.length; i++) buf[i] = bin.charCodeAt(i);
        blob = new Blob([buf], { type: contentType });
    } else {
        blob = new Blob([data], { type: contentType });
    }
    const url = URL.createObjectURL(blob);
    const a = document.createElement('a');
    a.href = url;
    a.download = filename;
    document.body.appendChild(a);
    a.click();
    document.body.removeChild(a);
    URL.revokeObjectURL(url);
};

window.openBarcodePrint = (html) => {
    const win = window.open('', '_blank');
    if (!win) { alert('Pop-up blocked. Please allow pop-ups for this site.'); return; }
    win.document.write(html);
    win.document.close();
};

window.printHtml = (html) => {
    const iframe = document.createElement('iframe');
    iframe.style.position = 'fixed';
    iframe.style.left = '-9999px';
    iframe.style.top = '-9999px';
    iframe.style.width = '0';
    iframe.style.height = '0';
    document.body.appendChild(iframe);
    const doc = iframe.contentDocument || iframe.contentWindow.document;
    doc.open();
    doc.write(html);
    doc.close();
    iframe.contentWindow.focus();
    iframe.contentWindow.print();
    setTimeout(() => { if (document.body.contains(iframe)) document.body.removeChild(iframe); }, 2000);
};
