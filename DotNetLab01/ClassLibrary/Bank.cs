using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Principal;
using System.Text;
using System.Threading.Tasks;
using static ClassLibrary.Account;

namespace ClassLibrary
{
    public class Bank
    {
        public string Name { get; set; }
        public List<AutomatedTellerMachine> ATMs { get; set; }
        public List<Account> Accounts { get; set; }

        public Bank(string name)
        {
            Name = name;
            ATMs = new List<AutomatedTellerMachine>();
            Accounts = new List<Account>();
        }

        public void AddATM(AutomatedTellerMachine atm)
        {
            ATMs.Add(atm);
        }

        public void AddAccount(Account account)
        {
            Accounts.Add(account);
        }

        public Account FindAccount(string cardNumber)
        {
            for (int i = 0; i < Accounts.Count; i++)
            {
                if (Accounts[i].CardNumber == cardNumber)
                {
                    return Accounts[i];
                }
            }
            return null;
        }

        public AutomatedTellerMachine FindATM(int id)
        {
            for (int i = 0; i < ATMs.Count; i++)
            {
                if (ATMs[i].ID == id)
                {
                    return ATMs[i];
                }
            }
            return null;
        }
    }
}
