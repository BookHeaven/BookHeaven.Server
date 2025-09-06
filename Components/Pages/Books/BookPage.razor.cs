using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Extensions;
using BookHeaven.Domain.Features.Authors;
using BookHeaven.Domain.Features.Books;
using BookHeaven.Domain.Features.BooksProgress;
using BookHeaven.Domain.Features.Seriess;
using BookHeaven.Domain.Features.Tags;
using BookHeaven.Server.Abstractions;
using BookHeaven.Server.Constants;
using BookHeaven.Server.Entities;
using BookHeaven.EpubManager.Epub.Entities;
using BookHeaven.EpubManager.Epub.Services;
using BookHeaven.Server.Components.Dialogs;
using BookHeaven.Server.MetadataProviders.DTO;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;
using MudBlazor;

namespace BookHeaven.Server.Components.Pages.Books;

public partial class BookPage
{
	[Inject] private ISender Sender { get; set; } = null!;
	[Inject] private IFormatService<EpubBook> EpubService { get; set; } = null!;
	[Inject] private IEpubWriter EpubWriter { get; set; } = null!;
	[Inject] private NavigationManager NavigationManager { get; set; } = null!;
	[Inject] private ISettingsManagerService SettingsManager { get; set; } = null!;
	[Inject] private ISessionService SessionService { get; set; } = null!;
	[Inject] private IDialogService DialogService { get; set; } = null!;

	[Parameter] public Guid Id { get; set; }
	[Parameter] public string? Editing { get; set; }

	private Guid _profileId;
	private ServerSettings _settings = new();

	private bool _addingTags;
	private string _tagNames = string.Empty;
		
	private bool IsEditing => Editing == "edit";
		
	private bool CoverExists => File.Exists(_book.CoverPath());

	private Book _book = new();
	private List<Author> _authors = [];
	private List<Series> _series = [];
	

	private string? _newCoverTempPath;
	private string? _newEpubTempPath;

	private string? _authorName;
	private string? _seriesName;

	private readonly Converter<TimeSpan> _timeReadConverter = new()
	{
		SetFunc = value => $"{(int)value.TotalHours:00}:{value.Minutes:00}",
		GetFunc = text => text != null ? new(int.Parse(text.Split(":")[0]), int.Parse(text.Split(":")[1]), 0) : TimeSpan.Zero
	};

	protected override async Task OnInitializedAsync()
	{
		_settings = await SettingsManager.LoadSettingsAsync();
		_profileId = await SessionService.GetAsync<Guid>(SessionKey.SelectedProfileId);
	}

	protected override async Task OnParametersSetAsync()
	{
		if (_book.BookId == Guid.Empty || Id != _book.BookId)
		{
				
			var getAuthors = await Sender.Send(new GetAllAuthors.Query());
			_authors = getAuthors.Value;
				
			var getSeries = await Sender.Send(new GetAllSeries.Query());
			_series = getSeries.Value;

			await LoadBook();
		}
	}

	private void EnableEditing()
	{
		NavigationManager.NavigateTo($"{Urls.GetBookUrl(_book.BookId)}/edit");
	}

	private void DisableEditing()
	{
		NavigationManager.NavigateTo(Urls.GetBookUrl(_book.BookId));
	} 

	private async Task LoadBook()
	{
		_authorName = null;
		_seriesName = null;
			
		var getBook = await Sender.Send(new GetBook.Query(Id));
		if (getBook.IsFailure)
		{
			return;
		}
		_book = getBook.Value;
			
		var getBookProgress = await Sender.Send(new GetBookProgressByProfile.Query(Id, _profileId));
		_book.Progresses.Add(getBookProgress.Value);
			
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
		if (file == null) return;
			
		var tempPath = Path.Combine(Path.GetTempPath(), file.Name);
		await using (var stream = new FileStream(tempPath, FileMode.Create))
		{
			await file.OpenReadStream(maxAllowedSize: 1024 * 30000).CopyToAsync(stream);
		}
		_newCoverTempPath = tempPath;
	}

	private async Task UploadEpubToTempPath(IBrowserFile? file)
	{
		if(file == null) return;
			
		var tempPath = Path.Combine(Path.GetTempPath(), file.Name);
		await using (var stream = new FileStream(tempPath, FileMode.Create))
		{
			await file.OpenReadStream(maxAllowedSize: 1024 * 30000).CopyToAsync(stream);
		}
		_newEpubTempPath = tempPath;
	}

