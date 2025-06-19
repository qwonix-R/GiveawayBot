using Polly;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Telegram.Bot.Types;
using Microsoft.Data.Sqlite;


namespace TgBot1
{
    public class PostContext : DbContext
    {
        public DbSet<Post> PostDbSet { get; set; }
        

        public PostContext()
        {

        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = new SqliteConnectionStringBuilder
            { //    /giveawaybot/data/    Для Docker
                DataSource = "/giveawaybot/data/posts.db",
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            }.ToString();

            optionsBuilder.UseSqlite(connectionString);
        }
        
    }
    public class GiveawayContext : DbContext
    {
        public DbSet<GiveawayParticipant> GiveawayParticipants { get; set; }
        public DbSet<Winner> Winners { get; set; }
        public DbSet<Subscriber> Subscribers { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<GiveawayParticipant>()
                .HasKey(p => new { p.UserId, p.GiveawayId }); // Composite key
            modelBuilder.Entity<Winner>()
                .HasKey(p => new { p.UserId, p.GiveawayId }); // Composite key
            modelBuilder.Entity<Subscriber>()
                .HasKey(p => new { p.UserId });
        }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = "/giveawaybot/data/giveaway.db",
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            }.ToString();

            optionsBuilder.UseSqlite(connectionString);
        }
    }
    public class AdminContext : DbContext
    {
        public DbSet<Admin> Admins { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = "/giveawaybot/data/admins.db",
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            }.ToString();

            optionsBuilder.UseSqlite(connectionString);
        }
    }
    public class BroadContext : DbContext
    {
        public DbSet<Broadcast> Broadcasts { get; set; }
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var connectionString = new SqliteConnectionStringBuilder
            {
                DataSource = "/giveawaybot/data/broads.db",
                Mode = SqliteOpenMode.ReadWriteCreate,
                Cache = SqliteCacheMode.Shared
            }.ToString();

            optionsBuilder.UseSqlite(connectionString);
        }
    }
    public class Broadcast
    {
        public int Id { get; set; }
        public string BroadText { get; set; }
        public string? BroadPhotoPath { get; set; }
        public string BroadTime { get; set; }
        public string TimeStamp {  get; set; }
        public int IsPublished { get; set; }
        public int IsCancelled { get; set; }
    }
    public class Admin
    {
        public int Id { get; set; } 
        public long UserId { get; set; }
        public string? UserName { get; set; }
    }

    public class Post
    {
        public int PostId { get; set; }
        public long ChatId { get; set; }
        public string Text { get; set; }
        public string? PhotoPath { get; set; }
        public int Winners { get; set; }
        //public string Prizes { get; set; }
        public string UploadTime { get; set; }
        public string PollTime { get; set; }
        public string Channel { get; set; }
        public string TimeStamp { get; set; }
        public int IsPublished { get; set; }
        public int IsEnded { get; set; }
        public int IsCancelled {  get; set; }

    }
    public class GiveawayParticipant
    {
        public int GiveawayId { get; set; }
        public long UserId { get; set; } // ID пользователя Telegram
        public string? Username { get; set; } // @username пользователя

    }
    public class Subscriber
    {
        public long UserId { get; set; } // ID пользователя Telegram
        public string? Username { get; set; } // @username пользователя

    }
    public class Winner
    {
        public int GiveawayId { get; set; }
        public long UserId { get; set; }
        public string? Username { get; set; }
    }


}
