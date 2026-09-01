let Reference = {};
Reference.DotNet = null;
let resizeObserver = null;
let resizeTimeout = null;

const waitForImagesToLoad = async () => {
    const images = Array.from(document.querySelectorAll('.chapter img, .chapter svg'));
    if (!images.length) {
        await new Promise((resolve) => requestAnimationFrame(() => resolve()));
        return;
    }

    await Promise.all(images.map(async (image) => {
        if (image.tagName === 'IMG') {
            if (image.complete) return;

            await new Promise((resolve) => {
                const cleanUp = () => {
                    image.removeEventListener('load', onDone);
                    image.removeEventListener('error', onDone);
                    resolve();
                };
                const onDone = () => cleanUp();

                image.addEventListener('load', onDone, { once: true });
                image.addEventListener('error', onDone, { once: true });
            });
            return;
        }

        if (image.tagName === 'SVG') {
            await new Promise((resolve) => requestAnimationFrame(() => resolve()));
        }
    }));

    await new Promise((resolve) => requestAnimationFrame(() => resolve()));
};

const keyDownHandler = (e) => {
    if (e.repeat) return;
    Reference.DotNet.invokeMethodAsync('OnKeyDown', e.key);
}

const notifyDebounced = (delay = 200) => {
    if (!Reference.DotNet) return;
    if (resizeTimeout) clearTimeout(resizeTimeout);
    resizeTimeout = setTimeout(() => {
        try {
            Reference.DotNet.invokeMethodAsync('OnViewportChanged');
        } catch (err) {
            // ignore invocation errors
        }
        resizeTimeout = null;
    }, delay);
}

export function SetDotNetReference(dotnet) {
    Reference.DotNet = dotnet;

    window.addEventListener('keydown', keyDownHandler);
    window.addEventListener('resize', notifyDebounced);
    window.addEventListener('orientationchange', notifyDebounced);

    const viewer = document.getElementById('viewer');
    if (viewer && typeof ResizeObserver !== 'undefined') {
        resizeObserver = new ResizeObserver(() => notifyDebounced());
        resizeObserver.observe(viewer);
    }
}

export async function GetPageCount() {
    const chapters = Array.from(document.querySelectorAll('.chapter.invisible'));
    if (!chapters.length) {
        return [];
    }
    await waitForImagesToLoad();
    
    const firstChapter = chapters[0];
    const pageGap = Number.parseFloat(getComputedStyle(firstChapter).columnGap) || 0;
    const pageWidth = Math.max(firstChapter.offsetWidth + pageGap, 1);

    return chapters.map((chapter) => Math.max(1, Math.ceil(chapter.scrollWidth / pageWidth)));
}

export function Dispose() {
    window.removeEventListener('keydown', keyDownHandler);
    window.removeEventListener('resize', notifyDebounced);
    window.removeEventListener('orientationchange', notifyDebounced);
    if (resizeObserver) {
        resizeObserver.disconnect();
        resizeObserver = null;
    }
    if (resizeTimeout) {
        clearTimeout(resizeTimeout);
        resizeTimeout = null;
    }
}