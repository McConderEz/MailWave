namespace MailWave.Accounts.Application.Providers;

public interface ICryptProvider
{
    public string HashPassword(string password);
    public bool Verify(string hashedPassword, string password);
}