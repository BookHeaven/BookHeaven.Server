namespace BookHeaven.Server.Constants;

public static class Urls
{
	public const string Shelf = "/shelf";
	public const string Authors = "/authors";
	public const string Series = "/series";
	public const string Settings = "/settings";

	public static string GetBookUrl(Guid bookId)
	{
		return $"{Shelf}/{bookId}";
	}
}
