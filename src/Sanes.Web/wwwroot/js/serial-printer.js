window.SanesSerialPrinter = (() => {

    let port = null;

    async function connect() {

        if (!("serial" in navigator)) {
            throw new Error(
                "Este navegador no soporta Web Serial.");
        }

        if (port && port.writable) {
            return;
        }

        port =
            await navigator.serial.requestPort();

        await port.open({
            baudRate: 9600,
            dataBits: 8,
            stopBits: 1,
            parity: "none",
            flowControl: "none"
        });
    }

    async function printBytes(data) {

        await connect();

        if (!port || !port.writable) {
            throw new Error(
                "El puerto serial no esta disponible.");
        }

        const writer =
            port.writable.getWriter();

        try {

            const bytes =
                data instanceof Uint8Array
                    ? data
                    : new Uint8Array(data);

            await writer.write(
                bytes);
        }
        finally {
            writer.releaseLock();
        }
    }

    async function disconnect() {

        if (!port) {
            return;
        }

        try {
            await port.close();
        }
        finally {
            port = null;
        }
    }

    return {
        connect,
        printBytes,
        disconnect
    };

})();