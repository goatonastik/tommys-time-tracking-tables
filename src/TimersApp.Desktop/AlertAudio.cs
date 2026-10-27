using System.IO;
using System.Text;
using System.Windows.Threading;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using TimersApp.Core;

namespace TimersApp.Desktop;
internal sealed class AlertAudio(string folder, Action<string> onError, Func<IWavePlayer>? outputFactory = null) : IDisposable
{
    private readonly Dictionary<Guid, Playback> players = [];
    private readonly Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
    private bool disposed;
    private sealed record Playback(IWavePlayer Output, LoopStream Source,Action<string> Report);
    internal static WaveStream Open(string path)
    {
        if (Path.GetExtension(path).Equals(".mp3", StringComparison.OrdinalIgnoreCase)) return new ManagedMp3(path);
        if (!Path.GetExtension(path).Equals(".wav", StringComparison.OrdinalIgnoreCase)) throw new InvalidDataException("Choose a WAV or MP3 sound.");
        var wave = new WaveFileReader(path);
        try { _ = wave.ToSampleProvider(); return wave; } catch { wave.Dispose(); throw new InvalidDataException("Choose an uncompressed PCM or IEEE-float WAV, or an MP3 file."); }
    }
    internal static void Validate(string path)
    {
        using var source = Open(path); var buffer = new byte[4096];
        if (source.Read(buffer, 0, buffer.Length) == 0) throw new InvalidDataException("The sound contains no playable audio.");
    }
    public void Play(TimerRecord timer,Action<string>? errorSink=null)
    {
        var report=errorSink??onError;
        Stop(timer.Id); if (disposed || !timer.Alert.PlaySound) return;
        LoopStream? source = null; IWavePlayer? output = null;
        try
        {
            string path = timer.Alert.SoundPath ?? SoundCatalog.Resolve(timer.Alert.SystemSound) ?? BuiltInSound();
            WaveStream decoded;
            try{decoded=Open(path);}catch(Exception ex)when(timer.Alert.SoundPath is not null||timer.Alert.SystemSound is not null){report("The selected sound is unavailable; using the built-in chime. "+ex.Message);decoded=Open(BuiltInSound());}
            source = new LoopStream(decoded, () => timer.AlertState.Active && timer.Alert.RepeatSound);
            var volume = new VolumeSampleProvider(source.ToSampleProvider()) { Volume = (float)Math.Clamp(timer.Alert.Volume, 0, 1) };
            output = outputFactory?.Invoke() ?? new WaveOutEvent();
            var playback = new Playback(output, source,report); players[timer.Id] = playback;
            output.PlaybackStopped += (_, e) => dispatcher.BeginInvoke(new Action(() =>
            {
                if (!players.TryGetValue(timer.Id, out var current) || !ReferenceEquals(current, playback)) return;
                Stop(timer.Id); if (e.Exception is not null) report("Could not play the alert sound: " + e.Exception.Message);
            }));
            output.Init(new SampleToWaveProvider16(volume)); output.Play();
        }
        catch (Exception ex)
        {
            if(players.ContainsKey(timer.Id))Stop(timer.Id);else{output?.Dispose();source?.Dispose();}
            report("Could not play the alert sound: " + ex.Message);
        }
    }
    public void Stop(Guid id)
    {
        if (!players.Remove(id, out var player)) return;
        player.Source.Halt();
        try { player.Output.Stop(); } catch(Exception ex) { player.Report("Could not stop the sound device: "+ex.Message); }
        try { player.Output.Dispose(); } catch(Exception ex) { player.Report("Could not close the sound device: "+ex.Message); }
        finally { player.Source.Dispose(); }
    }
    public void StopAll() { foreach (var id in players.Keys.ToArray()) Stop(id); }
    public void Dispose() { disposed = true; StopAll(); }
    internal string BuiltInSound()
    {
        Directory.CreateDirectory(folder); var path = Path.Combine(folder, "default-alert.wav"); if (File.Exists(path)) return path;
        const int rate = 22050, samples = rate / 2;
        using var stream = File.Create(path); using var writer = new BinaryWriter(stream, Encoding.ASCII);
        writer.Write(Encoding.ASCII.GetBytes("RIFF")); writer.Write(36 + samples * 2); writer.Write(Encoding.ASCII.GetBytes("WAVEfmt ")); writer.Write(16); writer.Write((short)1); writer.Write((short)1); writer.Write(rate); writer.Write(rate * 2); writer.Write((short)2); writer.Write((short)16); writer.Write(Encoding.ASCII.GetBytes("data")); writer.Write(samples * 2);
        for (int i = 0; i < samples; i++) { double envelope = Math.Min(1, i / 500d) * Math.Min(1, (samples - i) / 2000d); writer.Write((short)(Math.Sin(2 * Math.PI * 880 * i / rate) * 10000 * envelope)); }
        return path;
    }
    private sealed class ManagedMp3 : WaveStream
    {
        private readonly NLayer.MpegFile decoder;
        public ManagedMp3(string path) { decoder = new NLayer.MpegFile(path); WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(decoder.SampleRate, decoder.Channels); }
        public override WaveFormat WaveFormat { get; }
        public override long Length => decoder.Length;
        public override long Position { get => decoder.Position; set => decoder.Position = value; }
        public override int Read(byte[] buffer, int offset, int count) => decoder.ReadSamples(buffer, offset, count);
        protected override void Dispose(bool disposing) { if (disposing) decoder.Dispose(); base.Dispose(disposing); }
    }
    private sealed class LoopStream(WaveStream source, Func<bool> repeat) : WaveStream
    {
        private readonly object gate=new();
        private bool halted;
        internal void Halt(){lock(gate)halted=true;}
        public override WaveFormat WaveFormat => source.WaveFormat;
        public override long Length => source.Length;
        public override long Position { get => source.Position; set => source.Position = value; }
        public override int Read(byte[] buffer, int offset, int count)
        {
            lock(gate)
            {
            if(halted)return 0;
            int total=0;
            while(total<count)
            {
                int n=source.Read(buffer,offset+total,count-total);total+=n;
                if(n>0)continue;
                if(!repeat()||source.Position==0)break;
                source.Position=0;
            }
            return total;
            }
        }
        protected override void Dispose(bool disposing){lock(gate){halted=true;if(disposing)source.Dispose();}base.Dispose(disposing);}
    }
}
