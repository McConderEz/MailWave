using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MailWave.Accounts.Domain.Models;

public class User
{
    [BsonId]
    public string Id { get; set; }
    [BsonRequired]
    public string Email { get; set; } = string.Empty;
    [BsonRequired]
    public string Password { get; set; } = string.Empty;
    [BsonElement]
    public List<Friendship> Friendships { get; set; } = [];
}