namespace TimersApp.Core;

/// <summary>A durable primary write and its optional secondary backup have separate outcomes.</summary>
public sealed record PersistenceResult(bool Saved, string? SaveError, string? BackupWarning)
{
    public static PersistenceResult Run(Action save, Action? backup = null)
    {
        try { save(); }
        catch (Exception ex) { return new(false, ex.Message, null); }
        try { backup?.Invoke(); return new(true, null, null); }
        catch (Exception ex) { return new(true, null, ex.Message); }
    }
}
