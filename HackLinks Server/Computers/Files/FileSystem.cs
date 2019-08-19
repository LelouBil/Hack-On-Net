using HackLinks_Server.Files;
using System;
using System.Collections.Generic;
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
        public readonly FileSystemManager fileSystemManager;

        public File rootFile;

        private Node node;

        public FileSystem(FileSystemManager fileSystemManager,Node n)
        {
            this.fileSystemManager = fileSystemManager;
            this.node = n;
            this.rootFile = fileSystemManager.CreateRootFile(n);
            SetupDefaults();
        }

        private void SetupDefaults() {
            File etc = rootFile.MkDir("etc");
            File bin = rootFile.MkDir("bin");
            File daemons = rootFile.MkDir("daemons");
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

        public File CreateFile(Node computer, File parent, string fileName)
        {
            return File.CreateNewFile(fileSystemManager, computer, parent, fileName);
        }

        public File CreateFile(int id, Node computer, File parent, string fileName)
        {
            return File.CreateNewFile(id, fileSystemManager, computer, parent, fileName);
        }

        public File CreateFolder(Node computer, File parent, string fileName)
        {
            return File.CreateNewFolder(fileSystemManager, computer, parent, fileName);
        }
    }
}
