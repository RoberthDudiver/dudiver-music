// Dudiver Music — service worker (offline + instalable).
// Cache-first en runtime: tras la primera visita, la app abre sin internet.
const CACHE = 'dudiver-music';

self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', (e) => e.waitUntil(self.clients.claim()));

self.addEventListener('fetch', (e) => {
    const req = e.request;
    if (req.method !== 'GET') return;
    const url = new URL(req.url);
    if (url.origin !== location.origin) return;
    // version.json siempre desde la red (dispara el auto-update).
    if (url.pathname.endsWith('/version.json')) return;

    e.respondWith(
        caches.match(req).then((cached) =>
            cached || fetch(req).then((resp) => {
                if (resp && resp.ok) {
                    const copy = resp.clone();
                    caches.open(CACHE).then((c) => c.put(req, copy));
                }
                return resp;
            }).catch(() => cached)
        )
    );
});
