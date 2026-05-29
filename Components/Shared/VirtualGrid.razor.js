let observer = null;
let mutationObserver = null;
let observedNodes = null;

export function observe(container, dotnetRef) {
    const options = {
        root: null,
        rootMargin: "500px",
        threshold: 0.01
    };

    let visibleSet = new Set();
    let lastVisible = { first: 0, last: 0 };
    observedNodes = new WeakSet();

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

    const observeNew = (node) => {
        if (!observedNodes.has(node)) {
            observedNodes.add(node);
            observer.observe(node);
        }
    };

    container.querySelectorAll("[data-virtual-index]").forEach(observeNew);

    mutationObserver = new MutationObserver(() => {
        container.querySelectorAll("[data-virtual-index]").forEach(observeNew);
    });

    mutationObserver.observe(container, { childList: true, subtree: true });
}

export function dispose() {
    if (observer) observer.disconnect();
    if (mutationObserver) mutationObserver.disconnect();
}
