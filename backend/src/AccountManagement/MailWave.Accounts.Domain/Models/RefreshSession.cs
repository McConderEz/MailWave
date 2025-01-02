﻿using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MailWave.Accounts.Domain.Models;

public class RefreshSession
{
    [BsonId]
    public string Id { get; init; }
    [BsonRequired]
    public Guid UserId { get; init; }
    [BsonRequired]
    public User User { get; init; } = default!;
    [BsonRequired]
    public Guid Jti { get; init; }
    [BsonRequired]
    public Guid RefreshToken { get; init; }
    [BsonRequired]
    public DateTime ExpiresIn { get; init; }
    [BsonRequired]
    public DateTime CreatedAt { get; init; }
    
}