	private async Task Save()
	{
		if (!string.IsNullOrEmpty(_authorName))
		{
			if (_book.Author?.Name != _authorName)
			{
				var author = _authors.FirstOrDefault(a => a.Name == _authorName) ??
				             new Author
				             {
					             Name = _authorName
				             };;
				_book.AuthorId = author.AuthorId;
				_book.Author = author;
			}
		}
		else
		{
			_book.AuthorId = null;
			_book.Author = null;
		}

		if (!string.IsNullOrEmpty(_seriesName))
		{
			if (_book.Series?.Name != _seriesName)
			{
				var series = _series.FirstOrDefault(a => a.Name == _seriesName) ??
				             new Series
				             {
					             Name = _seriesName
				             };
				_book.SeriesId = series.SeriesId;
				_book.Series = series;
			}
		}
		else
		{
			_book.SeriesId = null;
			_book.Series = null;
		}

		var updateBook = await Sender.Send(new UpdateBook.Command(_book));
		if(updateBook.IsFailure)
		{
			throw new Exception(updateBook.Error.Description);
		}
		if(_book.Progress() is { EndDate: not null, Progress: < 100 })
		{
			_book.Progress().Progress = 100;
		}
		var updateProgress = await Sender.Send(new UpdateBookProgress.Command(_book.Progress()));
		if(updateProgress.IsFailure)
		{
			throw new Exception(updateProgress.Error.Description);
		}
			
		if(!string.IsNullOrWhiteSpace(_newCoverTempPath))
		{
			if (_newCoverTempPath.StartsWith("http"))
			{
				await EpubService.DownloadAndStoreCoverAsync(_newCoverTempPath, _book.CoverPath());
			}
			else
			{
				await EpubService.StoreCover(await File.ReadAllBytesAsync(_newCoverTempPath), _book.CoverPath());
				File.Delete(_newCoverTempPath);
			}
			
		}
		if(_newEpubTempPath != null)
		{
			await EpubService.StoreBook(_newEpubTempPath, _book.EpubPath());
			File.Delete(_newEpubTempPath);
		}

		await UpdateEpubFileMetadata();
		DisableEditing();
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

		await EpubWriter.ReplaceMetadataAsync(_book.EpubPath(), metadata);
		if(_newCoverTempPath != null)
		{
			await EpubWriter.ReplaceCoverAsync(_book.EpubPath(), _book.CoverPath());
		}
		_newCoverTempPath = null;
		_newEpubTempPath = null;
			
	}

	private async Task ShowMetadataDialog()
	{
		var dialogParameters = new DialogParameters
		{
			{ nameof(FetchMetadataDialog.Book), _book }
		};
		await DialogService.ShowAsync<FetchMetadataDialog>(null, dialogParameters);
	}

	private async Task ShowFetchCoversDialog()
	{
		var dialogParameters = new DialogParameters
		{
			{ nameof(FetchCoversDialog.Title), _book.Title },
			{ nameof(FetchCoversDialog.Author), _book.Author?.Name ?? string.Empty }
		};
		var dialog = await DialogService.ShowAsync<FetchCoversDialog>(null, dialogParameters);
		var result = await dialog.Result;
		if (result?.Canceled == false && !string.IsNullOrWhiteSpace(result?.Data as string))
		{
			_newCoverTempPath = result.Data as string;
			StateHasChanged();
		}
	}
		
	private async Task AddTagToBook()
	{
		if (string.IsNullOrEmpty(_tagNames))
		{
			return;
		}
		var addTags = await Sender.Send(new AddTagsToBook.Command(_tagNames, _book.BookId));
		if (addTags.IsSuccess)
		{
			_book.Tags.AddRange(addTags.Value);
			_tagNames = string.Empty;
			_addingTags = false;
		}
	}
		
	private async Task RemoveTagFromBook(MudChip<string> mudChip)
	{
		var tagToRemove = _book.Tags.FirstOrDefault(t => t.TagId == ((Tag)mudChip.Tag!).TagId);
		if (tagToRemove != null)
		{
			var removeTag = await Sender.Send(new RemoveTagFromBook.Command(tagToRemove.TagId, _book.BookId));
			if (removeTag.IsSuccess)
			{
				_book.Tags.Remove(tagToRemove);
			}
			else
			{
				throw new Exception(removeTag.Error.Description);
			}
		}
	}
}