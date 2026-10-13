using ChatBot.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ChatBot.Infrastructure.Persistence.Configurations;

public sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");

        builder.HasKey(c => c.Id);

        // Ids are created in the domain (Guid.NewGuid()), not by the database.
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Title)
            .HasMaxLength(Conversation.TitleMaxLength)
            .IsRequired();

        builder.HasIndex(c => c.UpdatedAt);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Messages is exposed as read-only; EF Core writes to the private _messages field.
        builder.Navigation(c => c.Messages).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
