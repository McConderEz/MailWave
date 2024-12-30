namespace MailWave.Accounts.Infrastructure.Options;

public class MongoDatabaseSettings
{
    public static readonly string Mongo = nameof(MongoDatabaseSettings);
    
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;

    public string UsersCollectionName { get; set; } = null!;
    public string RefreshSessionsCollectionName { get; set; } = null!;
}