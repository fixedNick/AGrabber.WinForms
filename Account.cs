using System.Collections.Generic;

namespace AGrabber.WinForms
{
    class Account
    {
        public static string Name = "Алексей";
        public static string Phone = "89991012310";

        public string Login;
        public string Password;

        static public List<Account> Accounts = new List<Account>();

        public Account(string login, string pass)
        {
            Login = login;
            Password = pass;
            Accounts.Add(this);
        }

        public static Account GetAccount(string login)
        {
            foreach (var a in Accounts)
            {
                if (a.Login.Equals(login))
                    return a;
            }

            return null;
        }

        public static Account GetSelectedAccount() => GetAccount(Form1.MainForm.GetSelectedAccountString());
    }
}   
