using System.Security.Cryptography;
using System.Text;

namespace server.Utils;

public static class TotpUtility
{
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public static string GenerateBase32Secret(int numBytes = 20)
    {
        var data = RandomNumberGenerator.GetBytes(numBytes);
        return ToBase32(data);
    }

    public static bool ValidateCode(string base32Secret, string? code, int allowedDriftSteps = 1, int periodSeconds = 30, int digits = 6)
    {
        if (string.IsNullOrWhiteSpace(base32Secret) || string.IsNullOrWhiteSpace(code))
            return false;

        var normalizedCode = new string(code.Where(char.IsDigit).ToArray());
        if (normalizedCode.Length != digits)
            return false;

        var secret = FromBase32(base32Secret);
        var unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        var timeStep = unixTime / periodSeconds;

        for (long i = -allowedDriftSteps; i <= allowedDriftSteps; i++)
        {
            var expected = ComputeTotp(secret, timeStep + i, digits);
            if (CryptographicOperations.FixedTimeEquals(
                    Encoding.ASCII.GetBytes(expected),
                    Encoding.ASCII.GetBytes(normalizedCode)))
            {
                return true;
            }
        }

        return false;
    }

    public static string BuildOtpAuthUri(string issuer, string accountName, string base32Secret)
    {
        var escapedIssuer = Uri.EscapeDataString(issuer);
        var escapedAccount = Uri.EscapeDataString(accountName);
        return $"otpauth://totp/{escapedIssuer}:{escapedAccount}?secret={base32Secret}&issuer={escapedIssuer}&algorithm=SHA1&digits=6&period=30";
    }

    private static string ComputeTotp(byte[] secret, long counter, int digits)
    {
        Span<byte> counterBytes = stackalloc byte[8];
        for (int i = 7; i >= 0; i--)
        {
            counterBytes[i] = (byte)(counter & 0xFF);
            counter >>= 8;
        }

        using var hmac = new HMACSHA1(secret);
        var hash = hmac.ComputeHash(counterBytes.ToArray());
        var offset = hash[^1] & 0x0F;
        var binary =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);

        var otp = binary % (int)Math.Pow(10, digits);
        return otp.ToString(new string('0', digits));
    }

    private static string ToBase32(byte[] data)
    {
        if (data.Length == 0) return string.Empty;

        var outputLength = (int)Math.Ceiling(data.Length / 5d) * 8;
        var output = new StringBuilder(outputLength);

        int bitBuffer = 0;
        int bitBufferLength = 0;

        foreach (var b in data)
        {
            bitBuffer = (bitBuffer << 8) | b;
            bitBufferLength += 8;

            while (bitBufferLength >= 5)
            {
                var index = (bitBuffer >> (bitBufferLength - 5)) & 0x1F;
                bitBufferLength -= 5;
                output.Append(Base32Alphabet[index]);
            }
        }

        if (bitBufferLength > 0)
        {
            var index = (bitBuffer << (5 - bitBufferLength)) & 0x1F;
            output.Append(Base32Alphabet[index]);
        }

        return output.ToString();
    }

    private static byte[] FromBase32(string input)
    {
        var cleaned = input.Trim().TrimEnd('=').ToUpperInvariant();
        var output = new List<byte>();
        int bitBuffer = 0;
        int bitBufferLength = 0;

        foreach (var c in cleaned)
        {
            var value = Base32Alphabet.IndexOf(c);
            if (value < 0) continue;

            bitBuffer = (bitBuffer << 5) | value;
            bitBufferLength += 5;

            if (bitBufferLength >= 8)
            {
                bitBufferLength -= 8;
                output.Add((byte)((bitBuffer >> bitBufferLength) & 0xFF));
            }
        }

        return output.ToArray();
    }
}
