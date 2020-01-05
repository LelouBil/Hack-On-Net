using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using HackLinks_Server.Computers;
using HackLinks_Server.Computers.Files;
using HackLinks_Server.Computers.Permissions;
using HackLinks_Server.Computers.Processes;
using HackLinks_Server.Util;

namespace HackLinks_Server.Files
{
    public class File
    {

        /// <summary>FilType determines how a file will be handled by the system</summary>
        public enum FileType
        {
            Regular,
            Directory,
            Link,
            LOG,
            // TODO other types 
        }

        [StringLength(255)]
        public string Name { get; private set; }
        
        [Key] [Required]
        public int id { get; set; }
        
        [Required]
        public int OwnerId { get; set; }

        [Required]
        public int groupId { get; private set; }
        
        [NotMapped]
        public Group Group {
            get => (Group) groupId;
            set => groupId = (int) value;
        }

        public string content { get; set; } = "";// TODO make hash function portable/low collision eg. https://softwareengineering.stackexchange.com/questions/49550/which-hashing-algorithm-is-best-for-uniqueness-and-speed

        
        [NotMapped]
        public string Content {
            get => content;
            set {
                content = value;
                Checksum = content?.GetHashCode() ?? 0;
            }
        }

        [InverseProperty("AllFiles")]
        public FileSystem FileSystem { get; set; }

        [Required]
        public FileType Type { get; set; } = FileType.Regular;

        [Required]
        public int filePermissions { get; set; }
        [NotMapped]
        public FilePermissions Permissions { get => FilePermissions.FromDigit(filePermissions);
            set => filePermissions = value.PermissionValue;
        }
        
        
        
        public int Checksum { get; private set; }
        
        
        
        public File Parent { get; set; }

        
        [InverseProperty("Parent")]
        public virtual List<File> children { get; set; } = new List<File>();

        private File(FileSystem fs, File parent, string name)
        {
            this.FileSystem = fs;
            this.Name = name;
            this.Parent = parent;
            Server.Instance.DatabaseLink.Entry(this);
            if(parent != null)
            {
                this.Parent.children.Add(this);
            }
            Permissions = new FilePermissions();
        }
        

        public bool HasExecutePermission(Credentials credentials)
        {
            return HasPermission(credentials.UserId, credentials.Group, false, false, true);
        }

        public bool HasWritePermission(Credentials credentials)
        {
            return HasPermission(credentials.UserId, credentials.Group, false, true, false);
        }

        public bool HasReadPermission(Credentials credentials)
        {
            return HasPermission(credentials.UserId, credentials.Group, true, false, false);
        }

        public bool HasExecutePermission(int userId, Group priv)
        {
            return HasPermission(userId, priv, false, false, true);
        }

        public bool HasWritePermission(int userId, Group priv)
        {
            return HasPermission(userId, priv, false, true, false);
        }

        public bool HasReadPermission(int userId, Group priv)
        {
            return HasPermission(userId, priv, true, false, false);
        }

        public bool HasExecutePermission(int userId, List<Group> privs)
        {
            return HasPermission(userId, privs, false, false, true);
        }

        public bool HasWritePermission(int userId, List<Group> privs)
        {
            return HasPermission(userId, privs, false, true, false);
        }

        public bool HasReadPermission(int userId, List<Group> privs)
        {
            return HasPermission(userId, privs, true, false, false);
        }

        public bool HasPermission(int userId, Group priv, bool read, bool write, bool execute)
        {
            return HasPermission(userId, new List<Group> { priv }, read, write, execute);
        }

        public bool HasPermission(int userId, List<Group> privs, bool read, bool write, bool execute)
        {
            if (privs.Contains((Group) groupId))
            {
                if (Permissions.CheckPermission(FilePermissions.PermissionType.Group, read, write, execute))
                {
                    return true;
                }
            }

            if (OwnerId == userId)
            {
                if (Permissions.CheckPermission(FilePermissions.PermissionType.User, read, write, execute))
                {
                    return true;
                }
            }

            return Permissions.CheckPermission(FilePermissions.PermissionType.Others, read, write, execute);
        }

        public bool IsFolder()
        {
            return Type == FileType.Directory;
        }

        public void RemoveFile()
        {
            Parent.children.Remove(this);
            Parent = null;
//            if (Type == FileType.LOG)
//            {
//                Log log = null;
//                foreach (var log2 in Computer.logs)
//                {
//                    if (log2.file == this)
//                    {
//                        log = log2;
//                        break;
//                    }
//                }
//                Computer.logs.Remove(log);
//            }
        }
        

        public string[] GetLines()
        {
            return this.Content.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.None);
        }

        public File GetFile(string name)
        {
            foreach (File file in children)
            {
                if (file.Name == name)
                    return file;
            }
            return null;
        }

        public File GetFileAtPath(string path)
        {
            string[] pathSteps = path.Split(new char[] { '/' }, StringSplitOptions.RemoveEmptyEntries);
            File activeFolder = this;
            for (int i = 0; i < pathSteps.Length - 1; i++)
            {
                var folder = activeFolder.GetFile(pathSteps[i]);
                if (folder == null || !folder.IsFolder())
                    return null;
                activeFolder = folder;
            }
            return activeFolder.GetFile(pathSteps[pathSteps.Length - 1]);
        }

        public void PrintFolderRecursive(int depth)
        {
            string tabs = new String(' ', depth);
            Logger.Debug(tabs + "  d- " + Name);
            foreach (var item in children)
            {
                if (item.IsFolder())
                {
                    item.PrintFolderRecursive(depth + 1);
                }
                else
                {
                    Logger.Debug(tabs + "  f- " + item.Name);
                }
            }
        }

        //used by db
        private File(String Name,FileSystem FileSystem,File Parent, FileType Type, string Content, int groupId, int Permissions, int OwnerId) {
            this.Name = Name;
            this.Parent = Parent;
            this.Type = Type;
            this.Content = Content;
            this.FileSystem = FileSystem;
            this.groupId = groupId;
            this.OwnerId = OwnerId;
            this.Permissions = FilePermissions.FromDigit(Permissions);
        }

        private File() {
            
        }
        public File MkDir(string name, int ownerid = 0 ,int permissions = 774,int groupId = 0)
        {
            File child = new File(name,this,FileType.Directory,"",groupId,permissions,ownerid);
            return child;
        }
        
        public File MkFile(string name,string content = "", int ownerid = 0 ,int permissions = 774,int groupId = 0)
        {
            File child = new File(name,this,FileType.Regular,content,groupId,permissions,ownerid);
            return child;
        }
        
        private File(String Name, File Parent, FileType Type, string Content, int groupId, int Permissions, int OwnerId) {
            this.Name = Name;
            this.Parent = Parent;
            this.Type = Type;
            this.Content = content;
            this.FileSystem = Parent.FileSystem;
            this.groupId = groupId;
            this.OwnerId = OwnerId;
            this.Permissions = FilePermissions.FromDigit(Permissions);
            Server.Instance.DatabaseLink.files.Add(this);
        }

        public static File GetRoot(FileSystem fs) {
            return new File(" ",fs,null,FileType.Directory,"",0,774,0);
        }

        public static File CreateNewFile(File parent, string filename, string content = "") {
            return parent.MkFile(filename, content);
        }

        public static File CreateNewFolder( File parent, string name) {
            return parent.MkDir(name);
        }
    }
}
