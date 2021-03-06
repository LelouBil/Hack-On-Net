﻿using HackLinks_Server.Computers.Files;
using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Computers.Processes;
using HackLinks_Server.Daemons;
using HackLinks_Server.Daemons.Types;
using HackLinks_Server.Files;
using HackLinksCommon;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HackLinks_Server.Computers.DataObjects;

namespace HackLinks_Server.Computers
{
    public class Node
    {
        public static string SERVER_CONFIG_PATH = "/cfg/server.cfg";

        public const string DefaultGroups = "root:x:0:\r\nadmin:x:1:root,admin\r\nuser:x:2:root,admin,user\r\nguest:x:3:root,admin,user,guest\r\n";
        
        public const string DefaultPasswd = "root:x:0:0:root:/root:/bin/hash\r\nadmin:x:1:1:root:/root:/bin/hash\r\nuser:x:2:2:root:/root:/bin/hash\r\nguest:x:3:3:root:/root:/bin/hash\r\n";

        
        [Key] [Required] [StringLength(15)]
        public string ip { get; set; }

        
        public virtual ServerAccount owner { get; set; }
        
        public string OwnerId { get; set; }
        
        [Required]
        public int type { get; set; }

        
        
        public FileSystem fileSystem { get; private set; }

        public List<Session> sessions = new List<Session>();
        public List<Daemon> daemons = new List<Daemon>();
        public List<Log> logs = new List<Log>();
        public string bankAccountRedirectionInfo = "";

        
        [NotMapped]
        public Kernel Kernel { get; set; }

        private Dictionary<int, Process> processes = new Dictionary<int, Process>();

        public Stack<int> freedPIDs = new Stack<int>();

        private int nextPID = 2;
        private Dictionary<int, int> parents = new Dictionary<int, int>();
        private Dictionary<int, List<int>> children = new Dictionary<int, List<int>>();


        public int NextPID => freedPIDs.Count > 0 ? freedPIDs.Pop() : nextPID++;
        public static List<Node> Defaults { get; set; } = new List<Node>() {
            new Node() {
                ip = "8.8.8.8"
            }
        };
        

        public Node() {
            fileSystem = new FileSystem();
            fileSystem.SetupDefaults();
            Kernel = new Kernel(this);
        }

        public Node(FileSystem fileSystem, string ip, ServerAccount owner, int type) {
            this.fileSystem = fileSystem;
            this.ip = ip;
            this.owner = owner;
            this.type = type;
            Kernel = new Kernel(this);
        }

        public Session GetSession(int processId)
        {
            do
            {
                foreach (Session session in sessions)
                {
                    if(session.HasProcessId(processId))
                    {
                        return session;
                    }
                }
                processId = parents.ContainsKey(processId) ? parents[processId] : 0;
            } while (processId != 0);


            return null;
        }

        public string GetDisplayName()
        {
            var cfgFile = fileSystem.RootFile.GetFileAtPath(SERVER_CONFIG_PATH);
            if (cfgFile == null)
                return ip;
            var lines = cfgFile.GetLines();
            foreach(var line in lines)
            {
                if (line.StartsWith("name="))
                    return line.Substring(5);
            }
            return ip;
        }

        public string GetBankAccountRedirectionInfo()
        {
            if (string.IsNullOrWhiteSpace(bankAccountRedirectionInfo))
            {
                var cfgFile = fileSystem.RootFile.GetFileAtPath("/cfg/BankAccountRedirectionInfo.cfg");
                if (cfgFile != null)
                    bankAccountRedirectionInfo = cfgFile.Content;
            }
            return bankAccountRedirectionInfo;
        }

