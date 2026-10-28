namespace TimersApp.Core;

/// <summary>Independent workspace policies; geometry is separate from editable settings drafts.</summary>
public static class PopoutPlacementPolicy
{
    public static PopoutGeometry Resolve(Workspace workspace, TimerRecord timer)
    {
        var s = workspace.Settings;
        var size = s.PopoutSizePerTimer ? timer.Popout : workspace.SharedPopoutGeometry;
        var location = s.PopoutLocationPerTimer ? timer.Popout : workspace.SharedPopoutGeometry;
        return new PopoutGeometry {
            Width = s.RememberPopoutSize ? size.Width : null, Height = s.RememberPopoutSize ? size.Height : null,
            Left = s.RememberPopoutLocation ? location.Left : null, Top = s.RememberPopoutLocation ? location.Top : null
        };
    }

    public static bool Capture(Workspace workspace, TimerRecord timer, PopoutGeometry bounds, bool sizeChanged, bool locationChanged, bool initialize = false)
    {
        bounds.Validate();
        var s = workspace.Settings;
        var size = s.PopoutSizePerTimer ? timer.Popout : workspace.SharedPopoutGeometry;
        var location = s.PopoutLocationPerTimer ? timer.Popout : workspace.SharedPopoutGeometry;
        bool changed = false;
        if (s.RememberPopoutSize && bounds.Width.HasValue && bounds.Height.HasValue &&
            (sizeChanged || initialize && !size.Width.HasValue) && (size.Width != bounds.Width || size.Height != bounds.Height))
        { size.Width = bounds.Width; size.Height = bounds.Height; changed = true; }
        if (s.RememberPopoutLocation && bounds.Left.HasValue && bounds.Top.HasValue &&
            (locationChanged || initialize && !location.Left.HasValue) && (location.Left != bounds.Left || location.Top != bounds.Top))
        { location.Left = bounds.Left; location.Top = bounds.Top; changed = true; }
        return changed;
    }
}
