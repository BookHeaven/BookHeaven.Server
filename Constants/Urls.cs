namespace BookHeaven.Server.Constants;

public static class Urls
{
	public const string Shelf = "/shelf";
	public const string Collections = "/collections";
	public const string Collection = $"{Shelf}/collection";
	public const string NewCollection = $"{Collections}/new";
	public const string Authors = "/authors";
	public const string CreateAuthor = $"{Authors}/create";
	public const string Series = "/series";
	public const string Settings = "/settings";
	public const string Profiles = "/profiles";
	public const string CreateProfile = $"{Profiles}/create";

	public static string GetBookUrl(Guid bookId)
	{
		return $"{Shelf}/{bookId}";
	}
	
	public static string GetAuthorUrl(Guid authorId)
	{
		return $"{Authors}/{authorId}";
	}
	
	public static string GetSeriesUrl(Guid seriesId)
	{
		return $"{Series}/{seriesId}";
	}
	
	public static string GetProfileUrl(Guid profileId)
	{
		return $"{Profiles}/{profileId}";
	}
	
	public static string GetCollectionUrl(Guid collectionId)
	{
		return $"{Collection}/{collectionId}";
	}
	
	public static string GetCollectionPageUrl(Guid collectionId)
	{
		return $"{Collections}/{collectionId}";
	}
}
