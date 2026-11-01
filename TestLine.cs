using System.Buffers.Binary;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;

namespace MediantTrace;

/// <summary>
/// The test line: someone calls MediantTrace (for example from a dealerboard
/// line), hears a short announcement and a beep, speaks, and hears their own
/// voice played back. Meanwhile the audio coming in is measured and recorded,
/// so both directions are checked by ear and by numbers.
/// </summary>
public static class TestLine
{
    private const int Rate = 8000, Frame = 160; // 20 ms of 8 kHz audio
    private const double RecordSeconds = 5;

    private static readonly object SpeechGate = new();
    private static readonly Dictionary<string, short[]> SpeechCache = new();

    /// <summary>Text to 8 kHz mono PCM with the Windows speech engine; a short tone if speech is unavailable.</summary>
    public static short[] Speak(string text)
    {
        lock (SpeechGate)
        {
            if (SpeechCache.TryGetValue(text, out short[]? cached)) return cached;
            short[] pcm;
            try
            {
                using var synth = new System.Speech.Synthesis.SpeechSynthesizer();
                // Culture can be missing (this build runs with invariant globalisation): fall back to the voice name.
                static string Lang(System.Speech.Synthesis.VoiceInfo v) => v.Culture?.Name ?? (v.Name.Contains("Zira") || v.Name.Contains("David") ? "en-US" : v.Name.Contains("Hazel") || v.Name.Contains("George") ? "en-GB" : "");
                var voice = synth.GetInstalledVoices().Where(v => v.Enabled).Select(v => v.VoiceInfo)
                    .OrderByDescending(v => Lang(v) == "en-GB").ThenByDescending(v => Lang(v).StartsWith("en", StringComparison.Ordinal))
                    .ThenBy(v => v.Gender == System.Speech.Synthesis.VoiceGender.Female ? 0 : 1)
                    .FirstOrDefault();
                if (voice is not null) synth.SelectVoice(voice.Name);
                synth.Rate = -1;
                using var ms = new MemoryStream();
                synth.SetOutputToAudioStream(ms, new System.Speech.AudioFormat.SpeechAudioFormatInfo(Rate, System.Speech.AudioFormat.AudioBitsPerSample.Sixteen, System.Speech.AudioFormat.AudioChannel.Mono));
                synth.Speak(text);
                synth.SetOutputToNull();
                byte[] raw = ms.ToArray();
                pcm = new short[raw.Length / 2];
                Buffer.BlockCopy(raw, 0, pcm, 0, pcm.Length * 2);
            }
            catch (Exception e)
            {
                Log.Write($"Test line: speech unavailable ({e}); using tones");
                pcm = Tone(600, 0.4, 4000);
            }
            SpeechCache[text] = pcm;
            return pcm;
        }
    }

    public static short[] Tone(double hz, double seconds, double amplitude = 7300)
    {
        var pcm = new short[(int)(Rate * seconds)];
        for (int i = 0; i < pcm.Length; i++)
        {
            double fade = Math.Min(1, Math.Min(i, pcm.Length - i) / 80.0); // 10 ms ramps: no clicks
            pcm[i] = (short)(Math.Sin(2 * Math.PI * hz * i / Rate) * amplitude * fade);
        }
        return pcm;
    }

    public static short[] Silence(double seconds) => new short[(int)(Rate * seconds)];

    /// <summary>
    /// Spoken line-test message for the trade-floor voice team. Always longer than five seconds,
    /// repeating the sentence if the speech engine returns less.
    /// </summary>
    public static short[] FloorAnnouncement()
    {
        const string key = "floor-announcement";
        lock (SpeechGate)
        {
            if (SpeechCache.TryGetValue(key, out short[]? cached)) return cached;
        }
        const string text = "This is a TradeCom line test for the voice team. You are hearing a spoken test message on this wire. It is a test only, and no reply is needed. This message lasts more than five seconds so the voice team can confirm that speech is present on the line.";
        short[] once = Speak(text);
        if (once.Length <= Rate * 5)
        {
            var padded = new List<short>(once);
            short[] gap = Silence(0.4);
            while (padded.Count <= Rate * 5)
            {
                padded.AddRange(gap);
                padded.AddRange(once);
            }
            once = padded.ToArray();
        }
        lock (SpeechGate) SpeechCache[key] = once;
        return once;
    }

    public sealed class Session
    {
        public required Socket Rtp;
        public required IPEndPoint Remote;
        public required bool Alaw;
        public required TestResult Result;
        public required CancellationToken HungUp;
        public short[] Recording = [];
    }

