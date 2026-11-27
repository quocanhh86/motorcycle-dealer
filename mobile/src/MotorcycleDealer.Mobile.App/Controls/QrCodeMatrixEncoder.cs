using System.Text;

namespace MotorcycleDealer.Mobile.App.Controls;

/// <summary>
/// Pure C# AOT-safe ISO/IEC 18004 Byte-Mode QR Code Matrix Encoder (Versions 1..10, ECC Level M).
/// Enables 100% offline voucher, booking fast-check-in, and immutable receipt verification QR rendering
/// with zero native graphics or reflection dependencies.
/// </summary>
public static class QrCodeMatrixEncoder
{
    private readonly record struct VersionSpec(
        int Version,
        int Size,
        int TotalCodewords,
        int DataCodewords,
        int EcCodewordsPerBlock,
        int Group1Blocks,
        int Group1DataCodewords,
        int Group2Blocks,
        int Group2DataCodewords,
        int[] AlignmentCenters);

    private static readonly VersionSpec[] LevelMSpecs =
    [
        new(1,  21,  26,  16, 10, 1, 16, 0,  0, []),
        new(2,  25,  44,  28, 16, 1, 28, 0,  0, [6, 18]),
        new(3,  29,  70,  44, 26, 1, 44, 0,  0, [6, 22]),
        new(4,  33, 100,  64, 18, 2, 32, 0,  0, [6, 26]),
        new(5,  37, 134,  86, 24, 2, 43, 0,  0, [6, 30]),
        new(6,  41, 172, 108, 16, 4, 27, 0,  0, [6, 34]),
        new(7,  45, 196, 124, 18, 4, 31, 0,  0, [6, 22, 38]),
        new(8,  49, 242, 154, 22, 2, 38, 2, 39, [6, 24, 42]),
        new(9,  53, 292, 182, 22, 3, 36, 2, 37, [6, 26, 46]),
        new(10, 57, 346, 216, 26, 4, 43, 1, 44, [6, 28, 50])
    ];

    private static readonly byte[] GfExp = new byte[512];
    private static readonly byte[] GfLog = new byte[256];

    static QrCodeMatrixEncoder()
    {
        int x = 1;
        for (int i = 0; i < 255; i++)
        {
            GfExp[i] = (byte)x;
            GfLog[x] = (byte)i;
            x <<= 1;
            if ((x & 0x100) != 0)
            {
                x ^= 0x11D;
            }
        }

        for (int i = 255; i < 512; i++)
        {
            GfExp[i] = GfExp[i - 255];
        }
    }

    public static bool[,] Encode(string payload)
    {
        string normalized = string.IsNullOrWhiteSpace(payload) ? "APEX-MOTO" : payload.Trim();
        byte[] bytes = Encoding.UTF8.GetBytes(normalized);

        VersionSpec spec = SelectVersion(bytes.Length);
        int maxPayloadBytes = spec.DataCodewords - (spec.Version <= 9 ? 2 : 3);
        if (bytes.Length > maxPayloadBytes)
        {
            bytes = bytes[..maxPayloadBytes];
        }

        byte[] dataCodewords = BuildDataCodewords(bytes, spec);
        byte[] allCodewords = BuildInterleavedCodewords(dataCodewords, spec);

        int size = spec.Size;
        bool[,] modules = new bool[size, size];
        bool[,] isFunction = new bool[size, size];

        DrawFunctionPatterns(modules, isFunction, spec);
        DrawCodewords(modules, isFunction, allCodewords, size);

        int bestMask = 0;
        int bestPenalty = int.MaxValue;
        bool[,]? bestMatrix = null;

        for (int mask = 0; mask < 8; mask++)
        {
            bool[,] candidate = (bool[,])modules.Clone();
            ApplyMask(candidate, isFunction, mask, size);
            DrawFormatBits(candidate, mask, size);
            int penalty = ComputePenaltyScore(candidate, size);
            if (penalty < bestPenalty)
            {
                bestPenalty = penalty;
                bestMask = mask;
                bestMatrix = candidate;
            }
        }

        _ = bestMask;
        return bestMatrix ?? modules;
    }

    private static VersionSpec SelectVersion(int byteLength)
    {
        foreach (VersionSpec spec in LevelMSpecs)
        {
            int countBits = spec.Version <= 9 ? 8 : 16;
            int totalBitsNeeded = 4 + countBits + (byteLength * 8);
            if (totalBitsNeeded <= spec.DataCodewords * 8)
            {
                return spec;
            }
        }

        return LevelMSpecs[^1];
    }

