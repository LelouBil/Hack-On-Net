using HackLinks_Server.Computers;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Data.SQLite.EF6;
using System.Data.SQLite.EF6.Migrations;
using System.IO;
using System.Linq;
using System.Reflection;
using HackLinks_Server.Computers.DataObjects;
using HackLinks_Server.Computers.Files;
using File = HackLinks_Server.Files.File;

namespace HackLinks_Server.Database {
    public class DatabaseLink : DbContext
    {
        public static bool sqlite;
        
        public static string dbpath = ".\\database.db";

        public DbSet<Binary> Binaries { get; set; }
        public DbSet<Node> Computers { get; set; }
        public DbSet<FileSystem> FileSystems { get; set; }
        public DbSet<File> files { get; set; }
        public DbSet<ServerAccount> ServerAccounts { get; set; }


        public DatabaseLink(ConfigUtil.ConfigData config) : base(GetConnectionString(config),true) { }

        public DatabaseLink() : base(GetConnectionString(null),true) { }


        private static DbConnection GetConnectionString(ConfigUtil.ConfigData config)
        {
            DbConnection connection;
            if (config == null || config.Sqlite)
            {

                var sqlitecn = new SQLiteConnectionStringBuilder();

                sqlitecn.DataSource = dbpath;
                sqlitecn.FailIfMissing = false;
                //sqlitecn.FailIfMissing = false;
                connection = new SQLiteConnection(){ConnectionString = sqlitecn.ConnectionString};
            }
            else
            {
                var sqlcn = new SqlConnectionStringBuilder();
                sqlcn.DataSource = config.MySQLServer;
                sqlcn.UserID = config.UserID;
                sqlcn.Password = config.Password;
                connection = new SqlConnection(){ConnectionString = sqlcn.ConnectionString};
            }

            return connection;
        }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Node>().HasRequired(n => n.fileSystem).WithMany();
            modelBuilder.Entity<ServerAccount>().HasOptional(s => s.homeComputer).WithOptionalDependent();
            modelBuilder.Entity<ServerAccount>().HasMany(s => s.netmap).WithOptional();
            modelBuilder.Entity<Node>().HasOptional(n => n.owner).WithMany(s => s.Nodes).HasForeignKey(n => n.OwnerId);
//            var conv = new ValueConverter<List<Permissions>,string>(
//                p => string.Join(",",p.ToArray()),
//                s => s.Split(',').Select(e => (Permissions)Enum.Parse(typeof(Permissions),e,true)).ToList()
//            );
//            modelBuilder.Entity<ServerAccount>(b => {
//                b.HasOne(n => n.homeComputer)
//                    .WithOne(n => n.owner)
//                    .HasForeignKey<Node>(n => n.OwnerId);
//                b.Property(l => l.netmap)
//                    .HasConversion(l => ServerAccount.NetMapNode.maptoList(l), l => ServerAccount.NetMapNode.ListToMap(l));
//                b.Property(f => f.permissions)
//                    .HasConversion(conv);
//                b.HasIndex(m => m.mailaddress).IsUnique();
//            });
//
//
//            modelBuilder.Entity<File>(b => {
//                b.HasOne(f => f.FileSystem).WithOne().HasForeignKey<File>(f => f.FilesystemId).IsRequired(false);
//                b.HasIndex(m => new {m.Name,m.ParentId,m.FilesystemId}).IsUnique();
//                b.HasMany(f => f.children).WithOne(f => f.Parent).HasForeignKey(f => f.ParentId)
//                    .IsRequired(false);
//            });
//
//            modelBuilder.Entity<FileSystem>(b => {
//                b.HasOne(f => f.RootFile).WithOne(f => f.FileSystem).HasForeignKey<FileSystem>(f => f.RootFileId)
//                    .IsRequired(false);
//            });
//            modelBuilder.Entity<Node>(b => {
//                b.HasOne(f => f.fileSystem).WithOne().HasForeignKey<Node>(f => f.FileSystemId);
//            });

        }


        public bool TryLogin(GameClient client, string tempUsername, string tempPass, out ServerAccount homeId)
        {
            homeId = null;
            ServerAccount acc = ServerAccounts.Find(tempUsername);
            if (acc == null || !acc.password.Equals(tempPass)) return false;
            homeId = acc;
            return true;
        }


        public bool SetUserBanStatus(string ac, int banExpiry, bool unban)
        {
            ServerAccount acc = ServerAccounts.Find(ac);
            if (acc == null) return false;
            acc.SetUserBanStatus(!unban, banExpiry);
            return true;
        }

        public bool CheckUserBanStatus(ServerAccount user, out int banExpiry)
        {
            return user.IsBanned(out banExpiry);
        }


        public void AddUserNode(ServerAccount acc, Node n, string pos)
        {
            acc.netmap.Add(new ServerAccount.NetMapNode(n.ip, pos));
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


        public void RebuildDatabase()
        {
            if (!sqlite)
            {
                Database.Delete();
                Database.Create();
            }

            Computers.AddRange(Node.Defaults);
            SaveChanges();
            Binaries.AddRange(Binary.getBinaries());
            ServerAccounts.AddRange(ServerAccount.Defaults);
            SaveChanges();
            var defAcc = ServerAccounts.First();
            defAcc.Nodes.Add(Node.Defaults[0]);
            defAcc.netmap.Add(new ServerAccount.NetMapNode(Node.Defaults[0].ip,"0,0"));
            SaveChanges();
        }

        public override int SaveChanges()
        {
            try
            {
                return base.SaveChanges();
            }
            catch (System.Data.Entity.Validation.DbEntityValidationException dbEx)
            {
                Exception raise = dbEx;
                foreach (var validationErrors in dbEx.EntityValidationErrors)
                {
                    foreach (var validationError in validationErrors.ValidationErrors)
                    {
                        string message = string.Format("{0}:{1}", 
                            validationErrors.Entry.Entity.ToString(),
                            validationError.ErrorMessage);
                        // raise a new exception nesting
                        // the current instance as InnerException
                        raise = new InvalidOperationException(message, raise);
                    }
                }
                throw raise;
            }
            
        }

        public DbSet<ServerAccount> GetUsersInDatabase()
        {
            return ServerAccounts;
        }
    }

    public class DatabaseConfiguration : DbConfiguration{
        public DatabaseConfiguration()
        {
            
            if (DatabaseLink.sqlite)
            {
                SetMigrationSqlGenerator("System.Data.SQLite", () => new SQLiteMigrationSqlGenerator());
            }
        }
    }
}
