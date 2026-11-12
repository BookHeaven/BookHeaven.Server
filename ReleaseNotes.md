### 🚀 Features

- *(openapi)* Add deletedAnnotationIds to required fields
- *(sdk)* Update sdk and dependencies to .net 11
- *(ProfilesPage)* Add Snackbar for error handling when loading profiles

### 🐛 Bug Fixes

- *(VirtualGrid)* Handle initialization errors in virtual grid setup
- *(ScrollToTop)* Handle potential errors when invoking scrollToTop
- *(Shelf)* Handle potential errors when saving scroll position
- *(GoBackButton)* Handle navigation errors with try-catch

### 🚜 Refactor

- Change leaf classes to sealed for better performance
- *(ApiDependencyInjection)* Improve method name retrieval with ordinal comparison
- *(ApiSaveKoreaderProgress)* Turn DocFragmentRegex to a GeneratedRegex
- *(CoverNotFoundMiddleware)* Path.Combine takes care of the slash, no need to replace
- *(ETagMiddleware)* Path.Combine takes care of the slash, no need to replace
- *(FontDialog)* Improve string comparison for font style and weight detection
- *(GoogleBooksProvider)* Replace List with static array for date formats and optimize service initialization
- *(MudDateOffsetPicker)* Change instance properties to static for performance optimization
- *(UdpBroadcastServer)* Await task for acknowledgment result
- *(UdpBroadcastServer)* Replace field with constant for max retries
- *(DuckDuckGoImageProvider)* Replace instance lists with static lists for improved performance
- *(DuckDuckGoImageProvider)* Streamline image fetching logic and improve HTTP client initialization
- *(Reader)* Implement new ReaderService flow
- *(Reader)* Implement new ReaderService api
- *(FetchCoversDialog)* Remove unnecessary StateHasChanged calls
- *(BookPage)* Remove unnecessary StateHasChanged calls
- *(AuthenticationService)* Simplify authentication flow and improve error handling
- *(ProfilesPage)* Improve layout and class structure for better readability
- *(Routes)* Change view transition implementation

