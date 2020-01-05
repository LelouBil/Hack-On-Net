
using System;
using System.Data.Entity;
using System.Data.Entity.Migrations;
using System.Data.SQLite.EF6.Migrations;
using System.Linq;

namespace HackLinks_Server.Migrations
{
    internal sealed class Configuration : DbMigrationsConfiguration<HackLinks_Server.Database.DatabaseLink>
    {
        public Configuration()
        {
            AutomaticMigrationsEnabled = true;
        }
    } 
}