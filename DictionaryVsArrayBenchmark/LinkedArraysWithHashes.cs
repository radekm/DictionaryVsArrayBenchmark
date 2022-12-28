namespace DictionaryVsArrayBenchmark;

public sealed class LinkedArraysWithHashes<T>
{
    private class Node
    {
        public readonly int capacity;
        public int length = 0;
        public readonly int[] hashes;
        public readonly string[] keys;
        public readonly T[] values;
        public Node? nextNode = null;

        public Node(int capacity)
        {
            this.capacity = capacity;
            hashes = new int[capacity];
            keys = new string[capacity];
            values = new T[capacity];
        }

        public int FindIndex(int hash, string key)
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
    }

    private readonly Node first;
    private Node last;

    public LinkedArraysWithHashes()
    {
        first = new(16);
        last = first;
    }

    private void FindNodeAndIndex(int hash, string key, out Node? node, out int index)
    {
        var cur = first;
        while (cur != null)
        {
            var i = cur.FindIndex(hash, key);
            if (i != -1)
            {
                node = cur;
                index = i;
                return;
            }
            cur = cur.nextNode;
        }
        node = null;
        index = -1;
    }

    public void Add(string key, T value)
    {
        var hash = key.GetHashCode();
        FindNodeAndIndex(hash, key, out var node, out var i);

        if (node == null)
        {
            if (last.length == last.capacity)
            {
                Node n = new(2 * last.capacity);
                last.nextNode = n;
                last = n;
            }

            last.hashes[last.length] = hash;
            last.keys[last.length] = key;
            last.values[last.length] = value;
            last.length++;
        }
        else
        {
            node.values[i] = value;
        }
    }

    public T Find(string key)
    {
        FindNodeAndIndex(key.GetHashCode(), key, out var node, out var i);
        if (node == null) throw new Exception("Key not found");
        return node.values[i];
    }
}
