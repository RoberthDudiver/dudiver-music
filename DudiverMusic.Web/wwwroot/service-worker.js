// Dudiver Music — service worker.
// Estrategia:
//  - Assets con fingerprint de Blazor (/_framework/): cache-first (la URL cambia si
//    cambia el contenido, así que cachearlos para siempre es seguro y rápido/offline).
//  - Todo lo demás (index.html, js/player.js, css/app.css, imágenes): NETWORK-FIRST.
//    Online siempre trae lo último (nada de mezclar viejo con nuevo); la caché queda
//    solo como respaldo offline. Esto evita el desajuste JS/WASM que crasheaba la app.
const CACHE = 'dudiver-music-v3';

self.addEventListener('install', () => self.skipWaiting());

self.addEventListener('activate', (e) => e.waitUntil((async () => {
    // Borrar cualquier caché de versiones anteriores.
    for (const k of await caches.keys()) if (k !== CACHE) await caches.delete(k);
    await self.clients.claim();
})()));

self.addEventListener('fetch', (e) => {
    const req = e.request;
    if (req.method !== 'GET') return;
    const url = new URL(req.url);
    if (url.origin !== location.origin) return;
    // version.json siempre desde la red (dispara el auto-update).
    if (url.pathname.endsWith('/version.json')) return;

    // Assets inmutables con fingerprint → cache-first.
    if (url.pathname.startsWith('/_framework/')) {
        e.respondWith(caches.match(req).then((cached) =>
            cached || fetch(req).then((resp) => {
                if (resp && resp.ok) { const copy = resp.clone(); caches.open(CACHE).then((c) => c.put(req, copy)); }
                return resp;
            })
        ));
        return;
    }

    // Resto → network-first, con la caché como respaldo offline.
    e.respondWith((async () => {
        try {
            const resp = await fetch(req);
            if (resp && resp.ok) { const copy = resp.clone(); caches.open(CACHE).then((c) => c.put(req, copy)); }
            return resp;
        } catch {
            const cached = await caches.match(req);
            if (cached) return cached;
            // navegación offline sin caché → intentar el index.
            if (req.mode === 'navigate') { const idx = await caches.match('/index.html'); if (idx) return idx; }
            return Response.error();
        }
    })());
});
