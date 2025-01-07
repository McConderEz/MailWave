using System.Text.Json;
using MailWave.Mail.Domain.Entities;
using MailWave.SharedKernel.Shared;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MailWave.Mail.Infrastructure.Configurations;

public class LetterConfiguration: IEntityTypeConfiguration<Letter>
{
    public void Configure(EntityTypeBuilder<Letter> builder)
    {
        builder.ToTable("letters");

        builder.Property(l => l.From)
            .HasMaxLength(Constraints.MAX_VALUE_LENGTH)
            .HasColumnName("from");

        builder.Property(l => l.To)
            .HasColumnName("to")
            .HasColumnType("jsonb");

        builder.Property(l => l.Body)
            .HasColumnName("body");
        
        builder.Property(l => l.Subject)
            .HasColumnName("subject");
        
        builder.Property(l => l.Date)
            .HasColumnName("date");
        
        builder.Property(l => l.IsCrypted)
            .HasColumnName("is_crypted");
        
        builder.Property(l => l.IsSigned)
            .HasColumnName("is_signed");
    }
}