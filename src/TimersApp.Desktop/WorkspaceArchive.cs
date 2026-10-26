using System.IO;
using System.IO.Compression;
using System.Text.Json;
using TimersApp.Core;

namespace TimersApp.Desktop;
internal static class WorkspaceArchive
{
    private static IEnumerable<(Func<string?> Read, Action<string?> Write, bool Image)> Assets(Workspace value)
    {
        var backgrounds = new List<BackgroundStyle> { value.Settings.AppBackground, value.Settings.DefaultStyle.Background, value.Settings.PopoutStyle.Background };
        backgrounds.AddRange(value.Presets.Select(p => p.Style.Background));
        foreach (var timer in value.Timers)
        {
            if (timer.Style is not null) backgrounds.Add(timer.Style.Background);
            if (timer.PopoutStyle is not null) backgrounds.Add(timer.PopoutStyle.Background);
            yield return (() => timer.Alert.SoundPath, path => timer.Alert.SoundPath = path, false);
        }
        foreach (var background in backgrounds) yield return (() => background.ImagePath, path => background.ImagePath = path, true);
    }
    public static void Export(string path, Workspace workspace)
    {
        var copy = Appearance.Copy(workspace);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            using (var file = File.Create(temporary))
            using (var zip = new ZipArchive(file, ZipArchiveMode.Create))
            {
                var mappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var asset in Assets(copy))
                {
                    string? source = asset.Read(); if (string.IsNullOrWhiteSpace(source)) continue;
                    if (!File.Exists(source)) throw new FileNotFoundException("A background or sound is missing. Replace it before exporting a portable backup.", source);
                    if (!mappings.TryGetValue(source, out var name))
                    {
                        name = $"assets/{mappings.Count:0000}{Path.GetExtension(source)}"; mappings[source] = name;
                        using var input = File.OpenRead(source); using var output = zip.CreateEntry(name).Open(); input.CopyTo(output);
                    }
                    asset.Write(name);
                }
                using var text = new StreamWriter(zip.CreateEntry("workspace.json").Open()); text.Write(JsonSerializer.Serialize(copy));
            }
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }
    public static Workspace Import(string path, string folder)
    {
        using var file = File.OpenRead(path); using var zip = new ZipArchive(file, ZipArchiveMode.Read);
        var entry = zip.GetEntry("workspace.json") ?? throw new InvalidDataException("This file is not a T⁴ backup.");
        if (entry.Length > 100 * 1024 * 1024) throw new InvalidDataException("Backup metadata exceeds 100 MB.");
        using var reader = new StreamReader(entry.Open()); var workspace = WorkspaceStore.Decode(reader.ReadToEnd());
        var library = new AssetLibrary(folder); var mappings = new Dictionary<(string, bool), string>();
        foreach (var asset in Assets(workspace))
        {
            var name = asset.Read(); if (string.IsNullOrWhiteSpace(name)) continue;
            if (!mappings.TryGetValue((name, asset.Image), out var managed))
            {
                var item = zip.GetEntry(name) ?? throw new InvalidDataException("A backup asset is missing.");
                if (item.Length > 60 * 1024 * 1024) throw new InvalidDataException("A backup asset exceeds 60 MB.");
                Directory.CreateDirectory(folder);
                var temporary = Path.Combine(folder, "import-" + Guid.NewGuid().ToString("N") + Path.GetExtension(item.Name));
                try { using (var source = item.Open()) using (var target = File.Create(temporary)) source.CopyTo(target); managed = library.Import(temporary, asset.Image); }
                finally { if (File.Exists(temporary)) File.Delete(temporary); }
                mappings[(name, asset.Image)] = managed;
            }
            asset.Write(managed);
        }
        return workspace;
    }
}
