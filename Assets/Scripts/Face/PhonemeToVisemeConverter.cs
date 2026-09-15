using System;
using System.Collections.Generic;
using Newtonsoft.Json;

/// <summary>A pre-aligned ARPAbet/PocketSphinx phoneme interval, in seconds from audio start.</summary>
[Serializable]
public sealed class AlignedPhoneme {
    [JsonProperty("phone", Required = Required.Always)] public string Phone;
    [JsonProperty("start", Required = Required.Always)] public float Start;
    [JsonProperty("end", Required = Required.Always)] public float End;
}

/// <summary>
/// Converts already aligned phonemes into FACS viseme cues. Does not perform acoustic alignment,
/// call an LLM, read audio, modify files, or start playback. Input must be ordered and non-overlapping.
/// Mapping and diphthong splits are animation heuristics, not measured articulatory trajectories.
/// </summary>
public static class PhonemeToVisemeConverter {
    private static readonly Dictionary<string, string> Mapping = new Dictionary<string, string> {
        { "AA", "AH" }, { "AH", "AH" }, { "AE", "AE" }, { "EH", "AE" },
        { "AO", "OH" }, { "OW", "OH" }, { "UH", "W_OO" }, { "UW", "W_OO" },
        { "IY", "EE" }, { "IH", "IH" }, { "ER", "R" },
        { "B", "B_M_P" }, { "M", "B_M_P" }, { "P", "B_M_P" },
        { "F", "F_V" }, { "V", "F_V" },
        { "CH", "CH_J" }, { "JH", "CH_J" }, { "SH", "CH_J" }, { "ZH", "CH_J" },
        { "S", "S_Z" }, { "Z", "S_Z" }, { "TH", "TH" }, { "DH", "TH" },
        { "T", "T_L_D_N" }, { "D", "T_L_D_N" }, { "L", "T_L_D_N" }, { "N", "T_L_D_N" },
        { "K", "K_G_H_NG" }, { "G", "K_G_H_NG" }, { "HH", "K_G_H_NG" }, { "NG", "K_G_H_NG" },
        { "R", "R" }, { "W", "W_OO" }, { "Y", "EE" },
        { "SIL", "sil" }, { "<SIL>", "sil" }, { "<S>", "sil" }, { "</S>", "sil" }
    };

    /// <param name="phonemes">Aligned phonemes; gaps are interpreted as silence.</param>
    /// <param name="audioDuration">Measured original audio length, excluding any alignment padding.</param>
    /// <param name="diphthongSplit">Fraction of a diphthong before its second shape (default 0.65).</param>
    public static List<VisemeFrame> Convert(IEnumerable<AlignedPhoneme> phonemes,
        float audioDuration, float diphthongSplit = 0.65f) {
        if (phonemes == null) throw new ArgumentNullException(nameof(phonemes));
        if (!Finite(audioDuration) || audioDuration <= 0)
            throw new ArgumentOutOfRangeException(nameof(audioDuration));
        if (!Finite(diphthongSplit) || diphthongSplit <= 0 || diphthongSplit >= 1)
            throw new ArgumentOutOfRangeException(nameof(diphthongSplit));

        var result = new List<VisemeFrame> { new VisemeFrame { time = 0, viseme = "sil" } };
        float previousEnd = 0;
        foreach (var interval in phonemes) {
            if (interval == null) throw new ArgumentException("Null phoneme interval.", nameof(phonemes));
            if (!Finite(interval.Start) || !Finite(interval.End) || interval.Start < previousEnd ||
                interval.End <= interval.Start || interval.End > audioDuration)
                throw new ArgumentException("Phoneme intervals must be positive-length, ordered, non-overlapping, and within the original audio duration.", nameof(phonemes));

            string phone = Normalize(interval.Phone);
            string first, second = null;
            switch (phone) {
                case "EY": first = "AE"; second = "IH"; break;
                case "AY": first = "AH"; second = "EE"; break;
                case "AW": first = "AH"; second = "W_OO"; break;
                case "OY": first = "OH"; second = "EE"; break;
                default:
                    if (!Mapping.TryGetValue(phone, out first))
                        throw new ArgumentException("Unsupported phoneme: " + interval.Phone, nameof(phonemes));
                    break;
            }
            if (interval.Start > previousEnd) Add(result, previousEnd, "sil");
            Add(result, interval.Start, first);
            if (second != null) {
                float split = interval.Start + (interval.End - interval.Start) * diphthongSplit;
                if (split <= interval.Start || split >= interval.End)
                    throw new ArgumentException("Phoneme interval is too short to represent a diphthong split.", nameof(phonemes));
                Add(result, split, second);
            }
            previousEnd = interval.End;
        }
        // End speech at its aligned endpoint, not by stretching it to the clip duration.
        Add(result, previousEnd, "sil");
        return result;
    }

    /// <summary>Input: [{"phone":"B","start":0.1,"end":0.2}, ...]. Output matches Parsers.ParseVisemes.</summary>
    public static string ConvertJson(string alignedPhonemeJson, float audioDuration,
        float diphthongSplit = 0.65f) {
        if (string.IsNullOrWhiteSpace(alignedPhonemeJson))
            throw new ArgumentException("Phoneme JSON is empty.", nameof(alignedPhonemeJson));
        var phonemes = JsonConvert.DeserializeObject<List<AlignedPhoneme>>(alignedPhonemeJson);
        return JsonConvert.SerializeObject(Convert(phonemes, audioDuration, diphthongSplit), Formatting.Indented);
    }

    private static string Normalize(string phone) {
        if (string.IsNullOrWhiteSpace(phone)) throw new ArgumentException("Phoneme label is empty.");
        // ARPAbet vowels may include a stress suffix: IY1, AH0, etc.
        return phone.Trim().ToUpperInvariant().TrimEnd('0', '1', '2');
    }

    private static bool Finite(float value) { return !float.IsNaN(value) && !float.IsInfinity(value); }

    private static void Add(List<VisemeFrame> frames, float time, string viseme) {
        var last = frames[frames.Count - 1];
        if (last.viseme == viseme) return;
        if (last.time == time) last.viseme = viseme;
        else frames.Add(new VisemeFrame { time = time, viseme = viseme });
    }
}
