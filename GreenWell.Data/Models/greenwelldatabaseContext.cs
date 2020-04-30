using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;

namespace Greenwell.Data.Models
{
    public partial class greenwelldatabaseContext : DbContext
    {
        public greenwelldatabaseContext()
        {
        }

        public greenwelldatabaseContext(DbContextOptions<greenwelldatabaseContext> options)
            : base(options)
        {
        }
        public virtual DbSet<Files> Files { get; set; }
        public virtual DbSet<Tagmap> Tagmap { get; set; }
        public virtual DbSet<Tags> Tags { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
#warning To protect potentially sensitive information in your connection string, you should move it out of source code. See http://go.microsoft.com/fwlink/?LinkId=723263 for guidance on storing connection strings.
                optionsBuilder.UseMySQL("server=localhost;port=3306;database=greenwelldatabase;user=root;password=3000;database=greenwelldatabase");
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Files>(entity =>
            {
                entity.HasKey(e => e.FileId);

                entity.ToTable("files", "greenwelldatabase");


                entity.Property(e => e.FileId)
                    .HasColumnName("fileID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.ExtType)
                    .HasColumnName("extType")
                    .HasMaxLength(5)
                    .IsUnicode(false);

                entity.Property(e => e.FileSize).HasColumnName("fileSize");

                entity.Property(e => e.Filename)
                    .HasColumnName("filename")
                    .IsUnicode(false);

                entity.Property(e => e.FullPath)
                    .HasColumnName("fullPath")
                    .HasMaxLength(100)
                    .IsUnicode(false);
                
                entity.Property(e => e.Author)
                    .HasColumnName("author")
                    .HasMaxLength(100)
                    .IsUnicode(false);


                entity.Property(e => e.UploadDate)
                    .HasColumnName("uploadDate")
                    .HasColumnType("date");

                entity.Property(e => e.AdminOnly)
                    .HasColumnName("adminOnly")
                    .HasColumnType("boolean");

                entity.Property(e => e.Approved)
                    .HasColumnName("approved")
                    .HasColumnType("boolean");



            });

            modelBuilder.Entity<Tagmap>(entity =>
            {
                entity.ToTable("tagmap", "greenwelldatabase");

                entity.HasIndex(e => e.FileId)
                    .HasName("fileId_idx");

                entity.HasIndex(e => e.TagId)
                    .HasName("tagId_idx");

                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .HasColumnType("int(11)");

                entity.Property(e => e.FileId)
                    .HasColumnName("fileId")
                    .HasColumnType("int(11)");

                entity.Property(e => e.TagId)
                    .HasColumnName("tagId")
                    .HasColumnType("int(11)");

                entity.HasOne(d => d.File)
                    .WithMany(p => p.Tagmap)
                    .HasForeignKey(d => d.FileId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("fileId");

                entity.HasOne(d => d.Tag)
                    .WithMany(p => p.Tagmap)
                    .HasForeignKey(d => d.TagId)
                    .OnDelete(DeleteBehavior.ClientSetNull)
                    .HasConstraintName("tagId");
            });

            modelBuilder.Entity<Tags>(entity =>
            {
                entity.HasKey(e => e.TagId);

                entity.ToTable("tags", "greenwelldatabase");

                entity.Property(e => e.TagId)
                    .HasColumnName("tagID")
                    .HasColumnType("int(11)");

                entity.Property(e => e.TagName)
                    .HasColumnName("tagName")
                    .HasMaxLength(30)
                    .IsUnicode(false);
            });
        }
    }
}
