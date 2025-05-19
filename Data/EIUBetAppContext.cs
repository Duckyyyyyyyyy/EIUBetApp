using EIUBetApp.Models;
using Microsoft.EntityFrameworkCore;

namespace EIUBetApp.Data
{
    public class EIUBetAppContext : DbContext
    {
        public EIUBetAppContext(DbContextOptions<EIUBetAppContext> options) : base(options) { }

        public DbSet<Admin> Admin { get; set; }
        public DbSet<Logs> Logs { get; set; }
        public DbSet<Player> Player { get; set; }
        public DbSet<Room> Room { get; set; }
        public DbSet<User> User { get; set; }
        public DbSet<RoomManagement> RoomManagement { get; set; }

    }
}
