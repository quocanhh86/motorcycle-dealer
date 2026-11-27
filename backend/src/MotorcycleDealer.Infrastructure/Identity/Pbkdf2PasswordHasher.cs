using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using MotorcycleDealer.Application.Common.Interfaces;

namespace MotorcycleDealer.Infrastructure.Identity;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const string AlgorithmPrefix = "PBKDF2-SHA512";
    private const int DefaultIterations = 210_000;
    private const int MinIterations = 10_000;
    private const int MaxIterations = 1_000_000;
    private const int SaltByteLength = 16;
    private const int SubkeyByteLength = 64;
    private const int MaxStackUtf8ByteLength = 512;

    private static readonly byte[] DummySalt = new byte[SaltByteLength];
    private static readonly byte[] DummySubkey = new byte[SubkeyByteLength];

    public string HashPassword(string password)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        Span<byte> salt = stackalloc byte[SaltByteLength];
        Span<byte> subkey = stackalloc byte[SubkeyByteLength];

        try
        {
            RandomNumberGenerator.Fill(salt);
            DeriveSubkey(password.AsSpan(), salt, subkey, DefaultIterations);

            return string.Create(
                CultureInfo.InvariantCulture,
                $"{AlgorithmPrefix}${DefaultIterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(subkey)}");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(subkey);
        }
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        if (string.IsNullOrEmpty(password) || string.IsNullOrWhiteSpace(passwordHash))
        {
            PerformConstantTimeDummyWork(password ?? string.Empty);
            return false;
        }

        var segments = passwordHash.Split('$');
        if (segments.Length != 4 ||
            !string.Equals(segments[0], AlgorithmPrefix, StringComparison.Ordinal) ||
            !int.TryParse(segments[1], NumberStyles.None, CultureInfo.InvariantCulture, out var iterations) ||
            iterations is < MinIterations or > MaxIterations)
        {
            PerformConstantTimeDummyWork(password);
            return false;
        }

        byte[]? salt = null;
        byte[]? expectedSubkey = null;
        try
        {
            salt = Convert.FromBase64String(segments[2]);
            expectedSubkey = Convert.FromBase64String(segments[3]);
        }
        catch (FormatException)
        {
            if (salt is not null)
            {
                CryptographicOperations.ZeroMemory(salt);
            }

            PerformConstantTimeDummyWork(password);
            return false;
        }

        try
        {
            if (salt.Length != SaltByteLength || expectedSubkey.Length != SubkeyByteLength)
            {
                PerformConstantTimeDummyWork(password);
                return false;
            }

            Span<byte> actualSubkey = stackalloc byte[SubkeyByteLength];
            try
            {
                DeriveSubkey(password.AsSpan(), salt, actualSubkey, iterations);
                return CryptographicOperations.FixedTimeEquals(actualSubkey, expectedSubkey);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(actualSubkey);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(expectedSubkey);
        }
    }

    private static void PerformConstantTimeDummyWork(string password)
    {
        Span<byte> dummyOutput = stackalloc byte[SubkeyByteLength];
        try
        {
            DeriveSubkey(password.AsSpan(), DummySalt, dummyOutput, DefaultIterations);
            _ = CryptographicOperations.FixedTimeEquals(dummyOutput, DummySubkey);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(dummyOutput);
        }
    }

    private static void DeriveSubkey(
        ReadOnlySpan<char> password,
        ReadOnlySpan<byte> salt,
        Span<byte> destination,
        int iterations)
    {
        var utf8ByteCount = Encoding.UTF8.GetByteCount(password);
        if (utf8ByteCount <= MaxStackUtf8ByteLength)
        {
            Span<byte> utf8StackBuffer = stackalloc byte[MaxStackUtf8ByteLength];
            var bytesWritten = Encoding.UTF8.GetBytes(password, utf8StackBuffer);
            var passwordUtf8Slice = utf8StackBuffer[..bytesWritten];
            try
            {
                Rfc2898DeriveBytes.Pbkdf2(
                    passwordUtf8Slice,
                    salt,
                    destination,
                    iterations,
                    HashAlgorithmName.SHA512);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(passwordUtf8Slice);
            }

            return;
        }

        var heapBuffer = new byte[utf8ByteCount];
        try
        {
            var bytesWritten = Encoding.UTF8.GetBytes(password, heapBuffer);
            Rfc2898DeriveBytes.Pbkdf2(
                heapBuffer.AsSpan(0, bytesWritten),
                salt,
                destination,
                iterations,
                HashAlgorithmName.SHA512);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(heapBuffer);
        }
    }
}
