window.SanesDownloads = {
    downloadFileFromStream: async function (
        fileName,
        contentType,
        contentStreamReference) {

        const arrayBuffer =
            await contentStreamReference.arrayBuffer();

        const blob =
            new Blob(
                [arrayBuffer],
                { type: contentType });

        const url =
            URL.createObjectURL(blob);

        const anchor =
            document.createElement("a");

        anchor.href = url;
        anchor.download = fileName;

        document.body.appendChild(anchor);

        anchor.click();
        anchor.remove();

        URL.revokeObjectURL(url);
    }
};