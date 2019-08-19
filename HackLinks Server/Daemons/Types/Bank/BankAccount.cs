using HackLinks_Server.Computers.DataObjects;

namespace HackLinks_Server.Daemons.Types.Bank {
    class BankAccount
    {
        public string accountName;
        public int balance;
        public string password;
        public ServerAccount client;
        public string email;

        public BankAccount(string accountName, int balance, string password, ServerAccount client, string email)
        {
            this.accountName = accountName;
            this.balance = balance;
            this.password = password;
            this.client = client;
            this.email = email;
        }
    }
}
