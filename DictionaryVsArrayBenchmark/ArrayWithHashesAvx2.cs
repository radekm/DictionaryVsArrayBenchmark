using System.Numerics;
using System.Runtime.Intrinsics;

namespace DictionaryVsArrayBenchmark;

public sealed class ArrayWithHashesAvx2<T>
{
    private int capacity = 16;
    private int length = 0;
    private int[] hashes;
    private string[] keys;
    private T[] values;

    public ArrayWithHashesAvx2()
    {
        hashes = new int[capacity];
        keys = new string[capacity];
        values = new T[capacity];
    }

    private int FindIndex(int hash, string key)
    {
        var needleVec = Vector256.Create(hash);
        for (var from = 0; from < length; from += Vector256<int>.Count)
        {
            var hashesVec = Vector256.LoadUnsafe(ref hashes[from]);
            var eq = Vector256.Equals(hashesVec, needleVec);

            // We're pessimists and assume that hash usually doesn't match.
            if (Vector256.EqualsAll(eq, Vector256<int>.Zero))
                continue;

            var mask = eq.ExtractMostSignificantBits();
            do
            {
                var j = BitOperations.TrailingZeroCount(mask);
                var i = from + j;
                if (keys[i] == key)
                    return i;
                mask &= ~(1u << j);
            } while (mask != 0);
        }
        return -1;
    }

    public void Add(string key, T value)
    {
        var hash = key.GetHashCode();
        var i = FindIndex(hash, key);
        if (i == -1)
        {
            if (length == capacity)
            {
                capacity *= 2;

                var newHashes = new int[capacity];
                hashes.AsSpan(0, length).CopyTo(newHashes.AsSpan(0, length));
                var newKeys = new string[capacity];
                keys.AsSpan(0, length).CopyTo(newKeys.AsSpan(0, length));
                var newValues = new T[capacity];
                values.AsSpan(0, length).CopyTo(newValues.AsSpan(0, length));

                hashes = newHashes;
                keys = newKeys;
                values = newValues;
            }

            hashes[length] = hash;
            keys[length] = key;
            values[length] = value;
            length++;
        }
        else
        {
            values[i] = value;
        }
    }

    public T Find(string key)
    {
        var i = FindIndex(key.GetHashCode(), key);
        if (i == -1) throw new Exception("Key not found");
        return values[i];
    }
}
