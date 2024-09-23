using Microsoft.AspNetCore.Mvc;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Services;

namespace BookHeaven.Server.Controllers
{
	[ApiController]
	[Route("api")]
	public class ApiController(IDatabaseService DatabaseService, ILogger<ApiController> logger) : Controller
	{

		[HttpGet("ping")]
		public ActionResult Ping() => Ok();

		[HttpGet("books")]
		public async Task<ActionResult> GetAll() => new JsonResult(await DatabaseService.GetAllIncluding<Book>(b => b.Author, b => b.Series));

		[HttpGet("authors")]
		public async Task<ActionResult> GetAllAuthors() => new JsonResult(await DatabaseService.GetAll<Author>());

		[HttpGet("profiles")]
		public async Task<ActionResult> GetAllProfiles() => new JsonResult(await DatabaseService.GetAll<Profile>());

		[HttpGet("profiles/{profileId}/{bookId}")]
		public async Task<ActionResult> GetBookProgress(Guid bookId, Guid profileId) => new JsonResult(await DatabaseService.GetBy<BookProgress>(p => p.ProfileId == profileId && p.BookId == bookId));

		[HttpPost("progress/update")]
		public async Task<ActionResult> UpdateBookProgress([FromBody] BookProgress progress)
		{
			logger.LogInformation($"Received progress update for book {progress.BookProgressId} (LastRead: {progress.LastRead})");
			try
			{
				BookProgress? existing = await DatabaseService.Get<BookProgress>(progress.BookProgressId);
				if (existing != null && progress.LastRead <= existing.LastRead) return Ok();
				
				logger.LogInformation("Updating progress");
				await DatabaseService.AddOrUpdate(progress);
				await DatabaseService.SaveChanges();
				return Ok();
			}
			catch (Exception ex)
            {
				logger.LogError(ex, "Failed to update book progress");
                return BadRequest(ex.Message);
            }
		}
	}
}
