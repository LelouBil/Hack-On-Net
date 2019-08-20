using HackLinks_Server.Computers;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using HackLinks_Server.Computers.DataObjects;
using HackLinks_Server.Computers.Files;
using HackLinks_Server.Files;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace HackLinks_Server.Database {
    public class DatabaseLink : DbContext {
        private const string dbpath = ".\\database.db";

        public DbSet<Binary> Binaries { get; set; }
        public DbSet<Node> Computers { get; set; }
        public DbSet<FileSystem> FileSystems { get; set; }
        public DbSet<ServerAccount> ServerAccounts { get; set; }


        public DatabaseLink(ConfigUtil.ConfigData config) : base(GetConnectionString(config)) { }

        private static DbContextOptions<DatabaseLink> GetConnectionString(ConfigUtil.ConfigData config) {
            DbContextOptionsBuilder<DatabaseLink> options = new DbContextOptionsBuilder<DatabaseLink>();
            DbConnectionStringBuilder connectionStringBuilder;
            if (config.Sqlite) {
                
                var sqlitecn = new SqliteConnectionStringBuilder();
                
                sqlitecn.DataSource = dbpath;
                options.UseSqlite(sqlitecn.ToString());
            }
            else {
                var sqlcn = new SqlConnectionStringBuilder();
                sqlcn.DataSource = config.MySQLServer;
                sqlcn.UserID = config.UserID;
                sqlcn.Password = config.Password;
                options.UseMySql(sqlcn.ToString());
            }
            return options.Options;
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder) {
            modelBuilder.Entity<ServerAccount>()
                .HasOne(n => n.homeComputer)
                .WithOne(n => n.owner)
                .HasForeignKey<Node>(n => n.OwnerId);

            modelBuilder.Entity<ServerAccount>()
                .Property(l => l.netmap)
                .HasConversion(l => ServerAccount.NetMapNode.maptoList(l), l => ServerAccount.NetMapNode.ListToMap(l));
            var conv = new ValueConverter<List<Permissions>,string>(
                p => string.Join(",",p.ToArray()),
                s => s.Split(',').Select(e => (Permissions)Enum.Parse(typeof(Permissions),e,true)).ToList()
            );
            modelBuilder.Entity<ServerAccount>()
                .Property(f => f.permissions)
                .HasConversion(conv);
            modelBuilder.Entity<Node>()
                .HasOne(f => f.fileSystem);
            modelBuilder.Entity<ServerAccount>().HasIndex(m => m.mailaddress).IsUnique();
//            modelBuilder.Entity<FileSystem>().HasOne(f => f.RootFile).WithOne(f => f.FileSystem)
//                .HasForeignKey<FileSystem>(f => f.RootFileId).IsRequired(false);
            modelBuilder.Entity<File>().HasOne(f => f.FileSystem).WithOne().HasForeignKey<File>(f => f.FilesystemId);
            modelBuilder.Entity<File>().HasOne(f => f.Parent).WithOne();
            
            modelBuilder.Entity<File>()
                .HasIndex(m => new {m.Name,m.ParentId,m.FilesystemId}).IsUnique();
        }


        public bool TryLogin(GameClient client, string tempUsername, string tempPass, out ServerAccount homeId)
        {
            homeId = null;
            ServerAccount acc = ServerAccounts.Find(tempUsername);
            if (acc == null || !acc.password.Equals(tempPass)) return false;
            homeId = acc;
            return true;
        }
        

        public bool SetUserBanStatus(string ac, int banExpiry, bool unban, bool permBan) {
            ServerAccount acc = ServerAccounts.Find(ac);
            if (acc == null) return false;
            acc.SetUserBanStatus(!unban,permBan,banExpiry);
            return true;
        }

        public bool CheckUserBanStatus(ServerAccount user, out int banExpiry) {
            return user.IsBanned(out banExpiry);
        }
        

        public void AddUserNode(ServerAccount acc, Node n,string pos) {
            acc.netmap.Add(new ServerAccount.NetMapNode(n.ip,pos));
        }
        

        public static IEnumerable<T> Traverse<T>(IEnumerable<T> items,
        Func<T, IEnumerable<T>> childSelector)
        {
            var stack = new Stack<T>(items);
            while (stack.Any())
            {
                var next = stack.Pop();
                yield return next;
                foreach (var child in childSelector(next))
                    stack.Push(child);
            }
        }
        

        public void RebuildDatabase() {
            Database.EnsureDeleted();
            Database.Migrate();
            
            Computers.AddRange(Node.Defaults);
            SaveChanges();
            Binaries.AddRange(Binary.getBinaries());
            ServerAccounts.AddRange(ServerAccount.Defaults);
            SaveChanges();


        }

        public DbSet<ServerAccount> GetUsersInDatabase() {
            return ServerAccounts;
        }
    }
    
}