        public void LaunchDaemon(File daemonLauncher)
        {
            var lines = daemonLauncher.Content.Split(new string[]{ "\r\n" }, StringSplitOptions.None);
            if(lines[0] == "IRC")
            {
                var newDaemon = new IrcDaemon(NextPID, null, this, new Credentials(GetUserId("guest"), Group.GUEST));
                daemons.Add(newDaemon);
            }
            else if(lines[0] == "DNS")
            {
                var newDaemon = new DNSDaemon(NextPID, null, this, new Credentials(GetUserId("guest"), Group.GUEST));
                daemons.Add(newDaemon);
            }
            else if(lines[0] == "HTTP")
            {
                var newDaemon = new HTTPDaemon(NextPID, null, this, new Credentials(GetUserId("guest"), Group.GUEST));
                daemons.Add(newDaemon);
            }
            else if(lines[0] == "BANK")
            {
                var newDaemon = new BankDaemon(NextPID, null, this, new Credentials(GetUserId("guest"), Group.GUEST));
                daemons.Add(newDaemon);
            }
            else if (lines[0] == "MISSION")
            {
                var newDaemon = new MissionDaemon(NextPID, null, this, new Credentials(GetUserId("guest"), Group.GUEST));
                daemons.Add(newDaemon);
            }
            else if (lines[0] == "MAIL")
            {
                var newDaemon = new MailDaemon(NextPID, null, this, new Credentials(GetUserId("guest"), Group.GUEST));
                daemons.Add(newDaemon);
            }
        }

        public Daemon GetDaemon(string type)
        {
            foreach(Daemon daemon in daemons)
                if (daemon.IsOfType(type))
                    return daemon;
            return null;
        }

        public bool HasUser(string username)
        {
            return GetUserId(username) != -1;
        }

        public string GetUserShell(int userId)
        {
            File configFolder = fileSystem.RootFile.GetFile("etc");
            if (configFolder == null || !configFolder.IsFolder())
            {
                return "";
            }
            File usersFile = configFolder.GetFile("passwd");
            if (usersFile == null)
            {
                return "";
            }
            File groupFile = configFolder.GetFile("group");
            if (usersFile == null)
            {
                return "";
            }
            string[] accounts = usersFile.Content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            string[] groups = groupFile.Content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string account in accounts)
            {
                string[] accountData = account.Split(':');
                string accountUserIdString = accountData[2];

                if (userId.ToString() == accountUserIdString)
                {
                    string accountUserShell = accountData[6];
                    return accountUserShell;
                }
            }
            return "";
        }

        public void SetChildProcess(Process process, Process child)
        {
            SetChildProcess(process.ProcessId, child.ProcessId);
        }

        protected void SetChildProcess(int process, int child)
        {
            if (parents.ContainsKey(child))
            {
                children[parents[child]].Remove(child);
            }
            if (!children.ContainsKey(process))
            {
                children.Add(process, new List<int>());
            }
            parents.Add(child, process);
            children[process].Add(child);
        }

