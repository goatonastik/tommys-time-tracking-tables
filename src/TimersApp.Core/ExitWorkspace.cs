namespace TimersApp.Core;

public static class ExitWorkspace
{
    public static Workspace Prepare(Workspace current, Workspace sessionStart, ExitChoice choice, bool remember)
    {
        if (choice == ExitChoice.Ask) throw new ArgumentException("Choose save or discard before committing an exit.");
        var result = Appearance.Copy(choice == ExitChoice.Save ? current : sessionStart);
        if (choice == ExitChoice.Discard) new TimerEngine(result).Recover(false, false);
        if (remember) result.Settings.ExitChoice = choice;
        result.CleanShutdown = true;
        return result;
    }
}
