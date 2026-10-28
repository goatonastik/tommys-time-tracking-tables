using System.Text.Json;
using TimersApp.Core;

namespace TimersApp.Desktop;

public sealed class StartupSoundSettings
{
    public bool Enabled {get;set;}
    public string? Path {get;set;}
    public string? Name {get;set;}
    public double Volume {get;set;}=.7;
}

public sealed partial class WorkspaceStore
{
    public StartupSoundSettings LoadStartupSound()
    {
        using var db=Open();using var c=db.CreateCommand();c.CommandText="SELECT payload FROM app_options WHERE name='startup-sound'";
        var value=c.ExecuteScalar() is string text?JsonSerializer.Deserialize<StartupSoundSettings>(text)??new():new();
        value.Volume=double.IsFinite(value.Volume)?Math.Clamp(value.Volume,0,1):.7;return value;
    }
    public void SaveSettingsPreferences(Workspace workspace,StartupSoundSettings startup)
    {
        if(!double.IsFinite(startup.Volume)||startup.Volume<0||startup.Volume>1)throw new ArgumentException("Check startup volume.");
        using var db=Open();using var tx=db.BeginTransaction();
        WriteSnapshot(db,tx,"preferences",PreferencePayload(workspace));
        using var c=db.CreateCommand();c.Transaction=tx;c.CommandText="INSERT OR REPLACE INTO app_options(name,payload) VALUES('startup-sound',$payload)";
        c.Parameters.AddWithValue("$payload",JsonSerializer.Serialize(startup));c.ExecuteNonQuery();tx.Commit();
    }
}
