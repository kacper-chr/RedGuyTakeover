using System;
using System.Collections.Generic;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace gamething
{
    // Procedural SFX player using NAudio. Effects are synthesized once at startup
    // into in-memory PCM buffers, then mixed through a single WaveOut so they can
    // overlap freely without instance limits.
    internal static class SfxPlayer
    {
        // 44.1kHz matches the rate Windows mixes at in shared mode on most
        // devices, so no resampling is done between our buffers and the output
        // stage. 22.05k was forcing the driver to upsample — cheap resamplers
        // can add periodic interpolation artefacts that sound like regular
        // soft clicks overlaid on sustained tones.
        private const int SampleRate = 44100;

        public static bool Enabled = true;
        public static float MasterVolume = 1.0f;   // global multiplier (UI-controlled)
        public static float SfxVolume = 0.45f;     // SFX bus level before master
        public static float MusicBaseVolume = 0.18f; // music bus level before master

        private static readonly Dictionary<string, float[]> cache = new();
        private static MixingSampleProvider? mixer;
        private static IWavePlayer? output;
        private static readonly object gate = new();

        public static void Init()
        {
            try
            {
                var fmt = WaveFormat.CreateIeeeFloatWaveFormat(SampleRate, 1);
                mixer = new MixingSampleProvider(fmt) { ReadFully = true };
                // More headroom: 3 buffers × 100ms avoids underruns when the
                // UI thread spikes (e.g. frame hitches) which would otherwise
                // manifest as a click at the buffer boundary.
                output = new WaveOutEvent { DesiredLatency = 100, NumberOfBuffers = 3 };
                output.Init(mixer);
                output.Play();
            }
            catch
            {
                output = null;
                mixer = null;
                return;
            }

            Register("shoot",   Synthesize(0.06f, (t, d) => Env(t, d, 0.002f, 0.055f) * Saw(t, Lerp(540f, 180f, t / d)) * 0.35f));
            Register("hit",     Synthesize(0.05f, (t, d) => Env(t, d, 0.001f, 0.045f) * Noise() * 0.5f));
            Register("die",     Synthesize(0.18f, (t, d) => Env(t, d, 0.003f, 0.16f) * (Square(t, Lerp(260f, 70f, t / d)) * 0.45f + Noise() * 0.2f)));
            Register("hurt",    Synthesize(0.22f, (t, d) => Env(t, d, 0.005f, 0.2f) * (Sine(t, Lerp(180f, 90f, t / d)) * 0.5f + Noise() * 0.15f)));
            Register("dash",    Synthesize(0.12f, (t, d) => Env(t, d, 0.002f, 0.11f) * Sine(t, Lerp(380f, 820f, t / d)) * 0.4f));
            Register("reload",  Synthesize(0.08f, (t, d) => Env(t, d, 0.001f, 0.07f) * Square(t, 140f) * 0.3f));
            Register("upgrade", Synthesize(0.28f, (t, d) =>
            {
                float a = Env(t, d, 0.005f, 0.26f);
                float f = t < 0.09f ? 600f : t < 0.18f ? 750f : 900f;
                return a * Sine(t, f) * 0.45f;
            }));
            Register("combo",   Synthesize(0.12f, (t, d) => Env(t, d, 0.002f, 0.11f) * Sine(t, Lerp(880f, 1320f, t / d)) * 0.4f));
            Register("boss",    Synthesize(0.45f, (t, d) => Env(t, d, 0.01f, 0.4f) * (Square(t, Lerp(70f, 40f, t / d)) * 0.5f + Noise() * 0.25f)));
            Register("pickup",  Synthesize(0.1f, (t, d) => Env(t, d, 0.002f, 0.09f) * Sine(t, Lerp(660f, 990f, t / d)) * 0.4f));
            Register("dash_ready", Synthesize(0.1f, (t, d) => Env(t, d, 0.002f, 0.09f) * Sine(t, Lerp(440f, 660f, t / d)) * 0.3f));
        }

        public static void Play(string name, float volumeScale = 1f)
        {
            if (!Enabled || mixer == null) return;
            if (!cache.TryGetValue(name, out var buf)) return;
            try
            {
                var src = new CachedSoundSampleProvider(buf, SampleRate, volumeScale * SfxVolume * MasterVolume);
                lock (gate) { mixer.AddMixerInput((ISampleProvider)src); }
            }
            catch { /* ignore audio errors */ }
        }

        public static void Shutdown()
        {
            StopMusic();
            try { output?.Stop(); output?.Dispose(); } catch { }
            output = null;
            mixer = null;
        }

        // ---- Background music ----
        // A looping procedural track is built once per style and added to the
        // same mixer as SFX through a dedicated VolumeSampleProvider so its
        // level can be tuned independently of effects.
        public static float MusicVolume => MusicBaseVolume * MasterVolume;
        private static readonly Dictionary<string, float[]> musicCache = new();
        private static LoopingSampleProvider? currentMusic;
        private static VolumeSampleProvider? currentMusicVolume;
        private static string? currentMusicName;

        public static void PlayMusic(string style)
        {
            if (!Enabled || mixer == null) return;
            if (currentMusicName == style && currentMusic != null) return; // already playing
            StopMusic();
            if (!musicCache.TryGetValue(style, out var buf))
            {
                buf = BuildMusic(style);
                if (buf == null) return;
                musicCache[style] = buf;
            }
            try
            {
                var loop = new LoopingSampleProvider(buf, SampleRate);
                var vol = new VolumeSampleProvider(loop) { Volume = MusicVolume };
                lock (gate) { mixer.AddMixerInput((ISampleProvider)vol); }
                currentMusic = loop;
                currentMusicVolume = vol;
                currentMusicName = style;
            }
            catch { }
        }

        public static void StopMusic()
        {
            try
            {
                if (currentMusic != null && mixer != null)
                {
                    currentMusic.Stop();
                }
            }
            catch { }
            currentMusic = null;
            currentMusicVolume = null;
            currentMusicName = null;
        }

        public static void SetMusicVolume(float v)
        {
            MusicBaseVolume = Math.Clamp(v, 0f, 1f);
            if (currentMusicVolume != null) currentMusicVolume.Volume = MusicVolume;
        }

        public static void SetSfxVolume(float v)
        {
            SfxVolume = Math.Clamp(v, 0f, 1f);
        }

        public static void SetMasterVolume(float v)
        {
            MasterVolume = Math.Clamp(v, 0f, 1f);
            if (currentMusicVolume != null) currentMusicVolume.Volume = MusicVolume;
        }

        // Build a ~16s loop of chiptune-ish procedural music for the given style.
        // "menu"   — slow, moody minor arpeggio with soft pad.
        // "game"   — faster driving riff over bass pulse.
        // "boss"   — tense low-register ostinato.
        private static float[]? BuildMusic(string style)
        {
            try
            {
                float bpm, bars, root;
                int[] chordNotes;        // semitone offsets for arpeggio (relative to current chord root)
                int[] bassPattern;       // semitone offsets, one per beat (relative to current chord root)
                bool driving;
                // Chord progression: semitone offset from the track root, one per bar.
                // Null = stay on the track root every bar (old behavior).
                int[]? progression = null;
                switch (style)
                {
                    case "game":
                        bpm = 124f; bars = 8f; root = 196f; // G3
                        chordNotes = new[] { 0, 7, 12, 15, 12, 7, 3, 10 };
                        bassPattern = new[] { 0, 0, -5, 0, -2, -2, -7, -5 };
                        driving = true; break;
                    case "boss":
                        bpm = 140f; bars = 8f; root = 131f; // C3
                        chordNotes = new[] { 0, 3, 7, 10, 12, 10, 7, 3 };
                        bassPattern = new[] { 0, 0, 0, -2, -3, -3, -5, -7 };
                        driving = true; break;
                    case "menu":
                    default:
                        bpm = 78f; bars = 8f; root = 174f; // F3
                        // Arpeggio shape in chord-relative intervals: root, 5th, oct, m3, 5th, m3, oct, 5th
                        // (the "15" is the minor 3rd above the octave). Chord quality is inherited from
                        // the progression below — using 3 and 15 makes each chord minor-flavoured when
                        // the progression stays in a minor mode.
                        chordNotes = new[] { 0, 7, 12, 15, 12, 7, 3, 7 };
                        // Bass moves between root and 5th of the current chord.
                        bassPattern = new[] { 0, 0, -5, 0, 0, 0, -5, -5 };
                        // Progression relative to root F minor: i – VI – III – VII (Fm – Db – Ab – Eb).
                        // One bar per chord, played twice across the 8-bar loop for pleasing variation.
                        progression = new[] { 0, -4, 3, -2, 0, -4, 3, -2 };
                        driving = false; break;
                }

                float beatSec = 60f / bpm;
                int beatsPerBar = 4;
                int totalBeats = (int)(bars * beatsPerBar);
                float durSec = totalBeats * beatSec;
                int n = (int)(durSec * SampleRate);
                var buf = new float[n];

                // Helper: semitone offset of the current chord root for beat `b`.
                int ChordRootFor(int beat)
                {
                    if (progression == null || progression.Length == 0) return 0;
                    int bar = beat / beatsPerBar;
                    return progression[bar % progression.Length];
                }

                // Lead arpeggio — one chord note per eighth note, transposed by
                // the current chord so the melody outlines each progression step.
                int stepsPerBeat = 2;
                float stepSec = beatSec / stepsPerBeat;
                int totalSteps = totalBeats * stepsPerBeat;
                for (int s = 0; s < totalSteps; s++)
                {
                    float stepStart = s * stepSec;
                    int beatIdx = s / stepsPerBeat;
                    int chordShift = ChordRootFor(beatIdx);
                    int idx = chordNotes[s % chordNotes.Length] + chordShift;
                    double freq = root * Math.Pow(2.0, idx / 12.0);
                    float noteDur = stepSec * 0.95f;
                    int i0 = (int)(stepStart * SampleRate);
                    int i1 = Math.Min(n, i0 + (int)(noteDur * SampleRate));
                    // Per-note phase accumulator (double precision) + cosine
                    // attack/release — same treatment as the bass so sustained
                    // notes don't pick up float-precision buzz and the edges
                    // can't click.
                    int arpFadeIn = Math.Max(1, (int)(0.010f * SampleRate));
                    int arpFadeOut = Math.Max(1, (int)(0.020f * SampleRate));
                    double phase = 0.0;
                    double dPhase = 2.0 * Math.PI * freq / SampleRate;
                    double dPhase2 = 2.0 * dPhase;
                    double phase2 = 0.0;
                    int span = i1 - i0;
                    for (int i = i0; i < i1; i++)
                    {
                        int pos = i - i0;
                        int remaining = span - pos - 1;
                        float atk = pos < arpFadeIn ? (pos / (float)arpFadeIn) : 1f;
                        float tail = remaining < arpFadeOut ? (remaining / (float)arpFadeOut) : 1f;
                        atk = 0.5f - 0.5f * MathF.Cos(atk * MathF.PI);
                        tail = 0.5f - 0.5f * MathF.Cos(tail * MathF.PI);
                        float env = atk * tail;
                        float tone = driving
                            ? (float)(0.5 * (Math.Sin(phase) + 0.4 * Math.Sin(phase2)))
                            : (float)Math.Sin(phase);
                        buf[i] += tone * env * 0.22f;
                        phase += dPhase; if (phase > 2.0 * Math.PI) phase -= 2.0 * Math.PI;
                        phase2 += dPhase2; if (phase2 > 2.0 * Math.PI) phase2 -= 2.0 * Math.PI;
                    }
                }

                // Bass — one note per beat, longer sustain, softer attack.
                // Bass also transposes with the progression so each bar is rooted
                // on the current chord. Uses a per-note phase accumulator in
                // double precision — computing sin(t * freq * 2π) with floats
                // loses precision as the product grows and manifests as a
                // buzzy/static-like noise floor on sustained low notes.
                for (int b = 0; b < totalBeats; b++)
                {
                    float beatStart = b * beatSec;
                    int chordShift = ChordRootFor(b);
                    int idx = bassPattern[b % bassPattern.Length] + chordShift;
                    double freq = (root * 0.5) * Math.Pow(2.0, idx / 12.0); // one octave down
                    float noteDur = beatSec * 0.92f;
                    int i0 = (int)(beatStart * SampleRate);
                    int i1 = Math.Min(n, i0 + (int)(noteDur * SampleRate));
                    // Fade the tail explicitly over the last ~8ms so the note
                    // can't truncate at a non-zero sample (would click/hiss).
                    // Longer attack (25ms) and release (40ms) so there are no
                    // audible clicks inside the note — the shorter ramps were
                    // leaving the note's first/last few samples near full
                    // amplitude relative to the click-detection threshold of
                    // the output device.
                    int fadeIn = Math.Max(1, (int)(0.025f * SampleRate));
                    int fadeOut = Math.Max(1, (int)(0.040f * SampleRate));
                    double phase = 0.0;
                    double dPhase = 2.0 * Math.PI * freq / SampleRate;
                    int span = i1 - i0;
                    for (int i = i0; i < i1; i++)
                    {
                        int pos = i - i0;
                        int remaining = span - pos - 1; // 0 at the final sample → tail exactly 0
                        float attack = pos < fadeIn ? (pos / (float)fadeIn) : 1f;
                        float tail = remaining < fadeOut ? (remaining / (float)fadeOut) : 1f;
                        // Smooth (cosine) shape on both ramps kills the high-
                        // frequency content of the envelope edges — linear
                        // ramps can still produce a subtle tick on some audio
                        // hardware because their first derivative is
                        // discontinuous.
                        attack = 0.5f - 0.5f * MathF.Cos(attack * MathF.PI);
                        tail = 0.5f - 0.5f * MathF.Cos(tail * MathF.PI);
                        float env = attack * tail;
                        float tone = (float)(Math.Sin(phase) + 0.18 * Math.Sin(2.0 * phase));
                        buf[i] += tone * env * 0.28f;
                        phase += dPhase;
                        if (phase > 2.0 * Math.PI) phase -= 2.0 * Math.PI;
                    }
                }

                // Soft pad in menu mode — sustained root+fifth that follows the
                // progression bar-by-bar. Uses explicit phase accumulators so
                // the oscillators stay continuous when the chord (and therefore
                // the frequency) changes — using sin(t*f) would kink the phase
                // at every bar boundary and produce a crackly/static artefact.
                if (!driving)
                {
                    double phase1 = 0.0, phase2 = 0.0;
                    double dt = 1.0 / SampleRate;
                    int samplesPerBar = (int)(beatsPerBar * beatSec * SampleRate);
                    // Smooth the target frequency toward the current chord so
                    // the transition itself is glide-free and click-free.
                    float curMul = 1f;
                    float targetMul = 1f;
                    for (int i = 0; i < n; i++)
                    {
                        float t = i / (float)SampleRate;
                        int beat = (int)(t / beatSec);
                        int curShift = ChordRootFor(beat);
                        targetMul = MathF.Pow(2f, curShift / 12f);
                        // Short exponential glide (~30 ms) between chord roots.
                        curMul += (targetMul - curMul) * 0.0012f;
                        double f1 = root * curMul;
                        double f2 = root * curMul * Math.Pow(2.0, 7.0 / 12.0);
                        phase1 += 2.0 * Math.PI * f1 * dt;
                        phase2 += 2.0 * Math.PI * f2 * dt;
                        // Keep phases bounded to avoid precision loss over long runs.
                        if (phase1 > 2.0 * Math.PI) phase1 -= 2.0 * Math.PI;
                        if (phase2 > 2.0 * Math.PI) phase2 -= 2.0 * Math.PI;
                        float slow = 0.5f + 0.5f * MathF.Sin(t * 0.35f * 2f * MathF.PI);
                        float pad = 0.5f * (MathF.Sin((float)phase1) + MathF.Sin((float)phase2));
                        buf[i] += pad * slow * 0.08f;
                    }
                }

                // Gentle hat on the off-beats for driving styles.
                if (driving)
                {
                    var r = new Random(42);
                    for (int b = 0; b < totalBeats; b++)
                    {
                        float t0 = (b + 0.5f) * beatSec;
                        int i0 = (int)(t0 * SampleRate);
                        int hatLen = (int)(0.04f * SampleRate);
                        for (int k = 0; k < hatLen && i0 + k < n; k++)
                        {
                            float tt = k / (float)hatLen;
                            float env = MathF.Max(0f, 1f - tt);
                            buf[i0 + k] += (float)(r.NextDouble() * 2 - 1) * env * 0.08f;
                        }
                    }
                }

                // Gentle loop crossfade so the seam is inaudible.
                int fade = Math.Min(n / 32, (int)(0.25f * SampleRate));
                for (int k = 0; k < fade; k++)
                {
                    float w = k / (float)fade;
                    buf[k] *= w;
                    buf[n - 1 - k] *= w;
                }

                // Normalize softly to avoid clipping from the layered parts.
                float peak = 0.0001f;
                for (int i = 0; i < n; i++) if (MathF.Abs(buf[i]) > peak) peak = MathF.Abs(buf[i]);
                if (peak > 1f) { float g = 1f / peak; for (int i = 0; i < n; i++) buf[i] *= g; }
                return buf;
            }
            catch { return null; }
        }

        // Loops a cached PCM buffer indefinitely until Stop() is called, after
        // which Read returns 0 and the mixer removes the input.
        private sealed class LoopingSampleProvider : ISampleProvider
        {
            private readonly float[] _data;
            private int _pos;
            private bool _stopped;
            public WaveFormat WaveFormat { get; }
            public LoopingSampleProvider(float[] data, int sampleRate)
            {
                _data = data;
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
            }
            public void Stop() => _stopped = true;
            public int Read(float[] buffer, int offset, int count)
            {
                if (_stopped || _data.Length == 0) return 0;
                int written = 0;
                while (written < count)
                {
                    int avail = _data.Length - _pos;
                    int take = Math.Min(avail, count - written);
                    Array.Copy(_data, _pos, buffer, offset + written, take);
                    _pos += take;
                    written += take;
                    if (_pos >= _data.Length) _pos = 0;
                }
                return written;
            }
        }

        // ---- Synthesis helpers ----
        private static float Sine(float t, float f) => MathF.Sin(t * f * 2f * MathF.PI);
        private static float Square(float t, float f) => MathF.Sin(t * f * 2f * MathF.PI) >= 0 ? 1f : -1f;
        private static float Saw(float t, float f) { float p = (t * f) % 1f; return p * 2f - 1f; }
        private static readonly Random noiseRng = new Random(17);
        private static float Noise() => (float)(noiseRng.NextDouble() * 2 - 1);
        private static float Lerp(float a, float b, float u) { u = Math.Clamp(u, 0f, 1f); return a + (b - a) * u; }
        private static float Env(float t, float dur, float attack, float release)
        {
            if (t < attack) return t / attack;
            float r = dur - release;
            if (t > r) return Math.Max(0f, 1f - (t - r) / Math.Max(0.0001f, dur - r));
            return 1f;
        }

        private static float[] Synthesize(float durSec, Func<float, float, float> sample)
        {
            int n = (int)(durSec * SampleRate);
            var buf = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SampleRate;
                buf[i] = Math.Clamp(sample(t, durSec), -1f, 1f);
            }
            return buf;
        }

        private static void Register(string name, float[] buf) => cache[name] = buf;

        // One-shot sample provider that plays a cached float[] once then ends.
        private sealed class CachedSoundSampleProvider : ISampleProvider
        {
            private readonly float[] _data;
            private readonly float _vol;
            private int _pos;
            public WaveFormat WaveFormat { get; }
            public CachedSoundSampleProvider(float[] data, int sampleRate, float volume)
            {
                _data = data;
                _vol = volume;
                WaveFormat = WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1);
            }
            public int Read(float[] buffer, int offset, int count)
            {
                int avail = _data.Length - _pos;
                int take = Math.Min(avail, count);
                for (int i = 0; i < take; i++) buffer[offset + i] = _data[_pos + i] * _vol;
                _pos += take;
                return take;
            }
        }
    }
}
