// Dudiver Music — interop de audio, archivos y persistencia para Blazor.
(function () {
    const AUDIO_EXT = ['.mp3', '.wav', '.flac', '.m4a', '.aac', '.ogg', '.oga', '.opus', '.aiff', '.aif'];
    const isAudio = (name) => AUDIO_EXT.some(e => name.toLowerCase().endsWith(e));

    const files = new Map();   // id -> File/Blob en memoria (cache)
    let audio = null, dotnet = null, curUrl = null;

    // ===================== IndexedDB =====================
    let dbPromise = null;
    function db() {
        if (dbPromise) return dbPromise;
        dbPromise = new Promise((resolve, reject) => {
            const r = indexedDB.open('dudiver-music', 1);
            r.onupgradeneeded = () => {
                const d = r.result;
                if (!d.objectStoreNames.contains('blobs')) d.createObjectStore('blobs');
                if (!d.objectStoreNames.contains('meta')) d.createObjectStore('meta');
            };
            r.onsuccess = () => resolve(r.result);
            r.onerror = () => reject(r.error);
        });
        return dbPromise;
    }
    function idb(store, mode, fn) {
        return db().then(d => new Promise((resolve, reject) => {
            const tx = d.transaction(store, mode);
            const req = fn(tx.objectStore(store));
            tx.oncomplete = () => resolve(req && req.result);
            tx.onerror = () => reject(tx.error);
        }));
    }
    const idbPut = (store, key, val) => idb(store, 'readwrite', s => s.put(val, key));
    const idbGet = (store, key) => idb(store, 'readonly', s => s.get(key));
    const idbDel = (store, key) => idb(store, 'readwrite', s => s.delete(key));

    // id estable por archivo (mismo archivo -> mismo id -> sin duplicados, persiste)
    const fileId = (f) => `${f.name}|${f.size}|${f.lastModified}`;

    // ===================== Audio =====================
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

    async function getBlob(id) {
        if (files.has(id)) return files.get(id);
        const b = await idbGet('blobs', id);
        if (b) files.set(id, b);
        return b;
    }

    // Registra archivos: guarda el blob en IndexedDB y devuelve metadatos.
    async function register(fileList) {
        const out = [];
        for (const f of fileList) {
            if (!isAudio(f.name)) continue;
            const id = fileId(f);
            files.set(id, f);
            try { await idbPut('blobs', id, f); } catch { /* quota u otro */ }
            out.push({ id, name: f.name.replace(/\.[^.]+$/, ''), folder: (f.webkitRelativePath || '').split('/')[0] || '' });
        }
        out.sort((a, b) => a.name.localeCompare(b.name, undefined, { numeric: true, sensitivity: 'base' }));
        return out;
    }

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

    function readDuration(blob) {
        return new Promise((resolve) => {
            const url = URL.createObjectURL(blob);
            const a = document.createElement('audio');
            a.preload = 'metadata';
            a.onloadedmetadata = () => { const d = a.duration; URL.revokeObjectURL(url); resolve(isFinite(d) ? d : 0); };
            a.onerror = () => { URL.revokeObjectURL(url); resolve(0); };
            a.src = url;
        });
    }

    window.dudiverPlayer = {
        init(ref) { dotnet = ref; ensureAudio(); },

        async onInputChange(input, isFolder) {
            const list = await register(input.files || []);
            input.value = '';
            if (dotnet && list.length) await dotnet.invokeMethodAsync('OnFilesPicked', list, isFolder);
        },

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
                const list = await register(acc);
                if (dotnet && list.length) await dotnet.invokeMethodAsync('OnFilesDropped', list);
            });
        },

        // Duraciones en lote (con concurrencia limitada). Devuelve [{id, dur}].
        async getDurations(ids) {
            const out = [];
            const N = 4;
            for (let i = 0; i < ids.length; i += N) {
                const chunk = ids.slice(i, i + N);
                const res = await Promise.all(chunk.map(async id => {
                    const b = await getBlob(id);
                    return { id, dur: b ? await readDuration(b) : 0 };
                }));
                out.push(...res);
            }
            return out;
        },

        async play(id) {
            const b = await getBlob(id);
            if (!b) return false;
            ensureAudio();
            if (curUrl) URL.revokeObjectURL(curUrl);
            curUrl = URL.createObjectURL(b);
            audio.src = curUrl;
            audio.play().catch(() => { });
            return true;
        },
        resume() { audio && audio.play().catch(() => { }); },
        pause() { audio && audio.pause(); },
        seek(sec) { if (audio) audio.currentTime = sec; },
        setVolume(v) { ensureAudio(); audio.volume = Math.max(0, Math.min(1, v)); },

        // ---- Persistencia de la biblioteca ----
        async savePlaylists(json) { try { await idbPut('meta', 'playlists', json); } catch { } },
        async loadPlaylists() { try { return (await idbGet('meta', 'playlists')) || null; } catch { return null; } },
        async saveSettings(json) { try { await idbPut('meta', 'settings', json); } catch { } },
        async loadSettings() { try { return (await idbGet('meta', 'settings')) || null; } catch { return null; } },
        // Borra blobs que ya no usa ninguna playlist.
        async pruneBlobs(keepIds) {
            try {
                const keep = new Set(keepIds);
                const d = await db();
                await new Promise((resolve) => {
                    const tx = d.transaction('blobs', 'readwrite');
                    const st = tx.objectStore('blobs');
                    const cur = st.openCursor();
                    cur.onsuccess = () => {
                        const c = cur.result;
                        if (!c) return;
                        if (!keep.has(c.key)) { st.delete(c.key); files.delete(c.key); }
                        c.continue();
                    };
                    tx.oncomplete = resolve; tx.onerror = resolve;
                });
            } catch { }
        },

        supportsSink() { ensureAudio(); return typeof audio.setSinkId === 'function'; },
        supportsPicker() { return !!(navigator.mediaDevices && navigator.mediaDevices.selectAudioOutput); },
        async pickOutput() {
            ensureAudio();
            try {
                const dev = await navigator.mediaDevices.selectAudioOutput();
                if (dev && dev.deviceId) { await audio.setSinkId(dev.deviceId); return { id: dev.deviceId, name: dev.label || 'Salida' }; }
            } catch { }
            return null;
        },
        async getDevices() {
            try {
                try { const s = await navigator.mediaDevices.getUserMedia({ audio: true }); s.getTracks().forEach(t => t.stop()); } catch { }
                const devs = await navigator.mediaDevices.enumerateDevices();
                return devs.filter(d => d.kind === 'audiooutput' && d.deviceId).map(d => ({ id: d.deviceId, name: d.label || 'Salida' }));
            } catch { return []; }
        },
        async setSink(deviceId) {
            ensureAudio();
            if (typeof audio.setSinkId !== 'function') return false;
            try { await audio.setSinkId(deviceId || 'default'); return true; } catch { return false; }
        }
    };
})();
