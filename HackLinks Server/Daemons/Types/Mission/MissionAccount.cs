using HackLinks_Server.Computers.DataObjects;

namespace HackLinks_Server.Daemons.Types.Mission {
    class MissionAccount
    {
        public string accountName;
        public int ranking;
        public int currentMission;
        public string password;
        public ServerAccount ServerAccount;
        public string email;

        public MissionAccount(string accountName, int ranking, int currentMission, string password, ServerAccount serveraccount, string email)
        {
            this.accountName = accountName;
            this.ranking = ranking;
            this.currentMission = currentMission;
            this.password = password;
            this.ServerAccount = serveraccount;
            this.email = email;
        }
    }
}
