using EIUBetApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EIUBetApp.Data
{
    public class EIUBetAppContext : DbContext
    {
        public EIUBetAppContext(DbContextOptions<EIUBetAppContext> options)
            : base(options) { }

        public DbSet<User> User { get; set; }
        public DbSet<Admin> Admin { get; set; }
        public DbSet<Player> Player { get; set; }
        public DbSet<Room> Room { get; set; }
        public DbSet<ManageRoom> ManageRoom { get; set; }
        public DbSet<Game> Game { get; set; }
        public DbSet<Logs> Logs { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure relationships as you already did
            modelBuilder.Entity<Player>()
                 .HasOne(p => p.User)
                 .WithOne(u => u.Player)
                 .HasForeignKey<Player>(p => p.UserId)
                 .IsRequired();

            modelBuilder.Entity<Admin>()
                .HasOne(a => a.User)
                .WithOne(u => u.Admin)
                .HasForeignKey<Admin>(a => a.UserId)
                .IsRequired();

            modelBuilder.Entity<ManageRoom>()
                .HasOne(mr => mr.Room)
                .WithMany(r => r.ManageRooms)
                .HasForeignKey(mr => mr.RoomId);

            modelBuilder.Entity<ManageRoom>()
                .HasOne(mr => mr.Player)
                .WithMany(p => p.ManageRooms)
                .HasForeignKey(mr => mr.PlayerId);

            modelBuilder.Entity<Room>()
                .HasOne(r => r.Game)
                .WithMany(g => g.Rooms)
                .HasForeignKey(r => r.GameId);

            modelBuilder.Entity<Logs>()
                .HasOne(l => l.Game)
                .WithMany(g => g.Logs)
                .HasForeignKey(l => l.GameId);

            modelBuilder.Entity<Logs>()
                .HasOne(l => l.Room)
                .WithMany(r => r.Logs)
                .HasForeignKey(l => l.RoomId);

            modelBuilder.Entity<Logs>()
                .HasOne(l => l.Player)
                .WithMany()
                .HasForeignKey(l => l.PlayerId);
        }
    }
}
