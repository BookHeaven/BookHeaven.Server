using BookHeaven.Domain;
using Microsoft.AspNetCore.Mvc;
using BookHeaven.Domain.Entities;
using BookHeaven.Domain.Services;
using Microsoft.EntityFrameworkCore;

namespace BookHeaven.Server.Controllers
{
	[ApiController]
	[Route("api")]
	public class ApiController(IDbContextFactory<DatabaseContext> dbContextFactory, ILogger<ApiController> logger) : Controller
	{

		[HttpGet("ping")]
		public ActionResult Ping() => Ok();

		[HttpGet("books")]
		public async Task<ActionResult> GetAll()
		{
			await using var context = await dbContextFactory.CreateDbContextAsync();
			
			return new JsonResult(await context.Books.Include(b => b.Author).Include(b => b.Series).ToListAsync());
		}

		[HttpGet("authors")]
		public async Task<ActionResult> GetAllAuthors()
		{
			await using var context = await dbContextFactory.CreateDbContextAsync();
			
			return new JsonResult(await context.Authors.ToListAsync());
		}

		[HttpGet("profiles")]
		public async Task<ActionResult> GetAllProfiles()
		{
			await using var context = await dbContextFactory.CreateDbContextAsync();
			
			return new JsonResult(await context.Profiles.ToListAsync());
		}

		[HttpGet("profiles/{profileId}/{bookId}")]
		public async Task<ActionResult> GetBookProgress(Guid bookId, Guid profileId)
		{
			await using var context = await dbContextFactory.CreateDbContextAsync();
			
			return new JsonResult(await context.BooksProgress.FirstOrDefaultAsync(x => x.BookId == bookId && x.ProfileId == profileId));
		}

		[HttpPost("progress/update")]
		public async Task<ActionResult> UpdateBookProgress([FromBody] BookProgress progress)
		{
			logger.LogInformation($"Received progress update for book {progress.BookProgressId} (LastRead: {progress.LastRead})");
			try
			{
				await using var context = await dbContextFactory.CreateDbContextAsync();
				
				var existingProgress = await context.BooksProgress.FirstOrDefaultAsync(x => x.BookProgressId == progress.BookProgressId);
				if (existingProgress != null && progress.LastRead <= existingProgress.LastRead) return Ok();
				
				logger.LogInformation("Updating progress");
				context.BooksProgress.Update(progress);
				await context.SaveChangesAsync();
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
