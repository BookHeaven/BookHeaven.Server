namespace BookHeaven.Server.Helpers;

public static class Transitions
{
    public static string GetTransitionName(string element, Guid id)
    {
        return $"view-transition-name:{element}-{id};";
    }
}