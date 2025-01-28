using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Security.Policy;
using System.Security.Principal;
using Microsoft.Extensions.Configuration;

namespace ClassLibrary
{
    public class Account
    {
        public string CardNumber { get; set; }
        public string UserSurname { get; set; }
        public string UserName { get; set; }
        public string UserMiddleName { get; set; }
        public string Password { get; set; }
        public string Email { get; set; }
        public int Balance { get; set; }
        public bool Entered { get; set; }
        public int CreditCash { get; set; }
        public DateTime? CreditDate { get; set; }

        private Hashing hashing;
        private readonly IConfiguration configuration;

        public delegate void AccountStateHandler(string msg);
        public event AccountStateHandler Withdrawn;
        public event AccountStateHandler Added;
        public event AccountStateHandler BalanceChecked;
        public event AccountStateHandler Validated;
        public event AccountStateHandler Credit;
        public event AccountStateHandler Transfered;
        public event AccountStateHandler PasswordChanged;

        public Account(string cardNumber, string userSurname, string userName, string userMiddleName, string password,
            string email, int balance, DateTime? creditDate, int creditCash, IConfiguration configuration)
        {
            CardNumber = cardNumber;
            UserSurname = userSurname;
            UserName = userName;
            UserMiddleName = userMiddleName;
            Password = password;
            Email = email;
            Balance = balance;
            CreditDate = creditDate;
            CreditCash = creditCash;
            this.configuration = configuration;

            string key = configuration["Encryption:Key"];
            string iv = configuration["Encryption:IV"];
            byte[] keyBytes = Convert.FromBase64String(key);
            byte[] ivBytes = Convert.FromBase64String(iv);
            hashing = new Hashing(keyBytes, ivBytes);
        }

        public int CurrentBalanceCheck
        {
            get
            {
                if (BalanceChecked != null) BalanceChecked($"Поточний баланс: {Balance}");
                return Balance;
            }
        }

        public void Put(int cash)
        {
            Balance += cash;
            if (Added != null) Added($"Ваш рахунок поповнено на {cash}");
            EmailSender("Поповнення рахунку.", $"Ваш рахунок поповнено на суму {cash}");
        }

        public bool Withdraw(int cash)
        {
            if (Balance >= cash)
            {
                Balance -= cash;
                if (Withdrawn != null) Withdrawn($"З вашого рахунку знято {cash}");
                EmailSender("Витрата коштів.", $"З вашого рахунку витрачено {cash}");
                return true;
            }
            else
            {
                if (Withdrawn != null) Withdrawn("На вашому рахунку недостатньо коштів!");
                return false;
            }
        }
        public void Validation(string password)
        {
            if (BCrypt.Net.BCrypt.EnhancedVerify(password, Password))
            {
                Entered = true;
            }
            else
            {
                Entered = false;
                if (Validated != null) Validated("Здається ви не правильно ввели PIN-код, спробуйте ще!");
            }

        }

        public void TakeAnATMCredit(int cash, AutomatedTellerMachine atm)
        {
            if (atm.WithdrawATM(cash))
            {
                CreditCash = cash;
                Balance += CreditCash;
                EmailSender("Кредит.", $"Вам було надано кредит у розмірі {CreditCash}");
                if (Credit != null) Credit($"Вам надано кредит у розмірі {CreditCash}");

            }

        }

        public void CreditCheck()
        {
            if (CreditCash == 0)
            {
                if (Credit != null) Credit("Наразі на вас немає ніяких кредитів!");
            }
            else
            {
                if (Credit != null) Credit($"Ваш кредит становить {CreditCash}");
            }
        }

        public void CreditDebt()
        {
            if (CreditCash > 0 && CreditDate != null)
            {
                int monthsLate = (int)((DateTime.Now - CreditDate.Value).TotalDays / 30);

                if (monthsLate > 0)
                {
                    double monthlyRate = 0.14 / 12;
                    double debt = CreditCash * monthlyRate * monthsLate;
                    CreditCash += (int)debt;
                }
            }
        }


        public void PayCredit(int cash, AutomatedTellerMachine atm, DataBase db, Account neededAccount)
        {
            if (cash >= CreditCash)
            {
                Balance -= CreditCash;
                atm.PutCashATM(CreditCash);
                CreditCash = 0;
                EmailSender("Кредит.", $"Ваш кредит повністю погашений!");
                db.SetDate("dbo.Account", neededAccount.CardNumber, neededAccount.CreditDate);
                if (Credit != null) Credit("Ваш кредит повністю погашений!");
            }
            else
            {
                Balance -= cash;
                atm.PutCashATM(cash);
                CreditCash -= cash;
                EmailSender("Кредит.", $"Ваш кредит погашений на {cash}, тепер ваш кредит становить {CreditCash}");
                if (Credit != null) Credit($"Ваш кредит погашений на {cash}, тепер ваш кредит становить {CreditCash}");
            }
        }

        public void Transfer(Account account, int cash)
        {
            if (cash > Balance)
            {
                if (Transfered != null) Transfered("На вашому рахунку не вистачає коштів!");
                return;
            }
            Balance -= cash;
            account.Balance += cash;
            EmailSender("Переказ.", $"З вашої карти відбувся грошовий переказ на карту, {account.CardNumber} у розмірі - {cash}");
            if (Transfered != null) Transfered("Операція проведена успішно");
        }

        private void EmailSender(string topic, string body)
        {
            string fromEmail = configuration["EmailSettings:FromEmail"];
            string fromPassword = hashing.Decrypt(configuration["EmailSettings:FromPassword"]);
            string smtpServer = configuration["EmailSettings:SmtpServer"];
            int smtpPort = int.Parse(configuration["EmailSettings:SmtpPort"]);

            var smtpClient = new SmtpClient(smtpServer)
            {
                Port = smtpPort,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromEmail, fromPassword),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };

            smtpClient.Send(fromEmail, Email, topic, body);
        }

        public bool ChangePassword(string newPassword, DataBase db, Account neededAccount)
        {
            if(newPassword.Length == 4 && newPassword.All(char.IsDigit))
            {
                Password = BCrypt.Net.BCrypt.EnhancedHashPassword(newPassword, 13);
                db.SetNewPassword("dbo.Account", neededAccount.CardNumber, neededAccount.Password);
                EmailSender("Безпека!!!", $"Ваш пароль було змінено! Новий пароль: {newPassword}");
                if (PasswordChanged != null) PasswordChanged("Пароль змінено успішно!");
                return true;
            }
            else
            {
                if (PasswordChanged != null) PasswordChanged("Пароль має бути з 4 чисел!");
                EmailSender("Безпека!!!", "Ваш пароль намагалися змінити, якщо це не ви, то замініть пароль на новий");
                return false;
            }
        }
    }

}
