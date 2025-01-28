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
            EmailSender("yourbank25@gmail.com", hashing.Decrypt("asdXuw/7xlkwpp94ACleFUAHh2xr6yWteDegYQaXZl4="), "Поповнення рахунку.",$"Ваш рахунок поповнено на суму {cash}", Email);
        }

        public bool Withdraw(int cash)
        {
            if (Balance >= cash)
            {
                Balance -= cash;
                if (Withdrawn != null) Withdrawn($"З вашого рахунку знято {cash}");
                EmailSender("yourbank25@gmail.com", hashing.Decrypt("asdXuw/7xlkwpp94ACleFUAHh2xr6yWteDegYQaXZl4="), "Витрата коштів.", $"З вашого рахунку витрачено {cash}", Email);
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
                EmailSender("yourbank25@gmail.com", hashing.Decrypt("asdXuw/7xlkwpp94ACleFUAHh2xr6yWteDegYQaXZl4="), "Кредит.", $"Вам було надано кредит у розмірі {CreditCash}", Email);
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
                EmailSender("yourbank25@gmail.com", hashing.Decrypt("asdXuw/7xlkwpp94ACleFUAHh2xr6yWteDegYQaXZl4="), "Кредит.", $"Ваш кредит повністю погашений!", Email);
                db.SetDate("dbo.Account", neededAccount.CardNumber, neededAccount.CreditDate);
                if (Credit != null) Credit("Ваш кредит повністю погашений!");
            }
            else
            {
                Balance -= cash;
                atm.PutCashATM(cash);
                CreditCash -= cash;
                EmailSender("yourbank25@gmail.com", hashing.Decrypt("asdXuw/7xlkwpp94ACleFUAHh2xr6yWteDegYQaXZl4="), "Кредит.", $"Ваш кредит погашений на {cash}, " +
                    $"тепер ваш кредит становить {CreditCash}", Email);
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
            EmailSender("yourbank25@gmail.com", hashing.Decrypt("asdXuw/7xlkwpp94ACleFUAHh2xr6yWteDegYQaXZl4="), "Переказ.", $"З вашої карти відбувся грошовий переказ на карту" +
                $" {account.CardNumber} у розмірі - {cash}", Email);
            if (Transfered != null) Transfered("Операція проведена успішно");
        }
        public void EmailSender(string FromEmail, string FromPassword, string Topic, string messageText, string ToEmail)
        {
            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(FromEmail, FromPassword),
                EnableSsl = true,
                DeliveryMethod = SmtpDeliveryMethod.Network
            };
            smtpClient.Send(FromEmail, ToEmail, Topic, messageText);

        }

        public bool ChangePassword(string newPassword, DataBase db, Account neededAccount)
        {
            if(newPassword.Length == 4 && newPassword.All(char.IsDigit))
            {
                Password = BCrypt.Net.BCrypt.EnhancedHashPassword(newPassword, 13);
                db.SetNewPassword("dbo.Account", neededAccount.CardNumber, neededAccount.Password);
                EmailSender("yourbank25@gmail.com", hashing.Decrypt("asdXuw/7xlkwpp94ACleFUAHh2xr6yWteDegYQaXZl4="), "Безпека!!!", $"Ваш пароль було змінено! Новий пароль: " +
                    $"{newPassword}", Email);
                if (PasswordChanged != null) PasswordChanged("Пароль змінено успішно!");
                return true;
            }
            else
            {
                if (PasswordChanged != null) PasswordChanged("Пароль має бути з 4 чисел!");
                EmailSender("yourbank25@gmail.com", hashing.Decrypt("asdXuw/7xlkwpp94ACleFUAHh2xr6yWteDegYQaXZl4="), "Безпека!!!", $"Ваш пароль намагалися змінити, " +
                    $"якщо це не ви, то замініть пароль на новий", Email);
                return false;
            }
        }
    }

}
