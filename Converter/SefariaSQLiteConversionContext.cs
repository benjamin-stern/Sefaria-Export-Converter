using Converter.Model.SQLite;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace Converter
{
    class SefariaSQLiteConversionContext:DbContext
    {
        public SefariaSQLiteConversionContext(DbContextOptions<SefariaSQLiteConversionContext> options)
            : base(options) { }
        public DbSet<Converter.Model.SQLite.Version> Version { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<LabelGroup> LabelGroups { get; set; }
        public DbSet<Label> Labels { get; set; }
        public DbSet<Text> Texts { get; set; }
        public DbSet<Topic> Topics { get; set; }
        public DbSet<LinkItem> Links { get; set; }
        public DbSet<LinkGroup> LinkGroups { get; set; }
        public DbSet<LinkLanguage> LinkLanguages { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
            => options.UseSqlite("Data Source=data.db");

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            //modelBuilder.Entity<Converter.Model.SQLite.Version>().HasNoKey();
            base.OnModelCreating(modelBuilder);

            //modelBuilder.Entity<Text>().HasKey(t=>t.Id);
        }
    }
}
