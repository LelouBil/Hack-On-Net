using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Files;
using MySql.Data.MySqlClient;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HackLinks_Server.Util;
using Microsoft.EntityFrameworkCore;

namespace HackLinks_Server.Computers
{
    public class ComputerManager
    {
        Server server;
        
        private List<File> toDelete = new List<File>();
        public List<File> ToDelete => toDelete;
        public DbSet<Node> NodeList => server.DatabaseLink.Computers;

        public ComputerManager(Server server)
        {
            this.server = server;
        }

        public void Init()
        {
            Logger.Info("Initializing daemons");
            foreach (Node node in server.DatabaseLink.Computers)
            {
                var daemonsFolder = node.fileSystem.RootFile.GetFile("daemons");
                if (daemonsFolder == null)
                    continue;
                var autorunFile = daemonsFolder.GetFile("autorun");
                if (autorunFile == null)
                    continue;
                foreach (string line in autorunFile.Content.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
                {
                    var daemonFile = daemonsFolder.GetFile(line);
                    if (daemonFile == null)
                        continue;
                    if (daemonFile.OwnerId != 0 || daemonFile.Group != Group.ROOT)
                        continue;
                    if (!daemonFile.HasExecutePermission(0, Group.ROOT))
                        continue;
                    //TODO user credentials from autorun file
                    node.LaunchDaemon(daemonFile);
                }
            }
        }

        public Node GetNodeByIp(string ip)
        {
            return server.DatabaseLink.Computers.FirstOrDefault(n => n.ip.Equals(ip));
        }

        public Node GetNodeById(int homeId) {
            return server.DatabaseLink.Computers.Find(homeId);
        }

        public static void FixFolder(List<File> files, File rootFile)
        {
            List<File> fixedFiles = new List<File>();
            Queue<File> fileQueue = new Queue<File>();

            fileQueue.Enqueue(rootFile);

            while(fileQueue.Any())
            {
                File parent = fileQueue.Dequeue();
                Logger.Info($"Processing File {parent.Name} ");

                foreach (File child in files.Where(x => x.Parent.Equals(parent)))
                {
                    Logger.Info($"Processing Child File {child.Name} of {parent.Name} ");

                    child.Parent = parent;
                    parent.children.Add(child);

                    fixedFiles.Add(child);
                    if(child.IsFolder())
                    {
                        fileQueue.Enqueue(child);
                    }
                }
            }
        }

        public void AddToDelete(File file)
        {
            toDelete.Add(file);
        }
    }
}
