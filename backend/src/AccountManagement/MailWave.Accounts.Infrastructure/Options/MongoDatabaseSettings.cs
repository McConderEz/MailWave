namespace MailWave.Accounts.Infrastructure.Options;

public class MongoDatabaseSettings
{
    public static string Mongo = nameof(Mongo);
    
    public string ConnectionString { get; set; } = null!;
    public string DatabaseName { get; set; } = null!;

    public string UsersCollectionName { get; set; } = null!;
    public string RefreshSessionsCollectionName { get; set; } = null!;
    public string FriendShipCollectionName { get; set; } = null!;
}