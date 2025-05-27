using EIUBetApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EIUBetApp.Data
{
    public class EIUBetAppContext : DbContext
    {
        public EIUBetAppContext(DbContextOptions<EIUBetAppContext> options)
            : base(options)
        {
        }

        public DbSet<User> User { get; set; }
        public DbSet<Admin> Admin { get; set; }
        public DbSet<Player> Player { get; set; }
        public DbSet<Room> Room { get; set; }
        public DbSet<ManageRoom> ManageRoom { get; set; }
        public DbSet<Game> Game { get; set; }
        public DbSet<Logs> Logs { get; set; }
        public DbSet<Role> Role { get; set; }
        public DbSet<UserRole> UserRole { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Player - User (One-to-One)
            modelBuilder.Entity<Player>()
                .HasOne(p => p.User)
                .WithOne(u => u.Player)
                .HasForeignKey<Player>(p => p.UserId)
                .IsRequired();

            // Admin - User (One-to-One)
            modelBuilder.Entity<Admin>()
                .HasOne(a => a.User)
                .WithOne(u => u.Admin)
                .HasForeignKey<Admin>(a => a.UserId)
                .IsRequired();

            // ManageRoom - Room (Many-to-One)
            modelBuilder.Entity<ManageRoom>()
                .HasOne(mr => mr.Room)
                .WithMany(r => r.ManageRooms)
                .HasForeignKey(mr => mr.RoomId);

            // ManageRoom - Player (Many-to-One)
            modelBuilder.Entity<ManageRoom>()
                .HasOne(mr => mr.Player)
                .WithMany(p => p.ManageRooms)
                .HasForeignKey(mr => mr.PlayerId);

            // Room - Game (Many-to-One)
            modelBuilder.Entity<Room>()
                .HasOne(r => r.Game)
                .WithMany(g => g.Rooms)
                .HasForeignKey(r => r.GameId)
                .OnDelete(DeleteBehavior.SetNull); // if Game is deleted

            // Logs - Game (Many-to-One)
            modelBuilder.Entity<Logs>()
                .HasOne(l => l.Game)
                .WithMany(g => g.Logs)
                .HasForeignKey(l => l.GameId)
                .OnDelete(DeleteBehavior.SetNull);

            // Logs - Room (Many-to-One)
            modelBuilder.Entity<Logs>()
                .HasOne(l => l.Room)
                .WithMany(r => r.Logs)
                .HasForeignKey(l => l.RoomId)
                .OnDelete(DeleteBehavior.SetNull);

            // Logs - Player (Many-to-One)
            modelBuilder.Entity<Logs>()
                .HasOne(l => l.Player)
                .WithMany()
                .HasForeignKey(l => l.PlayerId)
                .OnDelete(DeleteBehavior.Cascade);

            // Many-to-Many: User <-> Role via UserRole
            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.User)
                .WithMany(u => u.UserRoles)
                .HasForeignKey(ur => ur.UserId);

            modelBuilder.Entity<UserRole>()
                .HasOne(ur => ur.Role)
                .WithMany(r => r.UserRoles)
                .HasForeignKey(ur => ur.RoleId);
        }
    }
}
