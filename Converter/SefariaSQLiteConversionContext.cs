using Converter.Model.SQLite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using System;
using System.Collections.Generic;
using System.Linq;
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
        public DbSet<Chapter> Chapters { get; set; }
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
            modelBuilder.Entity<Chapter>().HasIndex(c => c.TopicTextId);
            modelBuilder.Entity<Chapter>().HasIndex(c => c.Path);

        }

        private Dictionary<Type, object> _trackedList = new Dictionary<Type, object>();

        public T FindFirstOrDefaultWhere<T>(DbSet<T> target, Func<T, bool> predicate) where T : class
        {
            //var localList = (_trackedList.GetValueOrDefault(typeof(T)) as List<T>) ?? new List<T>();
            ////if (localList == null)
            ////{
            ////    localList = target.Local;
            ////    _trackedList.Add(typeof(T), localList);
            ////}
            //var resultLocal = localList.Where(predicate).FirstOrDefault();
            //if(resultLocal != null) return resultLocal;

            var resultLocal = FindTrackedFirstOrDefaultWhere(target, predicate);
            if(resultLocal != null) return resultLocal;

            var resultDb = target.Where(predicate).FirstOrDefault();
            return resultDb;
        }

        public List<T> FindListWhere<T>(DbSet<T> target, Func<T, bool> predicate) where T : class
        {
            List<T> result = new List<T>();
            var localList = (_trackedList.GetValueOrDefault(typeof(T)) as List<T>) ?? new List<T>();
            var resultLocal = localList.Where(predicate).ToList();
            result = result.Concat(resultLocal).ToList();

            var resultDb = target.Where(predicate).ToList();
            result = result.Concat(resultDb).ToList();
            return result;
        }

        public T FindTrackedFirstOrDefaultWhere<T>(DbSet<T> target, Func<T, bool> predicate) where T : class
        {
            var localList = (_trackedList.GetValueOrDefault(typeof(T)) as List<T>) ?? new List<T>();
            var resultLocal = localList.Where(predicate).FirstOrDefault();
            return resultLocal;
        }

        private List<Type> _typesChanged = new List<Type>();
        public EntityEntry<T> Add<T>(DbSet<T> target, T item) where T : class
        {
            Type type = typeof(T);

            if (!_typesChanged.Contains(type))
            {
                _typesChanged.Add(type);
                _trackedList.Add(type, new List<T>());
            }
            ((List<T>)_trackedList.GetValueOrDefault(type))?.Add(item);
            return target.Add(item);
        }

        //public EntityEntry<T> AddAsync<T>(DbSet<T> target, T item) where T : class
        //{
        //    Type type = typeof(T);

        //    if (!_typesChanged.Contains(type))
        //    {
        //        _typesChanged.Add(type);
        //        _trackedList.Add(type, new List<T>());
        //    }
        //    ((List<T>)_trackedList.GetValueOrDefault(type))?.Add(item);
        //    return target.Add(item);
        //}



        public override int SaveChanges()
        {
            _typesChanged.ForEach(t =>
            {
                _trackedList.Remove(t);
            });
            _typesChanged.RemoveAll(_=>true);
            return base.SaveChanges();
        }
    }
}