    private static byte[] BuildDataCodewords(byte[] payload, VersionSpec spec)
    {
        var bits = new List<bool>(spec.DataCodewords * 8);
        AppendBits(bits, 0b0100, 4); // Byte mode
        AppendBits(bits, payload.Length, spec.Version <= 9 ? 8 : 16);

        foreach (byte b in payload)
        {
            AppendBits(bits, b, 8);
        }

        int capacityBits = spec.DataCodewords * 8;
        int terminatorLength = Math.Min(4, capacityBits - bits.Count);
        AppendBits(bits, 0, terminatorLength);

        while (bits.Count % 8 != 0)
        {
            bits.Add(false);
        }

        byte[] codewords = new byte[spec.DataCodewords];
        int cwIndex = 0;
        for (int i = 0; i < bits.Count; i += 8)
        {
            byte value = 0;
            for (int bit = 0; bit < 8; bit++)
            {
                value = (byte)((value << 1) | (bits[i + bit] ? 1 : 0));
            }

            codewords[cwIndex++] = value;
        }

        bool padToggle = true;
        while (cwIndex < codewords.Length)
        {
            codewords[cwIndex++] = padToggle ? (byte)0xEC : (byte)0x11;
            padToggle = !padToggle;
        }

        return codewords;
    }

    private static byte[] BuildInterleavedCodewords(byte[] dataCodewords, VersionSpec spec)
    {
        int totalBlocks = spec.Group1Blocks + spec.Group2Blocks;
        byte[][] dataBlocks = new byte[totalBlocks][];
        byte[][] ecBlocks = new byte[totalBlocks][];
        byte[] generator = ComputeReedSolomonGenerator(spec.EcCodewordsPerBlock);

        int offset = 0;
        int blockIdx = 0;
        for (int i = 0; i < spec.Group1Blocks; i++, blockIdx++)
        {
            dataBlocks[blockIdx] = dataCodewords[offset..(offset + spec.Group1DataCodewords)];
            ecBlocks[blockIdx] = ComputeReedSolomonRemainder(dataBlocks[blockIdx], generator, spec.EcCodewordsPerBlock);
            offset += spec.Group1DataCodewords;
        }

        for (int i = 0; i < spec.Group2Blocks; i++, blockIdx++)
        {
            dataBlocks[blockIdx] = dataCodewords[offset..(offset + spec.Group2DataCodewords)];
            ecBlocks[blockIdx] = ComputeReedSolomonRemainder(dataBlocks[blockIdx], generator, spec.EcCodewordsPerBlock);
            offset += spec.Group2DataCodewords;
        }

        var result = new byte[spec.TotalCodewords];
        int outIdx = 0;
        int maxDataLen = Math.Max(spec.Group1DataCodewords, spec.Group2DataCodewords);

        for (int col = 0; col < maxDataLen; col++)
        {
            for (int b = 0; b < totalBlocks; b++)
            {
                if (col < dataBlocks[b].Length)
                {
                    result[outIdx++] = dataBlocks[b][col];
                }
            }
        }

        for (int col = 0; col < spec.EcCodewordsPerBlock; col++)
        {
            for (int b = 0; b < totalBlocks; b++)
            {
                result[outIdx++] = ecBlocks[b][col];
            }
        }

        return result;
    }

    private static byte[] ComputeReedSolomonGenerator(int degree)
    {
        byte[] poly = [1];
        for (int i = 0; i < degree; i++)
        {
            byte[] next = new byte[poly.Length + 1];
            byte root = GfExp[i];
            for (int j = 0; j < poly.Length; j++)
            {
                next[j] ^= poly[j];
                next[j + 1] ^= GfMultiply(poly[j], root);
            }

            poly = next;
        }

        return poly;
    }

    private static byte[] ComputeReedSolomonRemainder(byte[] data, byte[] generator, int ecCount)
    {
        byte[] remainder = new byte[ecCount];
        foreach (byte b in data)
        {
            byte factor = (byte)(b ^ remainder[0]);
            Array.Copy(remainder, 1, remainder, 0, ecCount - 1);
            remainder[ecCount - 1] = 0;

            for (int i = 0; i < ecCount; i++)
            {
                remainder[i] ^= GfMultiply(generator[i + 1], factor);
            }
        }

        return remainder;
    }

