let chapter, chapterPrev, chapterNext;

let Reference = {};
Reference.DotNet = null;
let resizeObserver = null;
let resizeTimeout = null;

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

    chapter = document.getElementsByClassName('chapter current')[0];
    chapterPrev = document.getElementsByClassName('chapter prev')[0];
    chapterNext = document.getElementsByClassName('chapter next')[0];

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
    const columnGap = parseFloat(getComputedStyle(chapter).getPropertyValue('column-gap')) || 0;
    let pageWidth = chapter.offsetWidth + columnGap;

    const pagesPrev = chapterPrev && chapterPrev.childElementCount > 0 ? Math.ceil(parseFloat((chapterPrev.scrollWidth / pageWidth).toFixed(1))) : 0;
    const pages = Math.ceil(parseFloat((chapter.scrollWidth / pageWidth).toFixed(1)));
    const pagesNext = chapterNext && chapterNext.childElementCount > 0 ? Math.ceil(parseFloat((chapterNext.scrollWidth / pageWidth).toFixed(1))) : 0;

    return [pagesPrev, pages, pagesNext];
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