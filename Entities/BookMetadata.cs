namespace BookHeaven.Server.Entities;

public class BookMetadata
{
	public string Title { get; set; } = string.Empty;
	public string? Series { get; set; }
	public string? Author { get; set; }
	public string CoverUrl { get; set; } = string.Empty;
	public string? Publisher { get; set; }
	public DateTime? PublishedDate { get; set; }
	public string? Isbn10 { get; set; }
	public string? Isbn13 { get; set; }
	public string? Asin { get; set; }
	public string Description { get; set; } = string.Empty;
		
}