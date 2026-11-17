window.dropzone = {
    init: function (containerId, inputId) {
        const container = document.getElementById(containerId);
        const input = document.getElementById(inputId);
        if (!container || !input) {
            console.warn('dropzone: container or input not found', containerId, inputId);
            return;
        }

        const setCopyEffect = (e) => { try { if (e.dataTransfer) e.dataTransfer.dropEffect = 'copy'; } catch (_) {} };

        const onDragOver = function (e) {
            e.preventDefault();
            setCopyEffect(e);
            container.classList.add('dz-dragging');
        };
        const onDragEnter = function (e) {
            e.preventDefault();
            setCopyEffect(e);
            container.classList.add('dz-dragging');
        };
        const onDragLeave = function (e) {
            container.classList.remove('dz-dragging');
        };
        const onDrop = function (e) {
            e.preventDefault();
            container.classList.remove('dz-dragging');
            if (!e.dataTransfer || !e.dataTransfer.files || e.dataTransfer.files.length === 0) return;
            try {
                // cria DataTransfer para popular o input (mais compatível que atribuir FileList diretamente)
                const dt = new DataTransfer();
                for (let i = 0; i < e.dataTransfer.files.length; i++) dt.items.add(e.dataTransfer.files[i]);

                try {
                    input.files = dt.files;
                } catch (err) {
                    // fallback: armazena em container para leitura via JS interop, se necessário
                    container._droppedFiles = dt.files;
                    console.warn('dropzone: input.files assignment falhou, usando container._droppedFiles', err);
                }

                input.dispatchEvent(new Event('change', { bubbles: true }));
            } catch (err) {
                console.error('dropzone drop error', err);
            }
        };

        // Anexa handlers tanto no container quanto no input (input sobreposto pode interceptar eventos)
        container.addEventListener('dragover', onDragOver);
        container.addEventListener('dragenter', onDragEnter);
        container.addEventListener('dragleave', onDragLeave);
        container.addEventListener('drop', onDrop);

        input.addEventListener('dragover', onDragOver);
        input.addEventListener('dragenter', onDragEnter);
        input.addEventListener('dragleave', onDragLeave);
        input.addEventListener('drop', onDrop);

        container._dropzone = { onDragOver, onDragEnter, onDragLeave, onDrop, input };
    },

    dispose: function (containerId) {
        const container = document.getElementById(containerId);
        if (container && container._dropzone) {
            const { onDragOver, onDragEnter, onDragLeave, onDrop, input } = container._dropzone;
            container.removeEventListener('dragover', onDragOver);
            container.removeEventListener('dragenter', onDragEnter);
            container.removeEventListener('dragleave', onDragLeave);
            container.removeEventListener('drop', onDrop);
            if (input) {
                input.removeEventListener('dragover', onDragOver);
                input.removeEventListener('dragenter', onDragEnter);
                input.removeEventListener('dragleave', onDragLeave);
                input.removeEventListener('drop', onDrop);
            }
            delete container._dropzone;
        }
    }
};