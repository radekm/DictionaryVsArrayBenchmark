#  String dictionary vs plain array

Let's measure when plain arrays and SIMD instruction are
faster than `Dictionary` when keys have type `string`.

My configuration is

```
BenchmarkDotNet=v0.13.3, OS=macOS 13.0.1 (22A400) [Darwin 22.1.0]
Intel Core i7-9750H CPU 2.60GHz, 1 CPU, 12 logical and 6 physical cores
.NET SDK=7.0.100
  [Host]     : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
  DefaultJob : .NET 7.0.0 (7.0.22.51805), X64 RyuJIT AVX2
```

Implementations:

- Methods suffixed **Dictionary** use standard `System.Collections.Generic.Dictionary`.
- Methods suffixed **ArrayWithHashes** use three arrays `hashes`, `keys`, `values`.
  - First we search hash in array `hashes` by `MemoryExtensions.IndexOf`.
  - If the hash matches we check that the key equals to the corresponding key in `keys`
    (because two keys could have same hash).
  - Then we retrieve corresponding value from `values`.
- Methods suffixed **ArrayWithHashesAvx2** are similar to **ArrayWithHashes**
  except that instead of `MemoryExtensions.IndexOf` use custom function.
- Methods suffixed **LinkedArraysWithHashes** use again three arrays
  but don't resize them. Instead there's a linked list where each node contains
  three arrays. When it runs out of space new list node is allocated with new three
  arrays with doubled capacity.

## Construction

Constructing dictionaries of size `N` by adding elements one by one:

|                     Method |   N |        Mean |     Error |    StdDev |   Gen0 |   Gen1 | Allocated |
|--------------------------- |---- |------------:|----------:|----------:|-------:|-------:|----------:|
|             MakeDictionary |  16 |    610.7 ns |   4.13 ns |   3.67 ns | 0.1650 |      - |    1040 B |
|        MakeArrayWithHashes |  16 |    465.9 ns |   2.39 ns |   2.23 ns | 0.0672 |      - |     424 B |
|    MakeArrayWithHashesAvx2 |  16 |    437.9 ns |   2.05 ns |   1.82 ns | 0.0672 |      - |     424 B |
| MakeLinkedArraysWithHashes |  16 |    497.7 ns |   3.43 ns |   3.21 ns | 0.0734 |      - |     464 B |
|             MakeDictionary |  32 |  1,193.5 ns |   6.44 ns |   5.37 ns | 0.3376 |      - |    2128 B |
|        MakeArrayWithHashes |  32 |    976.9 ns |   7.95 ns |   7.44 ns | 0.1602 |      - |    1008 B |
|    MakeArrayWithHashesAvx2 |  32 |    911.3 ns |   5.22 ns |   4.36 ns | 0.1602 |      - |    1008 B |
| MakeLinkedArraysWithHashes |  32 |  1,104.8 ns |   5.62 ns |   4.70 ns | 0.1755 |      - |    1104 B |
|             MakeDictionary |  64 |  2,448.1 ns |  20.78 ns |  18.42 ns | 0.7439 | 0.0076 |    4672 B |
|        MakeArrayWithHashes |  64 |  2,035.6 ns |  12.42 ns |  11.01 ns | 0.3319 |      - |    2104 B |
|    MakeArrayWithHashesAvx2 |  64 |  1,968.2 ns |   8.87 ns |   7.86 ns | 0.3319 |      - |    2104 B |
| MakeLinkedArraysWithHashes |  64 |  2,402.0 ns |  19.67 ns |  18.40 ns | 0.3586 | 0.0038 |    2256 B |
|             MakeDictionary | 128 |  4,979.0 ns |  18.31 ns |  14.30 ns | 1.6251 | 0.0381 |   10240 B |
|        MakeArrayWithHashes | 128 |  4,152.2 ns |  34.28 ns |  32.07 ns | 0.6714 | 0.0076 |    4224 B |
|    MakeArrayWithHashesAvx2 | 128 |  4,882.7 ns |  26.45 ns |  24.74 ns | 0.6714 | 0.0076 |    4224 B |
| MakeLinkedArraysWithHashes | 128 |  5,468.9 ns |  38.32 ns |  33.97 ns | 0.7019 | 0.0153 |    4432 B |
|             MakeDictionary | 256 | 10,195.5 ns |  68.54 ns |  60.76 ns | 3.5553 | 0.1831 |   22360 B |
|        MakeArrayWithHashes | 256 |  9,792.9 ns |  53.22 ns |  44.44 ns | 1.3275 | 0.0153 |    8392 B |
|    MakeArrayWithHashesAvx2 | 256 | 11,876.8 ns |  40.16 ns |  33.54 ns | 1.3275 | 0.0153 |    8392 B |
| MakeLinkedArraysWithHashes | 256 | 13,645.8 ns | 127.91 ns | 113.39 ns | 1.3733 | 0.0610 |    8656 B |
|             MakeDictionary | 512 | 21,735.2 ns | 145.58 ns | 129.06 ns | 7.6599 | 0.8240 |   48144 B |
|        MakeArrayWithHashes | 512 | 26,232.6 ns | 121.76 ns | 113.89 ns | 2.6550 | 0.0916 |   16656 B |
|    MakeArrayWithHashesAvx2 | 512 | 32,639.9 ns | 195.69 ns | 183.04 ns | 2.6245 | 0.0610 |   16656 B |
| MakeLinkedArraysWithHashes | 512 | 34,837.9 ns | 222.91 ns | 197.61 ns | 2.6855 | 0.2441 |   16976 B |

