// SSE streaming helper for Blazor WASM
// Uses the browser's native fetch API with ReadableStream to avoid
// the synchronous read limitation of .NET HttpClient in WASM.

window.sseInterop = {
    /**
     * Performs a POST request and reads the SSE response stream,
     * invoking the .NET callback for each event.
     * @param {string} url - The endpoint URL
     * @param {string} bodyJson - The JSON request body
     * @param {object} dotNetRef - A DotNetObjectReference for callbacks
     */
    fetchSse: async function (url, bodyJson, dotNetRef) {
        try {
            const response = await fetch(url, {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: bodyJson
            });

            if (!response.ok) {
                await dotNetRef.invokeMethodAsync('OnSseError', `HTTP ${response.status}: ${response.statusText}`);
                return;
            }

            const reader = response.body.getReader();
            const decoder = new TextDecoder();
            let buffer = '';

            while (true) {
                const { done, value } = await reader.read();
                if (done) break;

                buffer += decoder.decode(value, { stream: true });

                // Parse SSE events from the buffer
                // Events are separated by double newlines
                const parts = buffer.split('\n\n');
                // The last part may be incomplete, keep it in the buffer
                buffer = parts.pop() || '';

                for (const part of parts) {
                    if (!part.trim()) continue;

                    let eventType = null;
                    let data = null;

                    for (const line of part.split('\n')) {
                        if (line.startsWith('event: ')) {
                            eventType = line.substring(7);
                        } else if (line.startsWith('data: ')) {
                            data = line.substring(6);
                        }
                    }

                    if (eventType && data) {
                        await dotNetRef.invokeMethodAsync('OnSseEvent', eventType, data);
                    }
                }
            }

            // Process any remaining data in the buffer
            if (buffer.trim()) {
                let eventType = null;
                let data = null;

                for (const line of buffer.split('\n')) {
                    if (line.startsWith('event: ')) {
                        eventType = line.substring(7);
                    } else if (line.startsWith('data: ')) {
                        data = line.substring(6);
                    }
                }

                if (eventType && data) {
                    await dotNetRef.invokeMethodAsync('OnSseEvent', eventType, data);
                }
            }

            await dotNetRef.invokeMethodAsync('OnSseComplete');
        } catch (error) {
            await dotNetRef.invokeMethodAsync('OnSseError', error.message || 'Unknown error');
        }
    }
};
