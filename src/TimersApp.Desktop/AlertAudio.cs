using System.IO;
using System.Text;
using System.Windows.Media;
using TimersApp.Core;

namespace TimersApp.Desktop;
internal sealed class AlertAudio(string folder, Action<string> onError) : IDisposable
{
    private readonly Dictionary<Guid, MediaPlayer> players = [];
    public void Play(TimerRecord timer)
    {
        Stop(timer.Id); if (!timer.Alert.PlaySound) return;
        try
        {
            string path = timer.Alert.SoundPath ?? BuiltInSound();
            var player = new MediaPlayer { Volume = Math.Clamp(timer.Alert.Volume, 0, 1) }; players[timer.Id] = player;
            player.MediaFailed += (_, args) => { Stop(timer.Id); onError("Could not play the alert sound: " + args.ErrorException.Message); };
            player.MediaEnded += (_, _) => { if (timer.AlertState.Active && timer.Alert.RepeatSound) { player.Position = TimeSpan.Zero; player.Play(); } else Stop(timer.Id); };
            player.Open(new Uri(path)); player.Play();
        }
        catch (Exception ex) { Stop(timer.Id); onError(ex.Message); }
    }
    public void Stop(Guid id) { if (players.Remove(id, out var player)) { player.Stop(); player.Close(); } }
    public void Dispose() { foreach (var id in players.Keys.ToArray()) Stop(id); }
    private string BuiltInSound()
    {
        var path = Path.Combine(folder, "default-alert.wav"); if (File.Exists(path)) return path;
        const int rate = 22050, samples = rate / 2;
        using var stream = File.Create(path); using var writer = new BinaryWriter(stream, Encoding.ASCII);
        writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples * 2); writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16); writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(samples * 2);
        for (int i = 0; i < samples; i++) { double envelope = Math.Min(1, i / 500d) * Math.Min(1, (samples - i) / 2000d); writer.Write((short)(Math.Sin(2 * Math.PI * 880 * i / rate) * 10000 * envelope)); }
        return path;
    }
}
