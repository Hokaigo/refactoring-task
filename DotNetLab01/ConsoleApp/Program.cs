using ClassLibrary;
using System;
using System.Text;
using System.Data.SqlClient;
using System.Collections.Generic;
using System.Net.Mail;
using System.Net;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.Configuration;

namespace ConsoleApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;
            Console.InputEncoding = Encoding.Unicode;

            var configuration = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
            string key = configuration["Encryption:Key"];
            string iv = configuration["Encryption:IV"];
            byte[] keyBytes = Convert.FromBase64String(key);
            byte[] ivBytes = Convert.FromBase64String(iv);

            Hashing hashing = new Hashing(keyBytes, ivBytes);

            string connectionString = $"Data Source=192.168.43.82,41433;Initial Catalog=ATMbd;User ID=sa;Password={hashing.Decrypt("IqsscPTuJIcGHlyH8yUxCQ==")};";
            DataBase db = new DataBase(connectionString, configuration);

            List<Account> accountsFromDB = db.GetAccounts("dbo.Account");
            List<AutomatedTellerMachine> ATMsFromDB = db.GetATM("dbo.ATM");
            Bank bank = new Bank("CringeBank");

            foreach (var account in accountsFromDB)
            {
                if (account.CreditCash == 0 && account.CreditDate != null)
                {
                    db.SetDateToNull("dbo.Account", account.CardNumber);
                }
                bank.AddAccount(account);
            }

            foreach (var atm in ATMsFromDB)
            {
                bank.AddATM(atm);
            }

            for(int i = 0; i < bank.ATMs.Count; i++) 
            {
                bank.ATMs[i].Withdrawn += Show_Message;
            }

            for (int i = 0; i < bank.Accounts.Count; i++)
            {
                bank.Accounts[i].Added += Show_Message;
                bank.Accounts[i].Withdrawn += Show_Message;
                bank.Accounts[i].BalanceChecked += Show_Message;
                bank.Accounts[i].Validated += Show_Message;
                bank.Accounts[i].Credit += Show_Message;
                bank.Accounts[i].Transfered += Show_Message;
                bank.Accounts[i].PasswordChanged += Show_Message;
            }

            for(int i = 0; i < bank.Accounts.Count; i++)
            {
                if (bank.Accounts[i].CreditDate != null)
                {
                    bank.Accounts[i].CreditDebt();
                }
            }

            AutomatedTellerMachine selectedATM = null;
            do
            {
                ShowAtmMenu(bank);
                int atmID;
                while (true)
                {
                    string input = Console.ReadLine();
                    if (int.TryParse(input, out atmID))
                    {
                        selectedATM = bank.FindATM(atmID);
                        if (selectedATM == null) Console.WriteLine("Здається ви неправильно ввели номер банкомату, спробуйте ще раз!");
                        break;
                    }
                    else
                    {
                        Console.WriteLine("Здається ви неправильно ввели номер банкомату, спробуйте ще раз!");
                        ShowAtmMenu(bank);
                    }
                }
            }
            while (selectedATM == null);

            Account neededAccount = null;
            string cardNumber;
            do
            {
                Console.WriteLine("Введіть номер картки:");
                cardNumber = Console.ReadLine();
                neededAccount = bank.FindAccount(cardNumber);
                if (neededAccount == null) Console.WriteLine("Здається ви неправильно ввели номер карти, спробуйте ще раз!");
            }
            while (neededAccount == null);

            if (neededAccount != null)
            {
                string password;
                do
                {
                    Console.WriteLine("Введіть пін-код:");
                    password = Console.ReadLine();
                    neededAccount.Validation(password);

                    if (neededAccount.Entered == true)
                    {
                        while (true)
                        {
                            ShowMenu();
                            int option;

                            while (true)
                            {
                                string inputOption = Console.ReadLine();
                                if (int.TryParse(inputOption, out option))
                                {
                                    break;
                                }
                                else
                                {
                                    Console.WriteLine("Такої опції не існує!");
                                    ShowMenu();
                                }
                            }

                            switch (option)
                            {
                                case 1:
                                     int balance = neededAccount.CurrentBalanceCheck;
                                    break;
                                case 2:
                                    Console.WriteLine("Введіть суму для поповнення: ");
                                    int putCash;
                                    while (true)
                                    {
                                        string input = Console.ReadLine();
                                        if (int.TryParse(input, out putCash) && putCash > 0)
                                        {
                                            neededAccount.Put(putCash);
                                            db.SetBalance("dbo.Account", neededAccount.CardNumber, neededAccount.Balance);
                                            selectedATM.PutCashATM(putCash);
                                            db.SetATMBalance("dbo.ATM", selectedATM.ID, selectedATM.ATMBalance);
                                            break;
                                        }
                                        else
                                        {
                                            Console.WriteLine("Сума поповнення має бути цілим числом та більше за нуль!");
                                        }
                                    }
                                    break;
                                case 3:
                                    Console.WriteLine("Введіть суму, яку хочете зняти: ");
                                    int withdrawCash;
                                    while (true)
                                    {
                                        string input = Console.ReadLine();
                                        if (int.TryParse(input, out withdrawCash) && withdrawCash > 0)
                                        {
                                            if (selectedATM.WithdrawATM(withdrawCash))
                                            {
                                                if (neededAccount.Withdraw(withdrawCash))
                                                {
                                                    db.SetBalance("dbo.Account", neededAccount.CardNumber, neededAccount.Balance);
                                                    db.SetATMBalance("dbo.ATM", selectedATM.ID, selectedATM.ATMBalance);
                                                    break;
                                                }
                                                else
                                                {
                                                    selectedATM.PutCashATM(withdrawCash);
                                                    db.SetATMBalance("dbo.ATM", selectedATM.ID, selectedATM.ATMBalance);
                                                    break;
                                                }
                                            }
                                            else
                                            {
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            Console.WriteLine("Сума виводу має бути цілим числом та більше за нуль!");
                                        }
                                    }
                                    break;
                                case 4:
                                    neededAccount.CreditCheck();
                                    break;
                                case 5:
                                    if (neededAccount.CreditCash <= 0)
                                    {
                                        Console.WriteLine("Введіть суму кредиту від 1000 до 15000: ");
                                        int creditCash;
                                        while (true)
                                        {
                                            string input = Console.ReadLine();
                                            if (int.TryParse(input, out creditCash) && creditCash >= 1000 && creditCash <= 15000)
                                            {
                                                neededAccount.TakeAnATMCredit(creditCash, selectedATM);
                                                DateTime gotCreditDate = DateTime.Now;
                                                db.SetDate("dbo.Account", neededAccount.CardNumber, gotCreditDate);
                                                db.SetBalance("dbo.Account", neededAccount.CardNumber, neededAccount.Balance);
                                                db.SetCreditCash("dbo.Account", neededAccount.CardNumber, neededAccount.CreditCash);
                                                db.SetATMBalance("dbo.ATM", selectedATM.ID, selectedATM.ATMBalance);
                                                break;
                                            }
                                            else
                                            {
                                                Console.WriteLine("Сума кредиту має бути цілим числом та від 1000 до 15000!");
                                            }
                                        }
                                    }
                                    else 
                                    {
                                        Console.WriteLine("Ви не можете взяти кредит, сплатіть попередній кредит!");
                                    }
                                    break;
                                case 6:
                                    if (neededAccount.CreditCash > 0)
                                    {
                                        Console.WriteLine("Введіть суму для погашення кредиту: ");
                                        int payCreditCash;
                                        while (true)
                                        {
                                            string input = Console.ReadLine();
                                            if (int.TryParse(input, out payCreditCash) && payCreditCash > 0)
                                            {
                                                neededAccount.PayCredit(payCreditCash, selectedATM, db, neededAccount);
                                                db.SetBalance("dbo.Account", neededAccount.CardNumber, neededAccount.Balance);
                                                db.SetCreditCash("dbo.Account", neededAccount.CardNumber, neededAccount.CreditCash);
                                                db.SetATMBalance("dbo.ATM", selectedATM.ID, selectedATM.ATMBalance);
                                                break;
                                            }
                                            else
                                            {
                                                Console.WriteLine("Сума погашення кредиту має бути цілим числом та більше за 0!");
                                            }
                                        }
                                    }
                                    else 
                                    {
                                        Console.WriteLine("Наразі у вас немає активного кредиту!");
                                    }
                                    break;
                                case 7:
                                    Console.WriteLine("Введіть номер картки отримувача: ");
                                    while (true)
                                    {
                                        string getter = Console.ReadLine();
                                        Account getterAccount = bank.FindAccount(getter);

                                        if (getterAccount != null && getterAccount != neededAccount)
                                        {
                                            Console.WriteLine("Введіть суму для переказу: ");
                                            int transferCash;
                                            while (true)
                                            {
                                                string input2 = Console.ReadLine();
                                                if (int.TryParse(input2, out transferCash) && transferCash > 0)
                                                {
                                                    neededAccount.Transfer(getterAccount, transferCash);
                                                    db.SetBalance("dbo.Account", neededAccount.CardNumber, neededAccount.Balance);
                                                    db.SetBalance("dbo.Account", getterAccount.CardNumber, getterAccount.Balance);
                                                    break;
                                                }
                                                else
                                                {
                                                    Console.WriteLine("Сума переказу має бути цілим числом та більше за 0!");
                                                }
                                            }
                                            break;
                                        }
                                        else if (getterAccount == neededAccount)
                                        {
                                            Console.WriteLine("Ви не можете відправити кошти на свою ж картку!");
                                            Console.WriteLine("Введіть номер картки отримувача: ");
                                        }
                                        else
                                        {
                                            Console.WriteLine("Картка отримувача не знайдена, спробуйте ще раз!");
                                            Console.WriteLine("Введіть номер картки отримувача: ");
                                        }
                                    }
                                    break;
                                case 8:
                                    Console.WriteLine("Введіть новий пароль з 4-ьох цифр:");
                                    string newPassword;
                                    while (true)
                                    {
                                        newPassword = Console.ReadLine();
                                        if(neededAccount.ChangePassword(newPassword, db, neededAccount))
                                        {
                                            break;
                                        }
                                    }
                                    break;
                                case 9:
                                    Process.Start(Process.GetCurrentProcess().MainModule.FileName);
                                    Environment.Exit(0);
                                    break;
                                default:
                                    Console.WriteLine("Такої опції не існує!");
                                    break;
                            }
                        }
                    }
                }
                while (neededAccount.Entered != true);
            }
            Console.ReadLine();
        }

        private static void Show_Message(string message)
        {
            Console.WriteLine(message);
        }

        private static void ShowMenu()
        {
            Console.WriteLine("Оберіть операцію:");
            Console.WriteLine("1. - Перевірити баланс.");
            Console.WriteLine("2. - Поповнити баланс.");
            Console.WriteLine("3. - Зняти кошти.");
            Console.WriteLine("4. - Перевірити наявність кредитних коштів.");
            Console.WriteLine("5. - Взяти кредит.");
            Console.WriteLine("6. - Погасити кредит.");
            Console.WriteLine("7. - Переказ за номером картки.");
            Console.WriteLine("8. - Змінити пароль.");
            Console.WriteLine("9. - Вихід у меню.");
        }

        private static void ShowAtmMenu(Bank bank)
        {
            Console.WriteLine("Оберіть банкомат, для здійснення операцій:");
            Console.WriteLine($"1. - Банкомат за адресою {bank.ATMs[0].Address}");
            Console.WriteLine($"2. - Банкомат за адресою {bank.ATMs[1].Address}");
            Console.WriteLine($"3. - Банкомат за адресою {bank.ATMs[2].Address}");
            Console.WriteLine("Введіть номер банкомату:");
        }
    }
}

