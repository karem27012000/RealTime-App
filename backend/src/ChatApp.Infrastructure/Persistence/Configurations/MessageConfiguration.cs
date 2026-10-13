using ChatApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatApp.Infrastructure.Persistence.Configurations;

public sealed class MessageConfiguration : IEntityTypeConfiguration<Message>
{
    public void Configure(EntityTypeBuilder<Message> builder)
    {
        builder.ToTable("Messages");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).ValueGeneratedNever();

        builder.Property(m => m.Content)
            .HasMaxLength(Message.ContentMaxLength)
            .IsRequired();

        // Deleting a room deletes its messages.
        builder.HasOne<ChatRoom>()
            .WithMany()
            .HasForeignKey(m => m.RoomId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Sender)
            .WithMany()
            .HasForeignKey(m => m.SenderId)
            .OnDelete(DeleteBehavior.Restrict);

        // Loading a room's messages in time order is the most common query.
        builder.HasIndex(m => new { m.RoomId, m.CreatedAt });
    }
}