    private static byte GfMultiply(byte a, byte b)
        => a == 0 || b == 0 ? (byte)0 : GfExp[GfLog[a] + GfLog[b]];

    private static void DrawFunctionPatterns(bool[,] modules, bool[,] isFunction, VersionSpec spec)
    {
        int size = spec.Size;

        // Timing patterns
        for (int i = 0; i < size; i++)
        {
            SetFunctionModule(modules, isFunction, 6, i, i % 2 == 0);
            SetFunctionModule(modules, isFunction, i, 6, i % 2 == 0);
        }

        // Finder patterns + separators
        DrawFinderPattern(modules, isFunction, 3, 3, size);
        DrawFinderPattern(modules, isFunction, size - 4, 3, size);
        DrawFinderPattern(modules, isFunction, 3, size - 4, size);

        // Alignment patterns
        int[] centers = spec.AlignmentCenters;
        for (int i = 0; i < centers.Length; i++)
        {
            for (int j = 0; j < centers.Length; j++)
            {
                bool isCornerFinder = (i == 0 && j == 0)
                    || (i == 0 && j == centers.Length - 1)
                    || (i == centers.Length - 1 && j == 0);
                if (!isCornerFinder)
                {
                    DrawAlignmentPattern(modules, isFunction, centers[i], centers[j]);
                }
            }
        }

        // Reserve format info areas (populated after masking)
        DrawFormatBits(modules, 0, size, isFunction);

        // Version info for V7..V10
        if (spec.Version >= 7)
        {
            DrawVersionBits(modules, isFunction, spec.Version, size);
        }
    }

    private static void DrawFinderPattern(bool[,] modules, bool[,] isFunction, int cx, int cy, int size)
    {
        for (int dy = -4; dy <= 4; dy++)
        {
            for (int dx = -4; dx <= 4; dx++)
            {
                int x = cx + dx;
                int y = cy + dy;
                if (x >= 0 && x < size && y >= 0 && y < size)
                {
                    int dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
                    bool dark = dist != 2 && dist != 4;
                    SetFunctionModule(modules, isFunction, x, y, dark);
                }
            }
        }
    }

    private static void DrawAlignmentPattern(bool[,] modules, bool[,] isFunction, int cx, int cy)
    {
        for (int dy = -2; dy <= 2; dy++)
        {
            for (int dx = -2; dx <= 2; dx++)
            {
                int dist = Math.Max(Math.Abs(dx), Math.Abs(dy));
                SetFunctionModule(modules, isFunction, cx + dx, cy + dy, dist != 1);
            }
        }
    }

    private static void DrawFormatBits(bool[,] modules, int mask, int size, bool[,]? isFunction = null)
    {
        // ECC Level M format indicator bits = 00
        int data = (0b00 << 3) | (mask & 0b111);
        int rem = data;
        for (int i = 0; i < 10; i++)
        {
            rem = (rem << 1) ^ ((rem >> 9) * 0x537);
        }

        int bits = ((data << 10) | rem) ^ 0x5412;

        // Top-left copy
        for (int i = 0; i <= 5; i++)
        {
            SetFormatModule(modules, isFunction, 8, i, GetBit(bits, i));
        }

        SetFormatModule(modules, isFunction, 8, 7, GetBit(bits, 6));
        SetFormatModule(modules, isFunction, 8, 8, GetBit(bits, 7));
        SetFormatModule(modules, isFunction, 7, 8, GetBit(bits, 8));
        for (int i = 9; i < 15; i++)
        {
            SetFormatModule(modules, isFunction, 14 - i, 8, GetBit(bits, i));
        }

        // Bottom-left & top-right copy
        for (int i = 0; i < 8; i++)
        {
            SetFormatModule(modules, isFunction, size - 1 - i, 8, GetBit(bits, i));
        }

        for (int i = 8; i < 15; i++)
        {
            SetFormatModule(modules, isFunction, 8, size - 15 + i, GetBit(bits, i));
        }

        // Dark module
        SetFormatModule(modules, isFunction, 8, size - 8, true);
    }

