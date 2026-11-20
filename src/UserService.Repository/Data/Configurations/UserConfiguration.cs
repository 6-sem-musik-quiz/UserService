using UserService.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace UserService.Repository.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(u => u.Id);

        // citext: the unique index treats "Alice" and "alice" as the same name.
        builder.Property(u => u.UserName)
            .IsRequired()
            .HasMaxLength(20)
            .HasColumnType("citext");
        builder.HasIndex(u => u.UserName).IsUnique();

        builder.Property(u => u.PasswordHash).IsRequired();

        builder.Property(u => u.Role)
            .IsRequired()
            .HasConversion<int>();
    }
}
