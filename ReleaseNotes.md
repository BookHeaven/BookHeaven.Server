# ⚠️ Breaking Changes
- A new property called "chapterProgress" has been added to the /progress/update api endpoint
- The properties "page", "pageCount", "pageCountPrev" and "pageCountNext" are now considered obsolete and will be removed in future versions.

The new property is meant to provide a more accurate representation of the current position inside the chapter, rather than just the page number which is not reliable when transfered between devices with different screen sizes or after changing the styling.

It is a float value between 0 and 1, where 0 represents the start of the chapter and 1 represents the end of the chapter.