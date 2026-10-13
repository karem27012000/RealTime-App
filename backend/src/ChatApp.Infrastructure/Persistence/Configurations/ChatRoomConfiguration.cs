using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Infrastructure.Persistence.Configurations;

public sealed class ChatRoomConfiguration : IEntityTypeConfiguration<ChatRoom>
{
    public void Configure(EntityTypeBuilder<ChatRoom> builder)
    {
        builder.ToTable("Rooms");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.Name).HasMaxLength(ChatRoom.NameMaxLength);

        // Store the enum as readable text ("Private" / "Group").
        builder.Property(r => r.Type).HasConversion<string>().HasMaxLength(20);

        // Only one private chat per pair of users.
        builder.Property(r => r.PrivateKey).HasMaxLength(80);
        builder.HasIndex(r => r.PrivateKey).IsUnique();

        builder.HasIndex(r => r.UpdatedAt);

        builder.HasMany(r => r.Members)
            .WithOne()
            .HasForeignKey(m => m.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        // Members is a read-only collection; EF Core writes to the private _members field.
        builder.Navigation(r => r.Members).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
