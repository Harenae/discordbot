using Microsoft.EntityFrameworkCore;
using SQLitePCL;
using DiscordBot_G.Logic.Objects;

namespace DiscordBot_G.Core
{
    public class SqliteDataBase : DbContext
    {
        /// <summary>
        /// Orders table
        /// </summary>
        public DbSet<Order> Orders { get; set; }
        /// <summary>
        /// Servers table
        /// </summary>
        public DbSet<Server> Servers { get; set; }
        /// <summary>
        /// Isers table
        /// </summary>
        public DbSet<User> Users { get; set; }
        /// <summary>
        /// Links table
        /// </summary>
        public DbSet<Link> Links { get; set; }
        /// <summary>
        /// Main costructor. Trying connect to data base; if failed trying to create; otherwise throw exception
        /// </summary>
        public SqliteDataBase() => Database.EnsureCreated();
        /// <summary>
        /// Initialize build
        /// </summary>
        /// <param name="optionsBuilder"></param>
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            /*optionsBuilder.UseSqlServer(
            $"Server={server};User Id={userID};Password={password};Database={name}");*/

            optionsBuilder.UseSqlite($"Data Source=local.db");
            Batteries.Init();
        }
    }
}
