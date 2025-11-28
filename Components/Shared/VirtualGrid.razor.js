let observer = null;
let mutationObserver = null;

export function observe(container, dotnetRef) {
    const options = {
        root: null,
        rootMargin: "500px",
        threshold: 0.01
    };

    let visibleSet = new Set();
    let lastVisible = { first: 0, last: 0 };

    observer = new IntersectionObserver((entries) => {
        entries.forEach(entry => {
            const index = parseInt(entry.target.dataset.virtualIndex);
            if (entry.isIntersecting) {
                visibleSet.add(index);
            } else {
                visibleSet.delete(index);
            }
        });

        if (visibleSet.size === 0) return;

        const visible = Array.from(visibleSet).sort((a, b) => a - b);
        const first = visible[0];
        const last = visible[visible.length - 1];

        if (first !== lastVisible.first || last !== lastVisible.last) {
            lastVisible = { first, last };
            dotnetRef.invokeMethodAsync("OnVisibilityChanged", first, last);
        }
    }, options);

    const items = container.querySelectorAll("[data-virtual-index]");
    items.forEach(i => observer.observe(i));

    mutationObserver = new MutationObserver(() => {
        const items = container.querySelectorAll("[data-virtual-index]");
        items.forEach(i => observer.observe(i));
    });

    mutationObserver.observe(container, { childList: true, subtree: true });
}

export function dispose() {
    if (observer) observer.disconnect();
    if (mutationObserver) mutationObserver.disconnect();
}