    /// <summary>Runs the announcement / record / play-back script. Returns when done or when the caller hangs up.</summary>
    public static void Run(Session s)
    {
        var rx = new Inbound(s);
        var receiver = new Thread(() => rx.Loop()) { IsBackground = true, Priority = ThreadPriority.Highest, Name = "testline-rx" };
        receiver.Start();
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var tx = new Sender(s, clock);
        TimeBeginPeriod(1);
        try
        {
            s.Result.Stage = "announcement";
            tx.Play(Silence(0.3));
            tx.Play(Speak("This is the Mediant Trace test line. After the tone, please say something. It will be played back to you."));
            tx.Play(Silence(0.2));
            tx.Play(Tone(1004, 0.5));
            if (s.HungUp.IsCancellationRequested) return;

            s.Result.Stage = "recording";
            rx.StartRecording();
            tx.Play(Silence(RecordSeconds));
            s.Recording = rx.StopRecording();
            if (s.HungUp.IsCancellationRequested) return;

            s.Result.Stage = "playing back";
            tx.Play(Speak("Playing back."));
            tx.Play(Silence(0.2));
            tx.Play(s.Recording.Length > 0 ? s.Recording : Speak("Nothing was heard."));
            tx.Play(Silence(0.3));
            tx.Play(Speak("Test complete. Goodbye."));
            tx.Play(Silence(0.3));
        }
        finally
        {
            TimeEndPeriod(1);
            if (s.Recording.Length == 0) s.Recording = rx.StopRecording();
            rx.Stop();
            receiver.Join(500);
            rx.Apply();
            s.Result.TxPackets = tx.Packets;
            s.Result.TxPerSecond = tx.PerSecond;
        }
    }

    /// <summary>Paced G.711 sender: one packet every 20 ms from a fixed start, so timing never drifts.</summary>
    private sealed class Sender(Session s, System.Diagnostics.Stopwatch clock)
    {
        private ushort _seq = (ushort)Random.Shared.Next(1, 30000);
        private uint _ts = (uint)Random.Shared.Next();
        private readonly uint _ssrc = (uint)Random.Shared.Next();
        private long _frame;
        private readonly byte[] _packet = new byte[12 + Frame];
        public long Packets;
        public List<int> PerSecond = [];

        public void Play(short[] pcm)
        {
            for (int off = 0; off < pcm.Length && !s.HungUp.IsCancellationRequested; off += Frame)
            {
                double due = _frame * 20.0 - clock.Elapsed.TotalMilliseconds;
                if (due > 2) Thread.Sleep(TimeSpan.FromMilliseconds(due - 1.5));
                while (_frame * 20.0 - clock.Elapsed.TotalMilliseconds > 0.05) Thread.SpinWait(50);
                _packet[0] = 0x80;
                _packet[1] = (byte)((s.Alaw ? 8 : 0) | (_frame == 0 ? 0x80 : 0));
                BinaryPrimitives.WriteUInt16BigEndian(_packet.AsSpan(2), _seq++);
                BinaryPrimitives.WriteUInt32BigEndian(_packet.AsSpan(4), _ts);
                BinaryPrimitives.WriteUInt32BigEndian(_packet.AsSpan(8), _ssrc);
                _ts += Frame;
                for (int i = 0; i < Frame; i++)
                {
                    short v = off + i < pcm.Length ? pcm[off + i] : (short)0;
                    _packet[12 + i] = s.Alaw ? G711.LinearToAlaw(v) : G711.LinearToUlaw(v);
                }
                try { s.Rtp.SendTo(_packet, s.Remote); } catch (SocketException) { } catch (ObjectDisposedException) { return; }
                Packets++;
                int sec = (int)(_frame / 50);
                while (PerSecond.Count <= sec) PerSecond.Add(0);
                PerSecond[sec]++;
                _frame++;
            }
        }
    }

    /// <summary>Measures and records the caller's audio.</summary>
    private sealed class Inbound(Session s)
    {
        private volatile bool _stop, _recording;
        private readonly List<short> _rec = [];
        private long _packets, _frames, _audioFrames;
        private double _dbSum, _jitter, _lastTransit, _maxGap, _lastArrival = -1;
        private bool _haveTransit, _seqInit;
        private ushort _maxSeq;
        private uint _cycles, _baseSeq;
        private double? _firstAudio;
        private string _from = "";
        private readonly List<int> _perSecond = [];
        private readonly System.Diagnostics.Stopwatch _clock = System.Diagnostics.Stopwatch.StartNew();

        public void StartRecording() { lock (_rec) { _rec.Clear(); _recording = true; } }
        public short[] StopRecording() { lock (_rec) { _recording = false; return [.. _rec]; } }
        public void Stop() => _stop = true;

