﻿using HackLinks_Server.Computers;
using HackLinks_Server.Computers.Processes;
using HackLinks_Server.Daemons.Types.Mission;
using HackLinks_Server.Files;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;

namespace HackLinks_Server.Daemons.Types {
    class MissionDaemon : Daemon
    {
        public override string StrType => "mission";

        protected override Type ClientType => typeof(MissionClient);

        public override DaemonType GetDaemonType()
        {
            return DaemonType.MISSION;
        }

        public MissionDaemon(int pid, Printer printer, Node computer, Credentials credentials) : base(pid, printer, computer, credentials)
        {

        }

        public List<MissionAccount> accounts = new List<MissionAccount>();
        public Dictionary<int, MissionListing> missions = new Dictionary<int, MissionListing>();

        public void LoadAccounts()
        {
            accounts.Clear();
            File accountFile = node.fileSystem.RootFile.GetFileAtPath("/mission/accounts.db");
            if (accountFile == null)
                return;
            foreach (string line in accountFile.Content.Split(new string[] { "\n", "\r\n" }, StringSplitOptions.RemoveEmptyEntries))
            {
                var data = line.Split(',');
                if (data.Length < 6)
                    continue;
                accounts.Add(new MissionAccount(data[0], Convert.ToInt32(data[1]), Convert.ToInt32(data[2]), data[3], Server.Instance.DatabaseLink.ServerAccounts.Find(data[4]), data[5]));
            }
        }

        public void LoadMissions()
        {
            missions.Clear();
            File missionFile = node.fileSystem.RootFile.GetFileAtPath("/mission/missions.db");
            if (missionFile == null)
                return;
            JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            missions = JsonConvert.DeserializeObject<Dictionary<int, MissionListing>>(missionFile.Content, settings);
            if (missions == null)
                missions = new Dictionary<int, MissionListing>();
        }

        public void UpdateAccountDatabase()
        {
            File accountFile = node.fileSystem.RootFile.GetFileAtPath("/mission/accounts.db");
            if (accountFile == null)
                return;
            string newAccountsFile = "";
            foreach (var account in accounts)
            {
                newAccountsFile += account.accountName + "," + account.ranking + "," + account.currentMission + "," + account.password + "," + account.ServerAccount.username + "\r\n";
            }
            accountFile.Content = newAccountsFile;
        }

        public void UpdateMissionDatabase()
        {
            File missionFile = node.fileSystem.RootFile.GetFileAtPath("/mission/missions.db");
            if (missionFile == null)
                return;
            JsonSerializerSettings settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };
            missionFile.Content = JsonConvert.SerializeObject(missions, settings);
        }

        public bool CheckFolders(CommandProcess process)
        {
            var missionFolder = process.computer.fileSystem.RootFile.GetFile("mission");
            if (missionFolder == null || !missionFolder.IsFolder())
            {
                process.Print("No mission daemon folder was found ! (Contact the admin of this node to create one as the mission board is useless without one)");
                return false;
            }
            var accountFile = missionFolder.GetFile("accounts.db");
            if (accountFile == null)
            {
                process.Print("No accounts file was found ! (Contact the admin of this node to create one as the mission board is useless without one)");
                return false;
            }
            var missionFile = missionFolder.GetFile("missions.db");
            if (accountFile == null)
            {
                process.Print("No missions file was found ! (Contact the admin of this node to create one as the mission board is useless without one)");
                return false;
            }
            return true;
        }

        public override void OnStartUp()
        {
            LoadAccounts();
            LoadMissions();
        }

        public override string GetSSHDisplayName()
        {
            return null;
        }
    }
}
