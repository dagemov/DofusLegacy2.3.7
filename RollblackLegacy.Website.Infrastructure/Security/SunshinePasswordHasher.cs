using RollblackLegacy.Website.Application.Abstractions;
using System.Security.Cryptography;
using System.Text;

namespace RollblackLegacy.Website.Infrastructure.Security;

public sealed class SunshinePasswordHasher : ISunshinePasswordHasher
{
    public string HashForStorage(string plainPassword)
    {
        using MD5 md5 = MD5.Create();
        byte[] buffer = Encoding.ASCII.GetBytes(plainPassword ?? string.Empty);
        byte[] hash = md5.ComputeHash(buffer);

        var builder = new StringBuilder(hash.Length * 2);
        foreach (byte value in hash)
        {
            builder.Append(value.ToString("x2"));
        }

        return builder.ToString();
    }
}
