using System;
using System.Collections.Generic;
using System.Linq;

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
            if (atm == null)
                throw new ArgumentNullException("Назва банкомату не може бути null!");

            ATMs.Add(atm);
        }

        public void AddAccount(Account account)
        {
            if (account == null)
                throw new ArgumentNullException("Назва аккаунту не може бути null!");

            Accounts.Add(account);
        }

        private T FindItem<T>(List<T> items, Predicate<T> predicate) => items.FirstOrDefault(item => predicate(item));

        public Account FindAccount(string cardNumber)
        {
            return FindItem(Accounts, account => account.CardNumber == cardNumber);
        }

        public AutomatedTellerMachine FindATM(int id)
        {
            return FindItem(ATMs, atm => atm.ID == id);
        }
    }
}
