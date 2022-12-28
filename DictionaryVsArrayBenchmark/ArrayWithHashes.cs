namespace DictionaryVsArrayBenchmark;

public sealed class ArrayWithHashes<T>
{
    private int capacity = 16;
    private int length = 0;
    private int[] hashes;
    private string[] keys;
    private T[] values;

    public ArrayWithHashes()
    {
        hashes = new int[capacity];
        keys = new string[capacity];
        values = new T[capacity];
    }

    private int FindIndex(int hash, string key)
    {
        var from = 0;
        while (from < length)
        {
            var j = hashes.AsSpan(from, length - from).IndexOf(hash);
            if (j == -1)
                return -1;

            var i = from + j;
            // Hashes are equal and we need to check whether keys are equal.
            if (keys[i] == key)
                return i;

            from = i + 1;
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