We can see that plain array (MakeArrayWithHashes) is faster when N <= 256.
When N = 512 `System.Collections.Generic.Dictionary` (MakeDictionary) becomes faster.
Our custom implementation of `IndexOf` using `Vector256` (MakeArrayWithHashesAvx2)
is also faster than `MemoryExtensions.IndexOf` (MakeArrayWithHashes) only when N <= 256.

From memory perspective plain arrays allocate 2 times less memory.

## Searching for one element

When dictionary contains 32 key-value pairs:

|                 Method |         Needle |      Mean |     Error |    StdDev |
|----------------------- |--------------- |----------:|----------:|----------:|
|             Dictionary | from beginning | 10.019 ns | 0.0875 ns | 0.0819 ns |
|        ArrayWithHashes | from beginning |  9.667 ns | 0.0508 ns | 0.0451 ns |
|    ArrayWithHashesAvx2 | from beginning |  8.554 ns | 0.0453 ns | 0.0378 ns |
| LinkedArraysWithHashes | from beginning | 13.889 ns | 0.0987 ns | 0.0875 ns |
|             Dictionary |    from middle | 10.439 ns | 0.0482 ns | 0.0427 ns |
|        ArrayWithHashes |    from middle | 10.970 ns | 0.0526 ns | 0.0466 ns |
|    ArrayWithHashesAvx2 |    from middle | 10.953 ns | 0.0675 ns | 0.0599 ns |
| LinkedArraysWithHashes |    from middle | 19.519 ns | 0.1030 ns | 0.0963 ns |
|             Dictionary |       from end | 10.148 ns | 0.1658 ns | 0.1551 ns |
|        ArrayWithHashes |       from end | 12.306 ns | 0.0922 ns | 0.0770 ns |
|    ArrayWithHashesAvx2 |       from end | 13.942 ns | 0.0885 ns | 0.0828 ns |
| LinkedArraysWithHashes |       from end | 22.093 ns | 0.1131 ns | 0.1058 ns |

When dictionary contains 16 key-value pairs:

|                 Method |         Needle |      Mean |     Error |    StdDev |
|----------------------- |--------------- |----------:|----------:|----------:|
|             Dictionary | from beginning | 11.634 ns | 0.0643 ns | 0.0602 ns |
|        ArrayWithHashes | from beginning |  9.709 ns | 0.0515 ns | 0.0482 ns |
|    ArrayWithHashesAvx2 | from beginning |  8.909 ns | 0.0427 ns | 0.0399 ns |
| LinkedArraysWithHashes | from beginning | 14.033 ns | 0.0843 ns | 0.0747 ns |
|             Dictionary |    from middle |  9.777 ns | 0.0539 ns | 0.0478 ns |
|        ArrayWithHashes |    from middle | 11.145 ns | 0.0608 ns | 0.0569 ns |
|    ArrayWithHashesAvx2 |    from middle | 10.052 ns | 0.0582 ns | 0.0516 ns |
| LinkedArraysWithHashes |    from middle | 14.767 ns | 0.1112 ns | 0.1040 ns |
|             Dictionary |       from end |  9.825 ns | 0.0703 ns | 0.0657 ns |
|        ArrayWithHashes |       from end |  9.752 ns | 0.0551 ns | 0.0516 ns |
|    ArrayWithHashesAvx2 |       from end | 10.358 ns | 0.0802 ns | 0.0669 ns |
| LinkedArraysWithHashes |       from end | 14.308 ns | 0.1273 ns | 0.0994 ns |

## Searching for all elements

We search for all elements but in random order.

When dictionary contains 32 key-value pairs:

|                 Method |     Mean |    Error |   StdDev |   Median |
|----------------------- |---------:|---------:|---------:|---------:|
|             Dictionary | 337.4 ns |  4.83 ns |  4.51 ns | 336.7 ns |
|        ArrayWithHashes | 330.8 ns |  1.60 ns |  1.50 ns | 330.7 ns |
|    ArrayWithHashesAvx2 | 322.1 ns |  3.52 ns |  3.29 ns | 321.1 ns |
| LinkedArraysWithHashes | 572.5 ns | 11.25 ns | 18.79 ns | 560.9 ns |

When dictionary contains 16 key-value pairs:

|                 Method |     Mean |   Error |  StdDev |
|----------------------- |---------:|--------:|--------:|
|             Dictionary | 170.3 ns | 1.01 ns | 0.95 ns |
|        ArrayWithHashes | 157.2 ns | 0.71 ns | 0.63 ns |
|    ArrayWithHashesAvx2 | 146.0 ns | 0.84 ns | 0.79 ns |
| LinkedArraysWithHashes | 229.5 ns | 0.91 ns | 0.85 ns |

## Summary

It seems that on Intel Core i7-9750H
array performs better than `System.Collections.Generic.Dictionary`
when the number of elements is <= 32.

For dictionaries with size <= 256 it could still make sense
to use array when Find is not used frequently and cheaper construction
outweighs slightly slower Find.

For larger dictionaries with > 256 elements `Dictionary` is better choice than array.
