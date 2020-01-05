using HackLinks_Server.Files;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
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
        public int FileSystemId { get; set; }
        
        [InverseProperty("FileSystem")]
        public virtual List<File> AllFiles { get; }

        public File RootFile
        {
            get { return AllFiles.Find(f => f.Parent == null); }
        }

        public FileSystem()
        {
            AllFiles = new List<File>();
            CreateRoot();
        }

        public FileSystem(int fileSystemId,File RootFile, List<File> allFiles)
        {
            this.FileSystemId = fileSystemId;
            AllFiles = allFiles;
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

        public void CreateRoot(File newFile = null)
        {
            if (newFile == null)
            {
                AllFiles.Add(File.GetRoot(this));
            }
            else
            {
                newFile.Parent = null;
                AllFiles.Add(newFile);
            }
        }
    }
}
