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
using System.Runtime;

namespace ConsoleApp
{
    public enum Operation
    {
        CheckBalance = 1,
        Deposit,
        Withdraw,
        CheckCredit,
        TakeCredit,
        PayCredit,
        Transfer,
        ChangePassword,
        Exit
    }

    internal class Program
    {
        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.Unicode;
            Console.InputEncoding = Encoding.Unicode;

            var configuration = LoadConfiguration();
            string connectionString = GetConnectionString(configuration);
            DataBase db = new DataBase(connectionString, configuration);

            Bank bank = InitializeBank(db);

            AutomatedTellerMachine selectedATM = SelectATM(bank);

            Account neededAccount = SelectAccount(bank);

            PerformOperations(neededAccount, selectedATM, db, bank);
        }

        private static string GetConnectionString(IConfiguration configuration)
        {
            string serverAddress = configuration["DbSettings:ServerAddress"];
            string databaseName = configuration["DbSettings:DatabaseName"];
            string userId = configuration["DbSettings:UserId"];
            string encryptedPassword = configuration["DbSettings:EncryptedPassword"];

            string key = configuration["Encryption:Key"];
            string iv = configuration["Encryption:IV"];
            byte[] keyBytes = Convert.FromBase64String(key);
            byte[] ivBytes = Convert.FromBase64String(iv);

            Hashing hashing = new Hashing(keyBytes, ivBytes);
            string decryptedPassword = hashing.Decrypt(encryptedPassword);

            return $"Data Source={serverAddress};Initial Catalog={databaseName};User ID={userId};Password={decryptedPassword};";
        }

        private static AutomatedTellerMachine SelectATM(Bank bank)
        {
            AutomatedTellerMachine selectedATM = null;
            do
            {
                ShowAtmMenu(bank);
                int atmID = GetValidInput("Введіть номер банкомату:", input => bank.FindATM(input) != null, "Здається ви неправильно ввели номер банкомату, спробуйте ще раз!");
                selectedATM = bank.FindATM(atmID);
            } while (selectedATM == null);

            return selectedATM;
        }
        private static Account SelectAccount(Bank bank)
        {
            Account neededAccount = null;
            do
            {
                Console.WriteLine("Введіть номер картки:");
                string cardNumber = Console.ReadLine();
                neededAccount = bank.FindAccount(cardNumber);
                if (neededAccount == null)
                {
                    Console.WriteLine("Невірний номер картки. Спробуйте ще раз.");
                }
            } while (neededAccount == null);
            return neededAccount;
        }

        private static int GetValidInput(string queryText, Predicate<int> validation, string errorMsg)
        {
            int result;
            do
            {
                Console.WriteLine(queryText);
                string input = Console.ReadLine();
                if (int.TryParse(input, out result) && validation(result))
                    break;
                else
                    Console.WriteLine(errorMsg);
            } while (true);

            return result;
        }

        private static IConfiguration LoadConfiguration()
        {
            return new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("appsettings.json").Build();
        }

        private static Bank InitializeBank(DataBase db)
        {
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

            for (int i = 0; i < bank.ATMs.Count; i++)
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

            for (int i = 0; i < bank.Accounts.Count; i++)
            {
                if (bank.Accounts[i].CreditDate != null)
                {
                    bank.Accounts[i].CreditDebt();
                }
            }

            return bank;
        }


        private static void PerformOperations(Account neededAccount, AutomatedTellerMachine selectedATM, DataBase db, Bank bank)
        {
            string password;
            do
            {
                Console.WriteLine("Введіть пін-код:");
                password = Console.ReadLine();
                neededAccount.Validation(password);
            } while (!neededAccount.Entered);

            while (true)
            {
                MenuDisplayer.ShowMenu();
                int option = GetValidInput("Оберіть опцію операції:", input => Enum.IsDefined(typeof(Operation), input), "Такої опції не існує");

                switch ((Operation)option)
                {
                    case Operation.CheckBalance:
                        int balance = neededAccount.CurrentBalanceCheck;
                        break;
                    case Operation.Deposit:
                        DepositFunds(neededAccount, selectedATM, db);
                        break;
                    case Operation.Withdraw:
                        WithdrawFunds(neededAccount, selectedATM, db);
                        break;
                    case Operation.CheckCredit:
                        neededAccount.CreditCheck();
                        break;
                    case Operation.TakeCredit:
                        TakeCredit(neededAccount, selectedATM, db);
                        break;
                    case Operation.PayCredit:
                        PayCredit(neededAccount, selectedATM, db);
                        break;
                    case Operation.Transfer:
                        TransferFunds(neededAccount, bank, db);
                        break;
                    case Operation.ChangePassword:
                        ChangePassword(neededAccount, db);
                        break;
                    case Operation.Exit:
                        Environment.Exit(0);
                        break;
                    default:
                        Console.WriteLine("Такої опції не існує!");
                        break;
                }
            }
        }

        private static void UpdateBalance(DataBase db, string accountTable, string atmTable, Account account, AutomatedTellerMachine atm)
        {
            db.SetBalance(accountTable, account.CardNumber, account.Balance);
            db.SetATMBalance(atmTable, atm.ID, atm.ATMBalance);
        }


