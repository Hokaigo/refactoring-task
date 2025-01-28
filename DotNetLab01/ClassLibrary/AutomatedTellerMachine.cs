using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static ClassLibrary.Account;

namespace ClassLibrary
{
    public class AutomatedTellerMachine
    {
        public int ID { get; set; }
        public int ATMBalance { get; set; }
        public string Address { get; set; }

        public delegate void ATMStateHandler(string msg);
        public event ATMStateHandler Withdrawn;

        public AutomatedTellerMachine(int id, int aTBBalance, string address)
        {
            ID = id;
            ATMBalance = aTBBalance;
            Address = address;
        }

        public bool WithdrawATM(int cash)
        {
            if (ATMBalance >= cash)
            {
                ATMBalance -= cash;
                return true;
            }
            else
            {
                if (Withdrawn != null) Withdrawn("У банкоматі закінчилися кошти або їх недостатньо щоб виконати ваш запит! Спробуйте пізніше!");
                return false;
            }
        }

        public void PutCashATM(int cash)
        {
            ATMBalance += cash;
        }

    }
}