    private static void DrawVersionBits(bool[,] modules, bool[,] isFunction, int version, int size)
    {
        int rem = version;
        for (int i = 0; i < 12; i++)
        {
            rem = (rem << 1) ^ ((rem >> 11) * 0x1F25);
        }

        int bits = (version << 12) | rem;
        for (int i = 0; i < 18; i++)
        {
            bool dark = GetBit(bits, i);
            int a = size - 11 + (i % 3);
            int b = i / 3;
            SetFunctionModule(modules, isFunction, a, b, dark);
            SetFunctionModule(modules, isFunction, b, a, dark);
        }
    }

    private static void DrawCodewords(bool[,] modules, bool[,] isFunction, byte[] data, int size)
    {
        int bitIndex = 0;
        int totalBits = data.Length * 8;

        for (int right = size - 1; right >= 1; right -= 2)
        {
            if (right == 6)
            {
                right = 5;
            }

            for (int vert = 0; vert < size; vert++)
            {
                for (int j = 0; j < 2; j++)
                {
                    int x = right - j;
                    bool upward = ((right + 1) & 2) == 0;
                    int y = upward ? size - 1 - vert : vert;

                    if (!isFunction[y, x] && bitIndex < totalBits)
                    {
                        bool dark = ((data[bitIndex >> 3] >> (7 - (bitIndex & 7))) & 1) != 0;
                        modules[y, x] = dark;
                        bitIndex++;
                    }
                }
            }
        }
    }

    private static void ApplyMask(bool[,] modules, bool[,] isFunction, int mask, int size)
    {
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (isFunction[y, x])
                {
                    continue;
                }

                bool invert = mask switch
                {
                    0 => (x + y) % 2 == 0,
                    1 => y % 2 == 0,
                    2 => x % 3 == 0,
                    3 => (x + y) % 3 == 0,
                    4 => ((x / 3) + (y / 2)) % 2 == 0,
                    5 => ((x * y) % 2) + ((x * y) % 3) == 0,
                    6 => (((x * y) % 2) + ((x * y) % 3)) % 2 == 0,
                    7 => (((x + y) % 2) + ((x * y) % 3)) % 2 == 0,
                    _ => false
                };

                if (invert)
                {
                    modules[y, x] = !modules[y, x];
                }
            }
        }
    }

    private static int ComputePenaltyScore(bool[,] modules, int size)
    {
        int score = 0;

        // Horizontal and vertical runs
        for (int y = 0; y < size; y++)
        {
            int runLen = 1;
            for (int x = 1; x < size; x++)
            {
                if (modules[y, x] == modules[y, x - 1])
                {
                    runLen++;
                }
                else
                {
                    if (runLen >= 5)
                    {
                        score += runLen - 2;
                    }

                    runLen = 1;
                }
            }

            if (runLen >= 5)
            {
                score += runLen - 2;
            }
        }

        for (int x = 0; x < size; x++)
        {
            int runLen = 1;
            for (int y = 1; y < size; y++)
            {
                if (modules[y, x] == modules[y - 1, x])
                {
                    runLen++;
                }
                else
                {
                    if (runLen >= 5)
                    {
                        score += runLen - 2;
                    }

                    runLen = 1;
                }
            }

            if (runLen >= 5)
            {
                score += runLen - 2;
            }
        }

        // 2x2 uniform blocks
        for (int y = 0; y < size - 1; y++)
        {
            for (int x = 0; x < size - 1; x++)
            {
                bool c = modules[y, x];
                if (c == modules[y, x + 1] && c == modules[y + 1, x] && c == modules[y + 1, x + 1])
                {
                    score += 3;
                }
            }
        }

        // Balance dark vs light
        int darkCount = 0;
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                if (modules[y, x])
                {
                    darkCount++;
                }
            }
        }

        int total = size * size;
        int k = Math.Abs((darkCount * 20) - (total * 10)) / total;
        score += k * 10;

        return score;
    }

    private static void SetFunctionModule(bool[,] modules, bool[,] isFunction, int x, int y, bool dark)
    {
        modules[y, x] = dark;
        isFunction[y, x] = true;
    }

    private static void SetFormatModule(bool[,] modules, bool[,]? isFunction, int x, int y, bool dark)
    {
        modules[y, x] = dark;
        if (isFunction is not null)
        {
            isFunction[y, x] = true;
        }
    }

    private static void AppendBits(List<bool> bits, int val, int len)
    {
        for (int i = len - 1; i >= 0; i--)
        {
            bits.Add(((val >> i) & 1) != 0);
        }
    }

    private static bool GetBit(int val, int index) => ((val >> index) & 1) != 0;
}
