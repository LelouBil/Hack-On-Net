using HackLinks_Server.Computers;
using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Files;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Entity;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlClient;
using System.Data.SQLite;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HackLinks_Server.Computers.DataObjects;
using HackLinks_Server.Computers.Files;
using HackLinks_Server.Util;

namespace HackLinks_Server.Database {
    public class DatabaseLink : DbContext {
        private const string dbpath = ".\\database.db";

        public DbSet<Binary> Binaries { get; set; }
        public DbSet<Node> Computers { get; set; }
        public DbSet<FileSystem> FileSystems { get; set; }
        public DbSet<ServerAccount> ServerAccounts { get; set; }


        public DatabaseLink(ConfigUtil.ConfigData config) : base(GetConnectionString(config)) { }

        private static string GetConnectionString(ConfigUtil.ConfigData config) {
            DbConnectionStringBuilder connectionStringBuilder;
            string provider;
            if (config.Sqlite) {
                var sqlitecn = new SQLiteConnectionStringBuilder();
                provider = "System.Data.SQLite.EF6";
                sqlitecn.DataSource = dbpath;
                connectionStringBuilder = sqlitecn;
            }
            else {
                var sqlcn = new MySqlConnectionStringBuilder();
                provider = "MySql.Data.MySqlClient";
                sqlcn.Server = config.MySQLServer;
                sqlcn.Database = config.Database;
                sqlcn.UserID = config.UserID;
                sqlcn.Password = config.Password;
                connectionStringBuilder = sqlcn;
            }

            var ent = new EntityConnectionStringBuilder();
            ent.Provider = provider;
            ent.ProviderConnectionString = connectionStringBuilder.ConnectionString;
            return ent.ConnectionString;
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
        

        public void AddUserNode(ServerAccount acc, string ip, string pos) {
            Node n = new Node(ip);
            acc.Nodes.Add(n);
            acc.netmap.Add(n,pos);
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
            Binaries.RemoveRange(Binaries);
            ServerAccounts.RemoveRange(ServerAccounts);
            FileSystems.RemoveRange(FileSystems);
            Computers.RemoveRange(Computers);

            ServerAccounts.AddRange(ServerAccount.Defaults);
            
            Binaries.AddRange(Binary.getBinaries());
            
            Computers.AddRange(Node.Defaults);
        }

        public DbSet<ServerAccount> GetUsersInDatabase() {
            return ServerAccounts;
        }
    }
}
