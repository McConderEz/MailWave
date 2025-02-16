﻿using MongoDB.Bson.Serialization.Attributes;

namespace MailWave.Accounts.Domain.Models;

public class Friendship
{
    [BsonId]
    public string Id { get; set; }
    [BsonRequired]
    public string PrivateKey { get; set; }
    [BsonRequired]
    public string PublicKey { get; set; }
    [BsonRequired]
    public string FirstUserId { get; set; } = String.Empty;
    [BsonRequired]
    public string FirstUserEmail { get; set; } = string.Empty;
    [BsonRequired]
    public string SecondUserId { get; set; } = String.Empty;
    [BsonRequired]
    public string SecondUserEmail { get; set; } = string.Empty;
    [BsonRequired]
    public bool IsAccepted { get; set; } = false;
}