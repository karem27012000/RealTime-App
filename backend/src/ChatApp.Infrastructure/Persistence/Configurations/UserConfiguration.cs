using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        // Ids are created in the domain (Guid.NewGuid()), not by the database.
        builder.Property(u => u.Id).ValueGeneratedNever();

        // NOCASE makes "Karem" and "karem" the same nickname.
        builder.Property(u => u.Nickname)
            .HasMaxLength(User.NicknameMaxLength)
            .UseCollation("NOCASE")
            .IsRequired();

        builder.HasIndex(u => u.Nickname).IsUnique();
    }
}
