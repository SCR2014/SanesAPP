window.SanesPrinting = {
    printReceipt: function () {
        const receipt =
            document.querySelector(
                ".sanes-print-receipt");

        if (!receipt) {
            console.error(
                "No printable receipt was found.");

            return;
        }

        const iframe =
            document.createElement("iframe");

        iframe.setAttribute(
            "aria-hidden",
            "true");

        iframe.style.position =
            "fixed";

        iframe.style.right =
            "0";

        iframe.style.bottom =
            "0";

        iframe.style.width =
            "0";

        iframe.style.height =
            "0";

        iframe.style.border =
            "0";

        document.body.appendChild(
            iframe);

        const printWindow =
            iframe.contentWindow;

        if (!printWindow) {
            iframe.remove();

            return;
        }

        const printDocument =
            printWindow.document;

        printDocument.open();

        printDocument.write(`
            <!DOCTYPE html>
            <html>
            <head>
                <meta charset="utf-8" />

                <meta
                    name="viewport"
                    content="width=device-width, initial-scale=1.0" />

                <link
                    rel="stylesheet"
                    href="/app.css" />

                <style>
                    html,
                    body {
                        width: 58mm !important;
                        margin: 0 !important;
                        padding: 0 !important;
                        background: white !important;
                    }

                    .sanes-print-receipt {
                        display: block !important;
                        position: static !important;
                    }
                </style>
            </head>

            <body>
                ${receipt.outerHTML}
            </body>
            </html>
        `);

        printDocument.close();

        let printed = false;

        const printNow = function () {
            if (printed) {
                return;
            }

            printed = true;

            printWindow.focus();
            printWindow.print();

            window.setTimeout(
                function () {
                    iframe.remove();
                },
                1000);
        };

        const stylesheet =
            printDocument.querySelector(
                'link[rel="stylesheet"]');

        if (stylesheet) {
            stylesheet.addEventListener(
                "load",
                function () {
                    window.setTimeout(
                        printNow,
                        100);
                });
        }

        window.setTimeout(
            printNow,
            750);
    }
};