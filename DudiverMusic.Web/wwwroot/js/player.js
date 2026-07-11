// Dudiver Music — interop de audio y archivos para Blazor.
(function () {
    const AUDIO_EXT = ['.mp3', '.wav', '.flac', '.m4a', '.aac', '.ogg', '.oga', '.opus', '.aiff', '.aif'];
    const isAudio = (name) => AUDIO_EXT.some(e => name.toLowerCase().endsWith(e));

    const files = new Map();   // id -> File
    let seq = 0;
    let audio = null;
    let dotnet = null;
    let curUrl = null;

    function ensureAudio() {
        if (audio) return audio;
        audio = new Audio();
        audio.preload = 'metadata';
        audio.addEventListener('timeupdate', () => dotnet && dotnet.invokeMethodAsync('OnTime', audio.currentTime || 0));
        audio.addEventListener('loadedmetadata', () => dotnet && dotnet.invokeMethodAsync('OnLoaded', audio.duration || 0));
        audio.addEventListener('ended', () => dotnet && dotnet.invokeMethodAsync('OnEnded'));
        audio.addEventListener('play', () => dotnet && dotnet.invokeMethodAsync('OnPlayState', true));
        audio.addEventListener('pause', () => dotnet && dotnet.invokeMethodAsync('OnPlayState', false));
        audio.addEventListener('error', () => dotnet && dotnet.invokeMethodAsync('OnEnded'));
        return audio;
    }

    function register(fileList) {
        const out = [];
        for (const f of fileList) {
            if (!isAudio(f.name)) continue;
            const id = 'f' + (++seq);
            files.set(id, f);
            out.push({ id, name: f.name.replace(/\.[^.]+$/, ''), folder: (f.webkitRelativePath || '').split('/')[0] || '' });
        }
        // orden natural
        out.sort((a, b) => a.name.localeCompare(b.name, undefined, { numeric: true, sensitivity: 'base' }));
        return out;
    }

    // Recorre entradas de un drop (soporta carpetas en Chromium).
    async function readEntry(entry, acc) {
        if (entry.isFile) {
            await new Promise(res => entry.file(f => { if (isAudio(f.name)) acc.push(f); res(); }, res));
        } else if (entry.isDirectory) {
            const reader = entry.createReader();
            await new Promise(res => {
                const readBatch = () => reader.readEntries(async (ents) => {
                    if (!ents.length) return res();
                    for (const e of ents) await readEntry(e, acc);
                    readBatch();
                }, res);
                readBatch();
            });
        }
    }

    function pickViaInput(attrs) {
        return new Promise((resolve) => {
            const inp = document.createElement('input');
            inp.type = 'file';
            inp.multiple = true;
            inp.accept = 'audio/*,' + AUDIO_EXT.join(',');
            for (const [k, v] of Object.entries(attrs || {})) inp.setAttribute(k, v);
            inp.style.display = 'none';
            document.body.appendChild(inp);
            inp.addEventListener('change', () => {
                const list = register(inp.files || []);
                inp.remove();
                resolve(list);
            }, { once: true });
            // cancelación: si el foco vuelve sin cambios, limpiar tras un rato
            inp.click();
        });
    }

    window.dudiverPlayer = {
        init(ref) { dotnet = ref; ensureAudio(); },

        pickFiles() { return pickViaInput(); },
        pickFolder() { return pickViaInput({ webkitdirectory: '', directory: '' }); },

        attachDrop(el) {
            if (!el) return;
            const stop = (e) => { e.preventDefault(); e.stopPropagation(); };
            el.addEventListener('dragover', (e) => { stop(e); el.classList.add('over'); });
            el.addEventListener('dragleave', (e) => { stop(e); el.classList.remove('over'); });
            el.addEventListener('drop', async (e) => {
                stop(e); el.classList.remove('over');
                const items = e.dataTransfer && e.dataTransfer.items;
                const acc = [];
                if (items && items.length && items[0].webkitGetAsEntry) {
                    const entries = [];
                    for (const it of items) { const en = it.webkitGetAsEntry && it.webkitGetAsEntry(); if (en) entries.push(en); }
                    for (const en of entries) await readEntry(en, acc);
                } else if (e.dataTransfer && e.dataTransfer.files) {
                    for (const f of e.dataTransfer.files) if (isAudio(f.name)) acc.push(f);
                }
                const list = register(acc);
                if (dotnet) await dotnet.invokeMethodAsync('OnFilesDropped', list);
            });
        },

        play(id) {
            const f = files.get(id);
            if (!f) return;
            ensureAudio();
            if (curUrl) URL.revokeObjectURL(curUrl);
            curUrl = URL.createObjectURL(f);
            audio.src = curUrl;
            audio.play().catch(() => { });
        },
        resume() { audio && audio.play().catch(() => { }); },
        pause() { audio && audio.pause(); },
        seek(sec) { if (audio) audio.currentTime = sec; },
        setVolume(v) { ensureAudio(); audio.volume = Math.max(0, Math.min(1, v)); },

        supportsSink() { ensureAudio(); return typeof audio.setSinkId === 'function'; },
        supportsPicker() { return !!(navigator.mediaDevices && navigator.mediaDevices.selectAudioOutput); },

        // Selector nativo (Chrome/Edge): abre el picker del sistema, otorga permiso
        // para ESE dispositivo y enruta el audio ahí. No pide micrófono.
        async pickOutput() {
            ensureAudio();
            try {
                const dev = await navigator.mediaDevices.selectAudioOutput();
                if (dev && dev.deviceId) {
                    await audio.setSinkId(dev.deviceId);
                    return { id: dev.deviceId, name: dev.label || 'Salida' };
                }
            } catch { /* cancelado o sin soporte */ }
            return null;
        },

        // Fallback: lista de dispositivos (requiere permiso para tener IDs reales).
        async getDevices() {
            try {
                try { const s = await navigator.mediaDevices.getUserMedia({ audio: true }); s.getTracks().forEach(t => t.stop()); } catch { }
                const devs = await navigator.mediaDevices.enumerateDevices();
                return devs.filter(d => d.kind === 'audiooutput' && d.deviceId)
                    .map(d => ({ id: d.deviceId, name: d.label || 'Salida' }));
            } catch { return []; }
        },
        async setSink(deviceId) {
            ensureAudio();
            if (typeof audio.setSinkId !== 'function') return false;
            try { await audio.setSinkId(deviceId || 'default'); return true; } catch { return false; }
        }
    };
})();
