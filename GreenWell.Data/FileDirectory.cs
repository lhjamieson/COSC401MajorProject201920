using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using System.Collections.Generic;
using System.Text;

namespace GreenWell.Data
{
    public class FileDirectory
    {
        public int Id { get; set; }
        public string path { get; set; }

    }
    public class FileDirectoryConfiguration : IEntityTypeConfiguration<FileDirectory>
    {
        public void Configure(EntityTypeBuilder<FileDirectory> builder)
        {
            builder.ToTable("file_directories");

            builder.HasKey(p => p.Id);
            builder.Property(p => p.Id)
                .HasAnnotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn);

            builder.HasIndex(p => p.path).IsUnique();
        }
    }
}
