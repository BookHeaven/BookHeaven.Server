using EpubManager;
using EpubManager.Entities;
using Microsoft.AspNetCore.Components.Forms;
using BookHeaven.Server.Interfaces;
using System.Globalization;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Services;

namespace BookHeaven.Server.Services
{
	public class EpubService(IEpubReader epubReader, IDatabaseService databaseService, ILogger<EpubService> logger) : IFormatService<EpubBook>
	{
		public async Task<EpubBook> GetMetadata(string path)
		{
			return await epubReader.ReadAsync(path, true);
		}

		public async Task<Guid?> LoadFromFile(IBrowserFile file)
		{
			Guid? id;
			try
			{
				string tempPath = Path.GetTempFileName();
				await using (FileStream fileStream = File.Create(tempPath))
				{
					await file.OpenReadStream(maxAllowedSize: 1024 * 30000).CopyToAsync(fileStream);
				}

				id = await LoadFromFilePath(tempPath);
				File.Delete(tempPath);
			}
			catch (Exception e)
            {
				logger.LogError(e, "Failed to load book from file");
				id = null;
            }
			
			return id;
		}

		public async Task<Guid?> LoadFromFilePath(string path)
		{
			EpubBook epubBook = await GetMetadata(path);
			Book? book = await databaseService.GetBy<Book>(x => x.Title!.Equals(epubBook.Metadata.Title));
			if (book == null)
			{
				
				Author? author = await databaseService.GetBy<Author>(x => x.Name!.ToUpper().Equals(epubBook.Metadata.Author.ToUpper()));

				if (author == null)
				{
					author = new Author
					{
						Name = epubBook.Metadata.Author,
					};

					await databaseService.AddOrUpdate(author);
				}
				
				book = new Book
				{
					Title = epubBook.Metadata.Title,
					AuthorId = author.AuthorId,
					Description = epubBook.Metadata.Description,
					PublishedDate = DateTime.TryParseExact(epubBook.Metadata.PublishDate, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var tempDate) ? tempDate : null,
					Publisher = epubBook.Metadata.Publisher,
					Language = epubBook.Metadata.Language
				};

				if (epubBook.Metadata.Series != null)
				{
					Series? series = await databaseService.GetBy<Series>(x => x.Name!.ToUpper().Equals(epubBook.Metadata.Series.ToUpper()));
					if (series == null)
					{
						series = new Series
						{
							Name = epubBook.Metadata.Series
						};
						await databaseService.AddOrUpdate(series);
					}
					
					book.SeriesId = series.SeriesId;
					book.SeriesIndex = epubBook.Metadata.SeriesIndex;
				}
				
				foreach (var identifier in epubBook.Metadata.Identifiers)
				{
					var value = identifier.Value.Split(":").Last();
					switch (identifier.Scheme)
					{
						case "ASIN":
							book.ASIN = value;
							break;
						case "UUID":
							book.UUID = value;
							break;
						case "ISBN":
							if (value.Length == 13)
							{
								book.ISBN13 = value;
							}
							else
							{
								book.ISBN10 = value;
							}
							break;
					}
				}
				await databaseService.AddOrUpdate(book);
				
				var profiles = await databaseService.GetAll<Profile>();
				
				foreach (var profile in profiles)
				{
					await databaseService.AddOrUpdate(new BookProgress
					{
						BookId = book.BookId,
						ProfileId = profile.ProfileId
					});
				}
				
				await databaseService.SaveChanges();

				await StoreCover(epubBook.Cover, book.GetCoverPath(Program.CoversPath)!);
				await StoreBook(epubBook.FilePath, book.GetBookPath(Program.BooksPath)!);
			}

			return book?.BookId;
		}

		public async Task LoadFromFolder(string path)
		{
			foreach (string file in Directory.EnumerateFiles(path, "*.epub", SearchOption.AllDirectories))
			{
				await LoadFromFilePath(file);
			}
		}

		public async Task StoreCover(byte[]? image, string dest)
		{
			if (image == null) return;
			await File.WriteAllBytesAsync(dest, image);
		}

		public async Task StoreBook(string? sourcePath, string dest)
		{
			if (sourcePath == null) return;
            await using FileStream source = File.OpenRead(sourcePath);
            await using FileStream destination = File.Create(dest);
            await source.CopyToAsync(destination);
        }
	}
}
