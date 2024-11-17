using EpubManager;
using EpubManager.Entities;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Mvc;
using MudBlazor;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Services;
using BookHeaven.Server.Entities;
using BookHeaven.Server.Features.Authors;
using BookHeaven.Server.Features.Books;
using BookHeaven.Server.Features.BooksProgress;
using BookHeaven.Server.Features.Seriess;
using BookHeaven.Server.Interfaces;
using MediatR;

namespace BookHeaven.Server.Components.Pages.BookComponents
{
	public partial class BookDetails
	{
		[Inject] private ISender Sender { get; set; } = null!;
		[Inject] private IFormatService<EpubBook> EpubService { get; set; } = null!;
		[Inject] private IMetadataProviderService OpenLibraryService { get; set; } = null!;
		[Inject] private IEpubWriter EpubWriter { get; set; } = null!;

		[Parameter]
		public Guid Id { get; set; }

		private bool _isEditing = false;
		private bool _searchingMetadata = false;

		private Book? _book;
		private BookProgress _progress = null!;
		private List<Author> _authors = [];
		private List<Series> _series = [];
		private List<BookMetadata> _metadataList = [];

		private string? _newCoverTempPath;
		private string? _newEpubTempPath;

		private string? _authorName;
		private string? _seriesName;

		private readonly Converter<TimeSpan> _timeReadConverter = new()
		{
			SetFunc = value => $"{(int)value.TotalHours:00}:{value.Minutes:00}",
			GetFunc = text => text != null ? new(int.Parse(text.Split(":")[0]), int.Parse(text.Split(":")[1]), 0) : TimeSpan.Zero
		};

		protected override async Task OnParametersSetAsync()
		{
			if (Id != _book?.BookId)
			{
				
				var getAuthors = await Sender.Send(new GetAllAuthorsQuery());
				_authors = getAuthors.Value;
				
				var getSeries = await Sender.Send(new GetAllSeriesQuery());
				_series = getSeries.Value;

				await LoadBook();
			}
		}

		private async Task LoadBook()
		{
			_authorName = null;
			_seriesName = null;
			
			var getBook = await Sender.Send(new GetBookQuery(Id));
			if (getBook.IsFailure)
			{
				return;
			}
			_book = getBook.Value;
			
			var getBookProgress = await Sender.Send(new GetBookProgressByProfileQuery(Id, Program.SelectedProfile!.ProfileId));
			_progress = getBookProgress.IsFailure ? new BookProgress { BookId = Id, ProfileId = Program.SelectedProfile!.ProfileId } : getBookProgress.Value;
			
			if (_book.Author != null)
			{
				_authorName = _book.Author.Name;
			}
			if (_book.Series != null)
			{
				_seriesName = _book.Series.Name;
			}
		}

		private async Task UploadCoverToTempPath(IBrowserFile? file)
		{
			string tempPath = Path.Combine(Path.GetTempPath(), file.Name);
			await using (var stream = new FileStream(tempPath, FileMode.Create))
			{
				await file.OpenReadStream(maxAllowedSize: 1024 * 30000).CopyToAsync(stream);
			}
			_newCoverTempPath = tempPath;
		}

		private async Task UploadEpubToTempPath(IBrowserFile? file)
		{
			string tempPath = Path.Combine(Path.GetTempPath(), file.Name);
			await using (var stream = new FileStream(tempPath, FileMode.Create))
			{
				await file.OpenReadStream(maxAllowedSize: 1024 * 30000).CopyToAsync(stream);
			}
			_newEpubTempPath = tempPath;
		}

		private async Task Cancel()
		{
			await LoadBook();
			_isEditing = false;
		}

		private async Task Save()
		{
			if (!string.IsNullOrEmpty(_authorName))
			{
				if (_book.Author?.Name != _authorName)
				{
					var author = _authors.FirstOrDefault(a => a.Name == _authorName);
					if (author == null)
					{
						var createAuthor = await Sender.Send(new CreateAuthorCommand(_authorName));
						if(createAuthor.IsFailure)
						{
							throw new Exception(createAuthor.Error.Description);
						}

						author = createAuthor.Value;
					}
					_book!.AuthorId = author.AuthorId;
				}
			}
			else
			{
				_book.AuthorId = null;
			}

			if (!string.IsNullOrEmpty(_seriesName))
			{
				if (_book.Series?.Name != _seriesName)
				{
					var series = _series.FirstOrDefault(a => a.Name == _seriesName);
					if (series == null)
					{
						var createSeries = await Sender.Send(new CreateSeriesCommand(_seriesName));
						if(createSeries.IsFailure)
						{
							throw new Exception(createSeries.Error.Description);
						}
						series = createSeries.Value;
					}
					_book!.SeriesId = series.SeriesId;
				}
			}
			else
			{
				_book.SeriesId = null;
			}

			var updateBook = await Sender.Send(new UpdateBookCommand(_book, _progress));
			if(updateBook.IsFailure)
			{
				throw new Exception(updateBook.Error.Description);
			}
			if(_newCoverTempPath != null)
			{
				await EpubService.StoreCover(File.ReadAllBytes(_newCoverTempPath), _book.GetCoverPath(Program.CoversPath)!);
				File.Delete(_newCoverTempPath);
			}
			if(_newEpubTempPath != null)
			{
				await EpubService.StoreBook(_newEpubTempPath, _book.GetBookPath(Program.BooksPath)!);
				File.Delete(_newEpubTempPath);
			}

			await UpdateEpubFileMetadata();
			_isEditing = false;
		}

		private async Task UpdateEpubFileMetadata()
		{
			var metadata = new EpubMetadata
			{
				Title = _book.Title!,
				Authors = [_book.Author?.Name!],
				Publisher = _book.Publisher,
				Description = _book.Description,
				Language = _book.Language ?? string.Empty,
				PublishDate = _book.PublishedDate != null ? string.Concat(_book.PublishedDate.Value.ToString("s"), "Z") : null,
				Series = _book.Series?.Name,
				SeriesIndex = _book.SeriesIndex
			};

			await EpubWriter.ReplaceMetadata(_book.GetBookPath(Program.BooksPath)!, metadata);
			if(_newCoverTempPath != null)
			{
				await EpubWriter.ReplaceCover(_book.GetBookPath(Program.BooksPath)!, _book.GetCoverPath(Program.CoversPath)!);
			}
			_newCoverTempPath = null;
			_newEpubTempPath = null;
			
		}

		private async Task ShowMetadataDialog()
		{
			_metadataList = await OpenLibraryService.GetMetadataByName(_book.Title!);
			_searchingMetadata = true;
		}
	}
}