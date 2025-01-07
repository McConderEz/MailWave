using System.Text.Json;
using MailWave.Mail.Domain.Entities;
using MailWave.SharedKernel.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace MailWave.Mail.Infrastructure.Configurations;

public class StringListJsonConverter : ValueConverter<List<string>, string>
{
    public StringListJsonConverter() : base(
        v => JsonSerializer.Serialize(v, (JsonSerializerOptions)null!),
        v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions)null!)!)
    {
    }
}

public class LetterConfiguration: IEntityTypeConfiguration<Letter>
{
    public void Configure(EntityTypeBuilder<Letter> builder)
    {
        builder.ToTable("letters");

        builder.HasKey(l => new { l.Id, l.Folder });
        
        builder.Property(l => l.From)
            .HasMaxLength(Constraints.MAX_VALUE_LENGTH)
            .HasColumnName("from");

        builder.Property(l => l.To)
            .HasColumnName("to")
            .HasColumnType("jsonb")
            .HasConversion(new StringListJsonConverter());

        builder.Property(l => l.Body)
            .HasColumnName("body");
        
        builder.Property(l => l.Subject)
            .HasColumnName("subject");
        
        builder.Property(l => l.Date)
            .HasColumnName("date");

        builder.Property(l => l.Folder)
            .HasColumnName("folder");
        
        builder.Property(l => l.IsCrypted)
            .HasColumnName("is_crypted");
        
        builder.Property(l => l.IsSigned)
            .HasColumnName("is_signed");
    }
}