        private static void DepositFunds(Account neededAccount, AutomatedTellerMachine selectedATM, DataBase db)
        {
            int putCash = GetValidInput("Введіть суму поповнення: ", input => input > 0,
                "Сума поповнення має бути цілим числом та більше за нуль!");

            neededAccount.Put(putCash);
            selectedATM.PutCashATM(putCash);

            UpdateBalance(db, "dbo.Account", "dbo.ATM", neededAccount, selectedATM);
        }

        private static void WithdrawFunds(Account neededAccount, AutomatedTellerMachine selectedATM, DataBase db)
        {
            int withdrawCash = GetValidInput("Введіть суму, яку хочете зняти: ", input => input > 0, 
                "Сума виводу має бути цілим числом та більше за нуль!");

            if (selectedATM.WithdrawATM(withdrawCash))
            {
                if (neededAccount.Withdraw(withdrawCash))
                {
                    UpdateBalance(db, "dbo.Account", "dbo.ATM", neededAccount, selectedATM);
                }
                else
                {
                    selectedATM.PutCashATM(withdrawCash);
                    db.SetATMBalance("dbo.ATM", selectedATM.ID, selectedATM.ATMBalance);
                }
            }
        }

        private static void TakeCredit(Account neededAccount, AutomatedTellerMachine selectedATM, DataBase db)
        {
            if (neededAccount.CreditCash <= 0)
            {
                const int minThresholdCredit = 1000;
                const int maxThresholdCredit = 15000;

                int creditCash = GetValidInput("Введіть суму кредиту від 1000 до 15000: ", input => input >= minThresholdCredit && input <= maxThresholdCredit,
                    "Сума кредиту має бути цілим числом та від 1000 до 15000!");

                neededAccount.TakeAnATMCredit(creditCash, selectedATM);
                DateTime gotCreditDate = DateTime.Now;
                db.SetDate("dbo.Account", neededAccount.CardNumber, gotCreditDate);
                db.SetCreditCash("dbo.Account", neededAccount.CardNumber, neededAccount.CreditCash);
                UpdateBalance(db, "dbo.Account", "dbo.ATM", neededAccount, selectedATM);
            }
            else
                Console.WriteLine("Ви не можете взяти кредит, сплатіть попередній кредит!");
        }

        private static void PayCredit(Account neededAccount, AutomatedTellerMachine selectedATM, DataBase db)
        {
            if (neededAccount.CreditCash > 0)
            {
                int payCreditCash = GetValidInput("Введіть суму для погашення кредиту: ", input => input > 0,
                    "Сума погашення кредиту має бути цілим числом та більше за 0!");

                neededAccount.PayCredit(payCreditCash, selectedATM, db, neededAccount);
                db.SetCreditCash("dbo.Account", neededAccount.CardNumber, neededAccount.CreditCash);
                UpdateBalance(db, "dbo.Account", "dbo.ATM", neededAccount, selectedATM);
            }
            else
                Console.WriteLine("Наразі у вас немає активного кредиту!");
        }

        private static void TransferFunds(Account senderAccount, Bank bank, DataBase db)
        {
            while (true)
            {
                Console.WriteLine("Введіть номер картки отримувача: ");
                string getterCardNumber = Console.ReadLine();
                Account getterAccount = bank.FindAccount(getterCardNumber);

                if (getterAccount != null && getterAccount != senderAccount)
                {
                    int transferAmount = GetValidInput("Введіть суму для переказу: ", input => input > 0 && input <= senderAccount.Balance,
                        "Сума переказу має бути цілим числом, більше нуля і не перевищувати ваш баланс!"
                    );

                    senderAccount.Withdraw(transferAmount);
                    getterAccount.Put(transferAmount);

                    db.SetBalance("dbo.Account", senderAccount.CardNumber, senderAccount.Balance);
                    db.SetBalance("dbo.Account", getterAccount.CardNumber, getterAccount.Balance);

                    Console.WriteLine("Переказ успішно здійснено!");
                    break;
                }
                else if (getterAccount == senderAccount)
                {
                    Console.WriteLine("Ви не можете відправити кошти на свою ж картку!");
                }
                else
                {
                    Console.WriteLine("Картка отримувача не знайдена, спробуйте ще раз!");
                }
            }
        }

        private static void ChangePassword(Account neededAccount, DataBase db)
        {
            Console.WriteLine("Введіть новий пароль з 4-ьох цифр:");
            string newPassword;
            while (true)
            {
                newPassword = Console.ReadLine();
                if (neededAccount.ChangePassword(newPassword, db, neededAccount))
                {
                    break;
                }
            }
        }

        private static void Show_Message(string message)
        {
            Console.WriteLine(message);
        }

        private static void ShowAtmMenu(Bank bank)
        {
            Console.WriteLine("Оберіть банкомат для здійснення операцій:");
            for (int i = 0; i < bank.ATMs.Count; i++)
            {
                Console.WriteLine($"{i + 1}. - Банкомат за адресою {bank.ATMs[i].Address}");
            }
        }
    }
}

