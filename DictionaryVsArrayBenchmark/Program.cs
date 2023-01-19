namespace DictionaryVsArrayBenchmark;

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

public record Needle(string Desc, string Word)
{
    public override string ToString() => Desc;
}

public static class Common
{
    public static string[] ReadWords() => File.ReadAllLines("google-10000-english-usa.txt");
}

[MemoryDiagnoser]
public class ConstructBenchmark
{
    private readonly string[] words;

    public ConstructBenchmark()
    {
        words = Common.ReadWords();
    }

    [Params(16, 32, 64, 128, 256, 512)]
    public int N { get; set; }

    [Benchmark]
    public Dictionary<string, int> MakeDictionary()
    {
        Dictionary<string, int> result = new();
        foreach (var word in words.Take(N))
        {
            result[word] = word.Length;
        }
        return result;
    }

    // Slow!
    // [Benchmark]
    // public ArrayWithoutHashes<int> MakeArrayWithoutHashes()
    // {
    //     ArrayWithoutHashes<int> result = new();
    //     foreach (var word in words.Take(N))
    //     {
    //         result.Add(word, word.Length);
    //     }
    //     return result;
    // }

    [Benchmark]
    public ArrayWithHashes<int> MakeArrayWithHashes()
    {
        ArrayWithHashes<int> result = new();
        foreach (var word in words.Take(N))
        {
            result.Add(word, word.Length);
        }
        return result;
    }

    [Benchmark]
    public ArrayWithHashesAvx2<int> MakeArrayWithHashesAvx2()
    {
        ArrayWithHashesAvx2<int> result = new();
        foreach (var word in words.Take(N))
        {
            result.Add(word, word.Length);
        }
        return result;
    }

    [Benchmark]
    public LinkedArraysWithHashes<int> MakeLinkedArraysWithHashes()
    {
        LinkedArraysWithHashes<int> result = new();
        foreach (var word in words.Take(N))
        {
            result.Add(word, word.Length);
        }
        return result;
    }

    [Benchmark]
    public CritBitTree<int> MakeCritBitTree()
    {
        CritBitTree<int> result = new();
        foreach (var word in words.Take(N))
        {
            result.Add(word, word.Length);
        }
        return result;
    }
}

public class FindOneBenchmark
{
    private readonly string[] words;
    private readonly Dictionary<string, int> dict = new();
    private readonly ArrayWithoutHashes<int> arrWithoutHashes = new();
    private readonly ArrayWithHashes<int> arrWithHashes = new();
    private readonly ArrayWithHashesAvx2<int> arrWithHashesAvx2 = new();
    private readonly LinkedArraysWithHashes<int> linkedArrsWithHashes = new();
    private readonly CritBitTree<int> critBitTree = new();

    public FindOneBenchmark()
    {
        words = Common.ReadWords().Take(16).ToArray();

        foreach (var word in words)
        {
            var value = word.Length;
            dict[word] = value;
            arrWithoutHashes.Add(word, value);
            arrWithHashes.Add(word, value);
            arrWithHashesAvx2.Add(word, value);
            linkedArrsWithHashes.Add(word, value);
            critBitTree.Add(word, value);
        }
    }

    [ParamsSource(nameof(Needles))]
    public Needle Needle;

    public IEnumerable<Needle> Needles => new[]
    {
        new Needle("from beginning", words.First()),
        new Needle("from middle", words[words.Length / 2]),
        new Needle("from end", words.Last()),
    };

    [Benchmark]
    public int Dictionary() => dict[Needle.Word];
    // Slow!
    // [Benchmark]
    // public int ArrayWithoutHashes() => arrWithoutHashes.Find(Needle.Word);
    [Benchmark]
    public int ArrayWithHashes() => arrWithHashes.Find(Needle.Word);
    [Benchmark]
    public int ArrayWithHashesAvx2() => arrWithHashesAvx2.Find(Needle.Word);
    [Benchmark]
    public int LinkedArraysWithHashes() => linkedArrsWithHashes.Find(Needle.Word);
    [Benchmark]
    public int CritBitTree() => critBitTree.Find(Needle.Word);
}

public class FindAllBenchmark
{
    private readonly string[] words;
    private readonly string[] shuffledWords;
    private readonly Dictionary<string, int> dict = new();
    private readonly ArrayWithoutHashes<int> arrWithoutHashes = new();
    private readonly ArrayWithHashes<int> arrWithHashes = new();
    private readonly ArrayWithHashesAvx2<int> arrWithHashesAvx2 = new();
    private readonly LinkedArraysWithHashes<int> linkedArrsWithHashes = new();
    private readonly CritBitTree<int> critBitTree = new();

    public static void Shuffle<T>(int seed, T[] array)
    {
        Random random = new(seed);
        int n = array.Length;
        while (n > 1)
        {
            int k = random.Next(n--);
            (array[n], array[k]) = (array[k], array[n]);
        }
    }

    public FindAllBenchmark()
    {
        words = Common.ReadWords().Take(16).ToArray();
        shuffledWords = words.ToArray();
        Shuffle(32, shuffledWords);

        foreach (var word in words)
        {
            var value = word.Length;
            dict[word] = value;
            arrWithoutHashes.Add(word, value);
            arrWithHashes.Add(word, value);
            arrWithHashesAvx2.Add(word, value);
            linkedArrsWithHashes.Add(word, value);
            critBitTree.Add(word, value);
        }
    }

    [Benchmark]
    public int Dictionary()
    {
        var result = 0;
        foreach (var word in shuffledWords)
        {
            result += dict[word];
        }
        return result;
    }

    // Slow!
    // [Benchmark]
    // public int ArrayWithoutHashes()
    // {
    //     var result = 0;
    //     foreach (var word in shuffledWords)
    //     {
    //         result += arrWithoutHashes.Find(word);
    //     }
    //     return result;
    // }

    [Benchmark]
    public int ArrayWithHashes()
    {
        var result = 0;
        foreach (var word in shuffledWords)
        {
            result += arrWithHashes.Find(word);
        }
        return result;
    }

    [Benchmark]
    public int ArrayWithHashesAvx2()
    {
        var result = 0;
        foreach (var word in shuffledWords)
        {
            result += arrWithHashesAvx2.Find(word);
        }
        return result;
    }

    [Benchmark]
    public int LinkedArraysWithHashes()
    {
        var result = 0;
        foreach (var word in shuffledWords)
        {
            result += linkedArrsWithHashes.Find(word);
        }
        return result;
    }

    [Benchmark]
    public int CritBitTree()
    {
        var result = 0;
        foreach (var word in shuffledWords)
        {
            result += critBitTree.Find(word);
        }
        return result;
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        BenchmarkRunner.Run<ConstructBenchmark>();
        BenchmarkRunner.Run<FindOneBenchmark>();
        BenchmarkRunner.Run<FindAllBenchmark>();
    }
}
