using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace GreenWell.Data
{
    public class GreenWellContext: DbContext
    {
        public DbSet<FileDirectory> FileDirectories { get; set; }

        public GreenWellContext(DbContextOptions<GreenWellContext> options) : base(options)
        { }

        protected override void OnConfiguring(DbContextOptionsBuilder builder)
        { }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfiguration(new FileDirectoryConfiguration());
        }
    }
}
