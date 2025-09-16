using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

class PhonemeExtractor {
    private static Dictionary<string, string[]> cmuDict = new Dictionary<string, string[]>();

    public static async Task InitializeCMUDict() {
        using(HttpClient client = new HttpClient()) {
            string cmuDictUrl = "https://raw.githubusercontent.com/cmusphinx/cmudict/master/cmudict.dict";
            string cmuData = await client.GetStringAsync(cmuDictUrl);
            foreach(var line in cmuData.Split('\n')) {
                if(!string.IsNullOrWhiteSpace(line) && !line.StartsWith(";;;")) {
                    var parts = line.Split("  ", StringSplitOptions.RemoveEmptyEntries);
                    if(parts.Length == 2) {
                        cmuDict[parts[0].ToLower()] = parts[1].Split(' ');
                    }
                }
            }
        }
    }

    public static async Task<List<string>> GetPhonemes(string sentence) {
        if(!cmuDict.Any()) {
            await InitializeCMUDict();
        }

        string[] words = sentence.ToLower().Replace(",", "").Replace(".", "").Split(' ');
        List<string> phonemes = new List<string>();

        foreach(var word in words) {
            if(cmuDict.ContainsKey(word)) {
                phonemes.AddRange(cmuDict[word]);
            }
            else {
                phonemes.AddRange(CallG2PModel(word));
            }
        }

        return phonemes;
    }

    private static List<string> CallG2PModel(string word) {
        // Placeholder: In real implementation, call an external G2P API or model
        Console.WriteLine($"G2P model needed for: {word}");
        return new List<string> { "UH", "N" }; // Default unknown phoneme replacement
    }

    public static async Task Main() {
        string sentence = "And I get up and I go into the next room and Bob is lying on the floor, not moving. I say, Bob, and there's no answer. And then I shout, Bob, and there's no answer. And then I dialed 911.";
        List<string> phonemes = await GetPhonemes(sentence);
        Console.WriteLine(string.Join(", ", phonemes));
    }
}