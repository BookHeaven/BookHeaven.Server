namespace BookHeaven.Server.Entities
{
	public class BookMetadata
	{
		public string Title { get; set; } = string.Empty;
		public string? Series { get; set; }
		public string? Author { get; set; }
		public string CoverURL { get; set; } = string.Empty;
		public string? Publisher { get; set; }
		public string? ISBN10 { get; set; }
		public string? ISBN13 { get; set; }
		public string? ASIN { get; set; }
		public string? Description { get; set; }
		
	}
}
