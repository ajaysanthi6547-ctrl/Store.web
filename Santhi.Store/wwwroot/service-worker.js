
const CACHE_NAME = "santhi-store-v1";

// Install service worker and save offline page
self.addEventListener("install", event => {
    event.waitUntil(
        caches.open(CACHE_NAME).then(cache => {
            return cache.add("/offline.html");
        })
    );

    self.skipWaiting();
});

// Activate service worker
self.addEventListener("activate", event => {
    event.waitUntil(
        self.clients.claim()
    );
});

// Handle page requests
self.addEventListener("fetch", event => {
    if (event.request.method !== "GET") {
        return;
    }

    if (event.request.mode === "navigate") {
        event.respondWith(
            fetch(event.request).catch(async () => {
                return await caches.match("/offline.html");
            })
        );
    }
});
