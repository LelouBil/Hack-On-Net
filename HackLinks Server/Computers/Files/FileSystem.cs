using HackLinks_Server.Files;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HackLinks_Server.Computers.Permissions;

namespace HackLinks_Server.Computers.Files
{
    /// <summary>
    /// Contains Files for a computer
    /// </summary>
    public class FileSystem
    {

        [Key] [Required]
        public int id { get; set; }
        
        public File RootFile { get; set; }
        
        public int? RootFileId { get; set; }


        public FileSystem()
        {
            this.RootFile = File.GetRoot(this);
            
            
        }

        public FileSystem(int id,File RootFile) {
            this.id = id;
            this.RootFile = RootFile;
        }

        public void SetupDefaults() {
            File etc = RootFile.MkDir("etc");
            File bin = RootFile.MkDir("bin");
            File daemons = RootFile.MkDir("daemons");
            etc.MkFile("passwd", groupId: 1,
                content:
                Node.DefaultPasswd);
            etc.MkFile("group", groupId: 1,
                content:
                Node.DefaultGroups);
            string[] hackybox = {
                "hackybox", "ping", "ls", "connect", "disconnect", "dc", "ls", "touch", "view", "mkdir", "rm", "login",
                "chown", "fedit", "netmap", "music"
            };
            foreach (var s in hackybox) {
                bin.MkFile(s, "hackybox");
            }
              
            daemons.MkFile("autorun", "irc\r\nbank", groupId: 1);
            bin.MkFile("admin", "serveradmin");
            bin.MkFile("cadmin", "computeradmin");
            bin.MkFile("hash", "hash");
        }
        
    }
}
