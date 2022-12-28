namespace DictionaryVsArrayBenchmark;

public sealed class ArrayWithoutHashes<T>
{
    private int capacity = 16;
    private int length = 0;
    private string[] keys;
    private T[] values;

    public ArrayWithoutHashes()
    {
        keys = new string[capacity];
        values = new T[capacity];
    }

    private int FindIndex(string key)
    {
        for (var i = 0; i < keys.Length; i++)
        {
            if (keys[i] == key)
                return i;
        }
        return -1;
    }

    public void Add(string key, T value)
    {
        var i = FindIndex(key);
        if (i == -1)
        {
            if (length == capacity)
            {
                capacity *= 2;

                var newKeys = new string[capacity];
                keys.AsSpan(0, length).CopyTo(newKeys.AsSpan(0, length));
                var newValues = new T[capacity];
                values.AsSpan(0, length).CopyTo(newValues.AsSpan(0, length));

                keys = newKeys;
                values = newValues;
            }

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
        var i = FindIndex(key);
        if (i == -1) throw new Exception("Key not found");
        return values[i];
    }
}
