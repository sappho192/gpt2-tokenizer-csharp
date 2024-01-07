using System.Text.RegularExpressions;
using System.Text;
using Newtonsoft.Json;

namespace GPT2Tokenizer
{
    public class GPT2Tokenizer
    {
        private List<string> bpe;
        private readonly Dictionary<string, object> encoder;
        private readonly Dictionary<object, string> decoder;

        private readonly Dictionary<string, string> cache = new();
        private readonly Dictionary<int, string> byte2unicode;
        private readonly Dictionary<Tuple<string, string>, int> bpeRanks = new();
        private readonly Regex pattern = new("'s|'t|'re|'ve|'m|'ll|'d| ?\\p{L}+| ?\\p{N}+| ?[^\\s\\p{L}\\p{N}]+|\\s+(?!\\S)|\\s+", RegexOptions.Compiled);

        private GPT2Tokenizer(string path)
        {
            var encoderFile = Path.Combine(path, Constants.ENCODER_FILE_NAME);
            var bpeFile = Path.Combine(path, Constants.VOCAB_FILE_NAME);

            try
            {
                // Read and parse the encoder file
                var encoderContent = File.ReadAllText(encoderFile);
                this.encoder = JsonConvert.DeserializeObject<Dictionary<string, object>>(encoderContent);

                // Create the decoder from the encoder
                this.decoder = this.encoder.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

                // Read the bpe file
                this.bpe = File.ReadAllLines(bpeFile).ToList();

                byte2unicode = ByteToUnicode();

                // Populate the bpeRanks dictionary
                for (int i = 0; i < this.bpe.Count; i++)
                {
                    var pairs = this.bpe[i].Split(' ');
                    var key = new Tuple<string, string>(pairs[0], pairs[1]);
                    this.bpeRanks[key] = i;
                }
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine(ex.StackTrace);
            }
            catch (JsonException ex)
            {
                Console.Error.WriteLine(ex.Message);
            }
        }

        public static GPT2Tokenizer FromPretrained(string path)
        {
            return new GPT2Tokenizer(path);
        }

        // Other methods go here
        private static HashSet<Tuple<string, string>> GetPairs(List<string> word)
        {
            HashSet<Tuple<string, string>> pairs = new HashSet<Tuple<string, string>>();
            string prevCharacter = word[0];
            foreach (string character in word.Skip(1))
            {
                pairs.Add(new Tuple<string, string>(prevCharacter, character));
                prevCharacter = character;
            }
            return pairs;
        }

        private static Dictionary<int, string> ByteToUnicode()
        {
            var bs = Enumerable.Range('!', '~' - '!' + 1)
                               .Union(Enumerable.Range('¡', '¬' - '¡' + 1))
                               .Union(Enumerable.Range('®', 'ÿ' - '®' + 1))
                               .ToList();

            var cs = new List<int>(bs);

            int n = 0;
            int max = (int)Math.Pow(2, 8);
            for (int b = 0; b < max; b++)
            {
                if (!bs.Contains(b))
                {
                    bs.Add(b);
                    cs.Add(max + n);
                    n += 1;
                }
            }

            var csString = cs.Select(i => ((char)i).ToString()).ToList();

            var output = new Dictionary<int, string>();
            for (int i = 0; i < bs.Count; i++)
            {
                output.Add(bs[i], csString[i]);
            }

            return output;
        }

        private string Bpe(string token)
        {
            if (cache.ContainsKey(token))
            {
                return cache[token];
            }

            var word = token.Select(i => i.ToString()).ToList();

            var pairs = GetPairs(word);

            while (true)
            {
                int minScore = int.MaxValue;
                Tuple<string, string>? bigram = null;

                foreach (var pair in pairs)
                {
                    if (bpeRanks.TryGetValue(pair, out int value))
                    {
                        int score = value;

                        if (score < minScore)
                        {
                            minScore = score;
                            bigram = pair;
                        }
                    }
                }

                if (bigram == null)
                {
                    break;
                }

                string first = bigram.Item1;
                string second = bigram.Item2;
                var newWord = new List<string>();
                int i = 0;

                while (i < word.Count)
                {
                    int j = IndexWithStartPosition(word, first, i);

                    if (j != -1)
                    {
                        newWord.AddRange(word.GetRange(i, j - i));
                        i = j;
                    }
                    else
                    {
                        newWord.AddRange(word.GetRange(i, word.Count - i));
                        break;
                    }

                    if (word[i].Equals(first) && i < word.Count - 1 && word[i + 1].Equals(second))
                    {
                        newWord.Add(first + second);
                        i += 2;
                    }
                    else
                    {
                        newWord.Add(word[i]);
                        i += 1;
                    }
                }

                word = newWord;
                if (word.Count == 1)
                {
                    break;
                }
                else
                {
                    pairs = GetPairs(word);
                }
            }

            string output = string.Join(" ", word);
            cache[token] = output;
            return output;
        }

        private static int IndexWithStartPosition<T>(List<T> list, T find, int startPosition)
        {
            if (list == null || list.Count == 0)
            {
                return -1;
            }
            for (int index = startPosition; index < list.Count; index++)
            {
                if (EqualityComparer<T>.Default.Equals(list[index], find))
                {
                    return index;
                }
            }
            return -1;
        }

        public List<int> Encode(string text)
        {
            var matches = pattern.Matches(text);
            var unicodes = new List<string>();
            var bpeTokens = new List<int>();

            foreach (Match match in matches.Cast<Match>())
            {
                var unicodeBuilder = new StringBuilder();
                foreach (byte b in Encoding.UTF8.GetBytes(match.Value))
                {
                    unicodeBuilder.Append(byte2unicode[(int)b]);
                }
                unicodes.Add(unicodeBuilder.ToString());
            }

            foreach (var token in unicodes)
            {
                foreach (var bpeToken in Bpe(token).Split(' '))
                {
                    bpeTokens.Add(Convert.ToInt32(encoder[bpeToken]));
                }
            }

            return bpeTokens;
        }

        public string Decode(List<int> tokens)
        {
            var textBuilder = new StringBuilder();
            var byteBufferList = new List<string>();

            foreach (var token in tokens)
            {
                decoder.TryGetValue((long)token, out string? decoded);
                textBuilder.Append(decoded);
            }
            var text = textBuilder.ToString();

            for (var i = 0; i < text.Length; i++)
            {
                //byteBufferList.Add(byte2unicode[(int)text[i]]);
                byteBufferList.Add(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(text[i].ToString())));
            }

            var byteBuffer = new byte[byteBufferList.Count];
            for (var i = 0; i < byteBuffer.Length; i++)
            {
                var byteString = byteBufferList[i];
                if (byteString == null)
                {
                    byteString = " ";
                }
                byteBuffer[i] = (byte)byteString[0];
            }

            return new string(Encoding.UTF8.GetChars(byteBuffer));
        }
    }
}
