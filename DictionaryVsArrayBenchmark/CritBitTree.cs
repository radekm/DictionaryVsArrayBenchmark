namespace DictionaryVsArrayBenchmark;

using System.Diagnostics;

/// This implementation supports at most 2^15 keys (external nodes).
public sealed class CritBitTree<T>
{
    private const int MaxNumberOfExternalNodes = 1 << 15;

    private struct Node
    {
        // Internal nodes: If the pointer `p` has lowest bit 0 then `p >> 1` is index into `nodes` array.
        // External nodes: If the pointer `p` has lowest bit 1 then `p >> 1` is index into `keys` array.
        public ushort pointer0;
        public ushort pointer1;
        // Position of differing `char`.
        public ushort position;
        // Bit mask which selects critical bit from `char`.
        public ushort mask;
    }

    private int capacity = 16;
    // Number of keys and values.
    // There's `length - 1` critical bits and nodes.
    private int length = 0;
    private ushort rootPointer = 0;
    private Node[] nodes;
    private string[] keys;
    private T[] values;

    public CritBitTree()
    {
        nodes = new Node[capacity];
        keys = new string[capacity];
        values = new T[capacity];
    }

    public void Add(string key, T value)
    {
        // Inserting into empty tree.
        if (length == 0)
        {
            keys[0] = key;
            values[0] = value;
            length = 1;
            rootPointer = 1;  // This is external node.
            return;
        }

        ref ushort pRef = ref rootPointer;
        // While `pRef` references a pointer which points to internal node.
        while ((pRef & 1) == 0)
        {
            // We must not copy node. We must reference it.
            // The reason is that we may want to modify pointer inside original node
            // (not inside a copy of original node).
            ref Node nodeRef = ref nodes[pRef >> 1];
            var c = nodeRef.position < key.Length ? key[nodeRef.position] : '\0';
            // Assign reference to either `pointer0` or `pointer1`.
            pRef = ref (c & nodeRef.mask) == 0 ? ref nodeRef.pointer0 : ref nodeRef.pointer1;
        }

        // Now `pRef` references a pointer which points to external node.
        string existingKey = keys[pRef >> 1];

        // Check if key already exists in the tree.
        if (existingKey == key)
        {
            values[pRef >> 1] = value;
            return;
        }

        if (length == MaxNumberOfExternalNodes)
            throw new Exception("CritBitTree is full");

        // `key` doesn't exist in the tree.
        // Find char where keys differ.
        // If one key is suffix of another then this loop will overflow.
        for (int i = 0; i <= ushort.MaxValue; i++)
        {
            var newC = i < key.Length ? key[i] : '\0';
            var existingC = i < existingKey.Length ? existingKey[i] : '\0';

            var differingBits = newC ^ existingC;

            if (differingBits == 0) continue;

            Node newNode;

            // Choose single bit where keys differ.
            var mask = differingBits & -differingBits;

            // Check that we found critical bit where both chars differ.
            Trace.Assert((newC & mask) != (existingC & mask));

            if ((newC & mask) == 0)
            {
                newNode.pointer0 = (ushort)(length << 1 | 1);
                newNode.pointer1 = pRef;
            }
            else
            {
                newNode.pointer0 = pRef;
                newNode.pointer1 = (ushort)(length << 1 | 1);
            }
            newNode.position = (ushort)i;
            newNode.mask = (ushort)mask;

            if (length == capacity)
            {
                capacity *= 2;

                var newNodes = new Node[capacity];
                var newKeys = new string[capacity];
                var newValues = new T[capacity];

                // Point to new internal node.
                // We do this AFTER allocating so if allocating fails our data structure is not broken.
                // We do this BEFORE copying so we copy correctly updated pointer.
                pRef = (ushort)(length << 1);

                nodes.AsSpan(0, length).CopyTo(newNodes.AsSpan(0, length));
                keys.AsSpan(0, length).CopyTo(newKeys.AsSpan(0, length));
                values.AsSpan(0, length).CopyTo(newValues.AsSpan(0, length));

                nodes = newNodes;
                keys = newKeys;
                values = newValues;
            }
            else
            {
                // Point to new internal node.
                pRef = (ushort)(length << 1);
            }

            // Add internal node.
            // `nodes[0]` is never used.
            nodes[length] = newNode;

            // Add external node.
            keys[length] = key;
            values[length] = value;

            length++;
            return;
        }

        throw new Exception(
            $"New string and existing string have same same prefix of length {ushort.MaxValue + 1}");
    }

    public T Find(string key)
    {
        if (length == 0) throw new Exception("Key not found (empty)");

        ushort p = rootPointer;
        // While `p` points to internal node.
        while ((p & 1) == 0)
        {
            var node = nodes[p >> 1];
            var c = node.position < key.Length ? key[node.position] : '\0';
            p = (c & node.mask) == 0 ? node.pointer0 : node.pointer1;
        }

        // Now `p` points to external node.
        if (keys[p >> 1] == key) return values[p >> 1];

        throw new Exception("Key not found");
    }
}