        public void Loop()
        {
            byte[] buf = new byte[2048];
            EndPoint any = new IPEndPoint(IPAddress.Any, 0);
            s.Rtp.ReceiveTimeout = 200;
            while (!_stop)
            {
                int n;
                EndPoint from = any;
                try { n = s.Rtp.ReceiveFrom(buf, ref from); }
                catch (SocketException) { continue; }
                catch (ObjectDisposedException) { return; }
                if (n < 12 || buf[0] >> 6 != 2 || buf[1] is >= 200 and <= 204) continue;
                int pt = buf[1] & 0x7F;
                double now = _clock.Elapsed.TotalMilliseconds;
                _packets++;
                if (_from.Length == 0) _from = from.ToString()!;
                ushort seq = BinaryPrimitives.ReadUInt16BigEndian(buf.AsSpan(2));
                uint ts = BinaryPrimitives.ReadUInt32BigEndian(buf.AsSpan(4));
                if (!_seqInit) { _seqInit = true; _baseSeq = seq; _maxSeq = seq; }
                else { ushort d = (ushort)(seq - _maxSeq); if (d < 3000) { if (seq < _maxSeq) _cycles += 65536; _maxSeq = seq; } }
                if (_lastArrival >= 0) _maxGap = Math.Max(_maxGap, now - _lastArrival);
                _lastArrival = now;
                int sec = (int)(now / 1000);
                while (_perSecond.Count <= sec) _perSecond.Add(0);
                _perSecond[sec]++;
                if (pt is not (0 or 8)) continue;
                double transit = now * 8.0 - ts;
                if (_haveTransit) { double dd = Math.Abs(transit - _lastTransit); if (dd < 8000) _jitter += (dd - _jitter) / 16.0; }
                _lastTransit = transit; _haveTransit = true;
                double sum = 0;
                int samples = n - 12;
                lock (_rec)
                {
                    for (int i = 0; i < samples; i++)
                    {
                        short v = pt == 8 ? G711.AlawToLinear(buf[12 + i]) : G711.UlawToLinear(buf[12 + i]);
                        sum += (double)v * v;
                        if (_recording && _rec.Count < Rate * 30) _rec.Add(v);
                    }
                }
                _frames++;
                double db = 20 * Math.Log10(Math.Max(Math.Sqrt(sum / Math.Max(1, samples)), 1) / 32768.0);
                if (db > -50) { _audioFrames++; _dbSum += db; _firstAudio ??= Math.Round(now, 0); }
            }
        }

        public void Apply()
        {
            var r = s.Result;
            long expected = _seqInit ? (long)(_cycles + _maxSeq) - _baseSeq + 1 : 0;
            r.RxPackets = _packets;
            r.RxLost = Math.Max(0, expected - _packets);
            r.RxLossPercent = expected > 0 ? Math.Round(100.0 * r.RxLost / expected, 2) : 0;
            r.RxJitterMs = Math.Round(_jitter / 8.0, 1);
            r.RxMaxGapMs = Math.Round(_maxGap, 0);
            r.FirstAudioMs = _firstAudio;
            r.AudioFrom = _from;
            r.RxAudioPercent = _frames > 0 ? Math.Round(100.0 * _audioFrames / _frames, 1) : 0;
            r.RxLevelDb = _audioFrames > 0 ? Math.Round(_dbSum / _audioFrames, 1) : -99;
            r.RxPerSecond = _perSecond;
            r.MosRx = _packets >= 25 ? Quality.Mos(r.RxLossPercent, r.RxJitterMs) : null;
        }
    }

    /// <summary>16-bit mono 8 kHz WAV, for listening to what the caller said.</summary>
    public static byte[] Wav(short[] pcm)
    {
        using var ms = new MemoryStream();
        using var w = new BinaryWriter(ms);
        int bytes = pcm.Length * 2;
        w.Write("RIFF"u8); w.Write(36 + bytes); w.Write("WAVE"u8);
        w.Write("fmt "u8); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(Rate); w.Write(Rate * 2); w.Write((short)2); w.Write((short)16);
        w.Write("data"u8); w.Write(bytes);
        foreach (short v in pcm) w.Write(v);
        w.Flush();
        return ms.ToArray();
    }

    [DllImport("winmm.dll", EntryPoint = "timeBeginPeriod")] private static extern uint TimeBeginPeriod(uint ms);
    [DllImport("winmm.dll", EntryPoint = "timeEndPeriod")] private static extern uint TimeEndPeriod(uint ms);
}
