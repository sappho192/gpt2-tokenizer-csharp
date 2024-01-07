# gpt2-tokenizer-csharp
C# implementation of GPT2 tokenizer based on the code from [hyunwoongko/gpt2-tokenizer-java](https://github.com/hyunwoongko/gpt2-tokenizer-java).

Following guide is also based on the original Java implementation.

## Add tokenizer files to resources directory
Please add `encoder.json` and `vocab.bpe` files to your project resources directory.
these files can be found [here](https://github.com/sappho192/gpt2-tokenizer-csharp/tree/main/GPT2Tokenizer/GPT2Tokenizer/tokenizers/gpt2).

## Usage
The following are simple examples of this library.
To check test code for this, refer to [here](https://github.com/sappho192/gpt2-tokenizer-csharp/blob/main/GPT2Tokenizer/GPT2TokenizerTest/GPT2TokenizerTest.cs).

### Encoding text to tokens
```CSharp
using GPT2Tokenizer;

GPT2Tokenizer tokenizer = GPT2Tokenizer.fromPretrained("PATH/IN/RESOURCES");
List<int> result = tokenizer.encode("Hello my name is Kevin.");
```
```
[15496, 616, 1438, 318, 7939, 13]
```

### Decoding tokens to text
```CSharp
using GPT2Tokenizer;

GPT2Tokenizer tokenizer = GPT2Tokenizer.fromPretrained("PATH/IN/RESOURCES");
string result = tokenizer.decode(List.of(15496, 616, 1438, 318, 7939, 13));
```
```
"Hello my name is Kevin."
```
