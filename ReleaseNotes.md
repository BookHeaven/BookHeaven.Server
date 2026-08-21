# ⚠️ Breaking Changes
- The api endpoint for retrieving the progress of a book for a profile has been renamed from '/profiles' to '/progress'. The old endpoint is now marked as deprecated and will be removed in a future release.

# Features
- Added a new api endpoint to retrieve the list of collections.

# Improvements
- The shelf now does collection filtering directly on the database instead of filtering the books in memory.
- The api endpoint for retrieving the list of books now accepts a few parameters for filtering by collection or search term (title, author, series or tags).
- The enums used in the api are now properly documented in the API reference by showing the accepted values and their meaning.

For more information about the API changes please check out the [API reference](https://bookheaven.ggarrido.dev/api-reference).