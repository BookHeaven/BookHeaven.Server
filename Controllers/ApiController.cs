using BookHeaven.Domain;
using Microsoft.AspNetCore.Mvc;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Services;
using BookHeaven.Server.Features.Authors;
using BookHeaven.Server.Features.Books;
using BookHeaven.Server.Features.BooksProgress;
using BookHeaven.Server.Features.Profiles;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Controllers
{
	[ApiController]
	[Route("api")]
	public class ApiController(ISender sender, ILogger<ApiController> logger) : Controller
	{

		[HttpGet("ping")]
		public ActionResult Ping() => Ok();

		[HttpGet("books")]
		public async Task<ActionResult> GetAll()
		{
			var getBooks = await sender.Send(new GetAllBooksQuery());
			if (getBooks.IsSuccess)
			{
				return new JsonResult(getBooks.Value);
			}
			return BadRequest(getBooks.Error.Description);
		}

		[HttpGet("authors")]
		public async Task<ActionResult> GetAllAuthors()
		{
			var getAuthors = await sender.Send(new GetAllAuthorsQuery());
			if (getAuthors.IsSuccess)
			{
				return new JsonResult(getAuthors.Value);
			}
			return BadRequest(getAuthors.Error.Description);
		}

		[HttpGet("profiles")]
		public async Task<ActionResult> GetAllProfiles()
		{
			var getProfiles = await sender.Send(new GetAllProfilesQuery());
			if (getProfiles.IsSuccess)
			{
				return new JsonResult(getProfiles.Value);
			}
			return BadRequest(getProfiles.Error.Description);
		}

		[HttpGet("profiles/{profileId}/{bookId}")]
		public async Task<ActionResult> GetBookProgress(Guid bookId, Guid profileId)
		{
			var getProgress = await sender.Send(new GetBookProgressByProfileQuery(bookId, profileId));
			if (getProgress.IsSuccess)
			{
				return new JsonResult(getProgress.Value);
			}
			return BadRequest(getProgress.Error.Description);
		}

		[HttpPost("progress/update")]
		public async Task<ActionResult> UpdateBookProgress([FromBody] BookProgress progress)
		{
			logger.LogInformation($"Received progress update for book {progress.BookProgressId} (LastRead: {progress.LastRead})");
			var getExistingProgress = await sender.Send(new GetBookProgress(progress.BookProgressId));

			if (getExistingProgress.IsSuccess && progress.LastRead <= getExistingProgress.Value.LastRead)
			{
				logger.LogInformation("Progress is older than existing, skipping update");
				return Ok();
			}
				
			logger.LogInformation("Updating progress");
				
			var updateProgress = await sender.Send(new UpdateBookProgressCommand(progress));
			if (updateProgress.IsFailure)
			{
				logger.LogError(updateProgress.Error.Description);
				return BadRequest(updateProgress.Error.Description);
			}
			return Ok();
		}
	}
}
