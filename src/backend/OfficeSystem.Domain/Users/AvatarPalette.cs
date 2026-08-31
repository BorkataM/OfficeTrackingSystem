namespace OfficeSystem.Domain.Users;

/// <summary>
/// A fixed set of avatar accent colours. Picking from a curated palette keeps the
/// UI coherent, and deriving the index from the user id keeps it stable forever.
/// </summary>
public static class AvatarPalette
{
    private static readonly string[] Colors =
    [
        "#6366f1", // indigo
        "#0ea5e9", // sky
        "#14b8a6", // teal
        "#22c55e", // green
        "#f59e0b", // amber
        "#f97316", // orange
        "#ef4444", // red
        "#ec4899", // pink
        "#a855f7", // purple
        "#8b5cf6"  // violet
    ];

    public static string ColorFor(Guid id)
        => Colors[(int)((uint)id.GetHashCode() % (uint)Colors.Length)];
}
