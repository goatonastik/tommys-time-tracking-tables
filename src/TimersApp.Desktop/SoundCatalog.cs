using System.IO;
namespace TimersApp.Desktop;
internal sealed record SoundChoice(string Name, string? SystemFile = null, string? CustomPath=null, bool ImportOnApply=false)
{
    public override string ToString()=>Name;
    internal void Select(TimersApp.Core.AlertSettings draft){draft.SystemSound=SystemFile;draft.SoundPath=CustomPath;draft.SoundName=CustomPath is null?null:Name.Replace("Custom: ","");}
}
internal static class SoundCatalog
{
    private static string MediaFolder => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Media");
    internal static string? Resolve(string? filename)
    {
        if (string.IsNullOrWhiteSpace(filename) || filename != Path.GetFileName(filename)) return null;
        string path=Path.Combine(MediaFolder,filename);return File.Exists(path)?path:null;
    }
    internal static List<SoundChoice> Discover()
    {
        var sounds=new List<SoundChoice>{new("Built-in chime")};
        if(!Directory.Exists(MediaFolder))return sounds;
        foreach(string path in Directory.EnumerateFiles(MediaFolder,"*.wav").OrderBy(Path.GetFileName))
        {
            try{AlertAudio.Validate(path);sounds.Add(new("Windows: "+Path.GetFileNameWithoutExtension(path),Path.GetFileName(path)));}catch{ /* Only list locally playable sounds. */ }
        }
        return sounds;
    }
}