        public string GetUsername(int userId)
        {
            File configFolder = fileSystem.RootFile.GetFile("etc");
            if (configFolder == null || !configFolder.IsFolder())
            {
                return "";
            }
            File usersFile = configFolder.GetFile("passwd");
            if (usersFile == null)
            {
                return "";
            }
            File groupFile = configFolder.GetFile("group");
            if (usersFile == null)
            {
                return "";
            }
            string[] accounts = usersFile.Content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            string[] groups = groupFile.Content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string account in accounts)
            {
                string[] accountData = account.Split(':');
                string accountUsername = accountData[0];
                string accountUserIdString = accountData[2];

                if (userId.ToString() == accountUserIdString)
                {
                    return accountUsername;
                }
            }
            return "";
        }

        public int GetUserId(string username)
        {
            File configFolder = fileSystem.RootFile.GetFile("etc");
            if (configFolder == null || !configFolder.IsFolder())
            {
                return -1;
            }
            File usersFile = configFolder.GetFile("passwd");
            if (usersFile == null)
            {
                return -1;
            }
            File groupFile = configFolder.GetFile("group");
            if (usersFile == null)
            {
                return -1;
            }
            string[] accounts = usersFile.Content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            string[] groups = groupFile.Content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string account in accounts)
            {
                string[] accountData = account.Split(':');
                string accountUsername = accountData[0];
                string accountUserIdString = accountData[2];

                if (accountUsername == username)
                {
                    return int.TryParse(accountUserIdString, out int result) ? result : -1;
                }
            }
            return -1;
        }

        // TODO no prints
        // TODO Log Errors to log file?
        public Credentials Login(GameClient client, string username, string password)
        {
            var configFolder = fileSystem.RootFile.GetFile("etc");
            if (configFolder == null || !configFolder.IsFolder())
            {
                client.Send(NetUtil.PacketType.MESSG, "No config folder was found!");
                return null;
            }
            File usersFile = configFolder.GetFile("passwd");
            if (usersFile == null)
            {
                client.Send(NetUtil.PacketType.MESSG, "No passwd file was found!");
                return null;
            }
            File groupFile = configFolder.GetFile("group");
            if (usersFile == null)
            {
                client.Send(NetUtil.PacketType.MESSG, "No group file was found!");
                return null;
            }
            string[] accounts = usersFile.Content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            string[] groups = groupFile.Content.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            foreach (string account in accounts)
            {
                string[] accountData = account.Split(':');
                string accountUsername = accountData[0];
                string accountPassword = accountData[1];
                string accountGroupId = accountData[3];

                if (accountUsername == username && accountPassword == password)
                {
                    Group primaryGroup = PermissionHelper.GetGroupFromString(accountGroupId);
                    if (primaryGroup == Group.INVALID)
                    {
                        client.Send(NetUtil.PacketType.MESSG, $"Can't login as {username}, '{accountGroupId}' is not a valid accountGroupId");
                        break;
                    }
                    List<Group> loginGroups = new List<Group>();
                    foreach(string group in groups)
                    {
                        string[] groupData = group.Split(':');
                        string groupName = groupData[0];
                        string groupId = groupData[2];
                        string[] groupUsers = groupData[3].Split(',');
                        if (groupUsers.Contains(username) || accountGroupId.Equals(groupId))
                        {
                            Group loginGroup = PermissionHelper.GetGroupFromString(groupId);
                            if (loginGroup != Group.INVALID)
                            {
                                loginGroups.Add(loginGroup);
                            }
                            else
                            {
                                client.Send(NetUtil.PacketType.MESSG, $"Can't login as {username} {groupName} is not a valid group");
                                break;
                            }
                        }
                    }
                    return new Credentials(GetUserId(username), primaryGroup, loginGroups);
                }
            }
            return null;
        }

        public void Log(Log.LogEvents logEvent, string message, int sessionId, string ip)
        {
            File logsFolder = null;
            foreach (var file in fileSystem.RootFile.children)
            {
                if (file.Name == "logs")
                {
                    logsFolder = file;
                    break;
                }
            }
            if (logsFolder == null)
            {
                logsFolder = File.CreateNewFolder( fileSystem.RootFile, "logs");
                logsFolder.OwnerId = 0;
                logsFolder.Permissions.SetPermission(FilePermissions.PermissionType.User, true, true, true);
                logsFolder.Permissions.SetPermission(FilePermissions.PermissionType.Group, true, true, true);
                logsFolder.Group = logsFolder.Parent.Group;
                logsFolder.Type = File.FileType.LOG;
            }
            message = message.Replace(' ', '_');
            File logFile = File.CreateNewFile(logsFolder, message);
            logFile.OwnerId = 0;
            logFile.Permissions.SetPermission(FilePermissions.PermissionType.User, true, true, true);
            logFile.Permissions.SetPermission(FilePermissions.PermissionType.Group, true, true, true);
            logFile.Group = logsFolder.Parent.Group;
            logFile.Type = File.FileType.LOG;
            logs.Add(new Log(logFile, sessionId, ip, logEvent, message));
        }

        public void Log(Log.LogEvents logEvent, string message, string messageExtended, int sessionId, string ip)
        {
            File logsFolder = null;
            foreach (var file in fileSystem.RootFile.children)
            {
                if (file.Name == "logs")
                {
                    logsFolder = file;
                    break;
                }
            }
            if (logsFolder == null)
            {
                logsFolder = File.CreateNewFolder(fileSystem.RootFile, "logs");
                logsFolder.OwnerId = 0;
                logsFolder.Permissions.SetPermission(FilePermissions.PermissionType.User, true, true, true);
                logsFolder.Permissions.SetPermission(FilePermissions.PermissionType.Group, true, true, true);
                logsFolder.Group = logsFolder.Parent.Group;
                logsFolder.Type = File.FileType.LOG;
            }
            File logFile = File.CreateNewFile(logsFolder, message);
            logFile.OwnerId = 0;
            logFile.Permissions.SetPermission(FilePermissions.PermissionType.User, true, true, true);
            logFile.Permissions.SetPermission(FilePermissions.PermissionType.Group, true, true, true);
            logFile.Group = logsFolder.Parent.Group;
            logFile.Type = File.FileType.LOG;
            logs.Add(new Log(logFile, sessionId, ip, logEvent, message, messageExtended));
        }

        internal void ParseLogs()
        {
            List<Log> logs = new List<Log>();
            File logsFolder = null;
            foreach (var file in fileSystem.RootFile.children)
            {
                if (file.Name == "logs")
                {
                    logsFolder = file;
                    break;
                }
            }
            if (logsFolder == null)
                return;

            foreach (var log in logsFolder.children)
            {
                string machineReadChars = "";
                int machineReadCharType = 0;
                int machineReadCharsFound = 0;
                int machineReadSplit = 0;
                foreach (var character in log.Content)
                {
                    if (character == '#' && machineReadCharType == 0 && machineReadCharsFound < 4)
                    {
                        machineReadChars = machineReadChars + "#";
                        machineReadCharsFound++;
                        if (machineReadCharsFound >= 4)
                        {
                            machineReadCharType++;
                            machineReadCharsFound = 0;
                        }
                    }
                    else if (character == '!' && machineReadCharType == 1 && machineReadCharsFound < 2)
                    {
                        machineReadChars = machineReadChars + "!";
                        machineReadCharsFound++;
                        if (machineReadCharsFound >= 2)
                        {
                            machineReadCharType++;
                            machineReadCharsFound = 0;
                        }
                    }
                    else if (character == '*' && machineReadCharType == 2 && machineReadCharsFound < 1)
                    {
                        machineReadChars = machineReadChars + "*";
                        machineReadCharsFound++;
                    }
                    else if (machineReadChars == "####!!*")
                        break;
                    else
                    {
                        machineReadChars = "";
                        machineReadCharType = 0;
                        machineReadCharsFound = 0;
                    }
                    machineReadSplit++;
                }

                machineReadSplit += 23;
                Log logAdd = Computers.Log.Deserialize(log.Content.Substring(machineReadSplit));
                logAdd.file = log;
                logs.Add(logAdd);
            }

            this.logs = logs;
        }

        internal void SetRoot(File newFile)
        {
            if(fileSystem.RootFile != null)
                throw new ArgumentException("Root file for this computer is already set.");
            fileSystem.CreateRoot(newFile);
        }

        internal void RegisterProcess(Process process)
        {
            processes[process.ProcessId] = process;
        }

        internal void NotifyProcessStateChange(int processId, Process.State newState)
        {
            switch (newState)
            {
                case Process.State.Dead:
                    int parentId = GetParentId(processId);
                    if (parentId > 1)
                    {
                        processes[parentId].NotifyDeadChild(processes[processId]);
                        children[parentId].Remove(processId);
                        parents.Remove(processId);
                    }
                    processes.Remove(processId);
                    freedPIDs.Push(processId);
                    // We give all the children away to (fake) init process if our process has any
                    if (children.ContainsKey(processId))
                    {
                        foreach (int child in children[processId])
                        {
                            SetChildProcess(1, child);
                        }
                    }
                    break;
            }
        }

        public int GetParentId(int pid)
        {
            return parents.ContainsKey(pid) ? parents[pid] : 1;
        }

        /*public Folder getFolderFromPath(string path, bool createFoldersThatDontExist = false)
        {
            Folder result;
            if (string.IsNullOrWhiteSpace(path))
            {
                result = rootFolder;
            }
            else
            {
                System.Collections.Generic.List<int> folderPath = this.getFolderPath(path, createFoldersThatDontExist);
                result = Computer.getFolderAtDepth(this, folderPath.Count, folderPath);
            }
            return result;
        }

        public System.Collections.Generic.List<int> getFolderPath(string path)
        {
            System.Collections.Generic.List<int> list = new System.Collections.Generic.List<int>();
            char[] separator = new char[]
            {
                '/',
                '\\'
            };
            string[] array = path.Split(separator);
            Folder folder = rootFolder;
            for (int i = 0; i < array.Length; i++)
            {
                bool flag = false;
                for (int j = 0; j < folder.children.Count; j++)
                {
                    if (folder.children[j].IsFolder() && folder.children[j].name.Equals(array[i]))
                    {
                        list.Add(j);
                        folder = (Folder)folder.children[j];
                        flag = true;
                        break;
                    }
                }
            }
            return list;
        }*/
    }
}
