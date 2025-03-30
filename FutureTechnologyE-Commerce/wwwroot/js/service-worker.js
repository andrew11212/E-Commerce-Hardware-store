// service-worker.js
const CACHE_NAME = 'futuretech-v1';
const urlsToCache = [
    '/',
    '/css/site.min.css',
    '/js/site.min.js',
    '/img/placeholder.svg'
];

self.addEventListener('install', event => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then(cache => cache.addAll(urlsToCache))
    );
});

self.addEventListener('fetch', event => {
    event.respondWith(
        caches.match(event.request)
            .then(response => response || fetch(event.request))
    );
});