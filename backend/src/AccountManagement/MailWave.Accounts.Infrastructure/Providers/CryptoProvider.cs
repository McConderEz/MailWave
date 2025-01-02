using BCrypt.Net;
using MailWave.Accounts.Application.Providers;

namespace MailWave.Accounts.Infrastructure.Providers;

public class CryptoProvider: ICryptProvider
{
    public string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.EnhancedHashPassword(password, HashType.SHA512);
    }

    public bool Verify(string hashedPassword, string password)
    {
        return BCrypt.Net.BCrypt.EnhancedVerify(password, hashedPassword, HashType.SHA512);
    }
}