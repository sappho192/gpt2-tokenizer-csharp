namespace GPT2Tokenizer
{
    public class GPT2TokenizerTest
    {
        private string encodingExample = "Hello my name is Kevin.";
        private List<int> decodingExample = [15496, 616, 1438, 318, 7939, 13];

        private string encodingLongTextExample = "interesting";
        List<int> decodingLongTextExample = [47914];

        [Fact]
        public void TestEncoding()
        {
            GPT2Tokenizer tokenizer = GPT2Tokenizer.FromPretrained("tokenizers/gpt2");
            List<int> result = tokenizer.Encode(encodingExample);
            Console.WriteLine($"{string.Join(",", decodingExample)} vs {string.Join(",", result)}");
            Assert.Equal(decodingExample, result);
        }

        [Fact]
        public void TestDecoding()
        {
            GPT2Tokenizer tokenizer = GPT2Tokenizer.FromPretrained("tokenizers/gpt2");
            string result = tokenizer.Decode(decodingExample);
            Console.WriteLine($"{encodingExample} vs {string.Join(",", result)}");
            Assert.Equal(encodingExample, result);
        }

        [Fact]
        public void TestLongWord()
        {
            GPT2Tokenizer tokenizer = GPT2Tokenizer.FromPretrained("tokenizers/gpt2");
            List<int> result = tokenizer.Encode(encodingLongTextExample);
            Console.WriteLine($"{string.Join(",", decodingLongTextExample)} vs {string.Join(",", result)}");
            Assert.Equal(decodingLongTextExample, result);
        }
    }
}