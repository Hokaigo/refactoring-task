using Microsoft.Extensions.Configuration;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace ClassLibrary
{
    public class DataBase
    {
        private string ConnectionString { set; get; }
        private readonly IConfiguration configuration;

        public DataBase(string connectionString, IConfiguration configuration)
        {
            ConnectionString = connectionString;
            this.configuration = configuration;
        }

        public List<Account> GetAccounts(string tableName)
        {
            List<Account> accounts = new List<Account>();
            string query = $"SELECT cardNumber, userSurname, userName, userMiddleName, password, email, balance, creditCash, gotCredit FROM {tableName}";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read()) 
                    {
                        DateTime? creditDate = null;
                        if (!reader.IsDBNull(8)) 
                        {
                            creditDate = reader.GetDateTime(8);
                        }
                        accounts.Add(new Account(reader.GetString(0), reader.GetString(1), reader.GetString(2), reader.GetString(3), reader.GetString(4),   
                            reader.GetString(5), reader.GetInt32(6), creditDate, reader.GetInt32(7), configuration));
                    }
                }
            }
            return accounts;
        }



        public List<AutomatedTellerMachine> GetATM(string tableName)
        {
            List<AutomatedTellerMachine> ATMs = new List<AutomatedTellerMachine>();
            string query = $"SELECT atmID, balance, address FROM {tableName}";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        ATMs.Add(new AutomatedTellerMachine(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2)));
                    }
                }
            }
            return ATMs;

        }


        public void SetDate(string tableName, string cardNumber, DateTime? date)
        {
            string checkQuery = $"SELECT gotCredit FROM {tableName} WHERE cardNumber = @cardNumber";
            string updateQuery = $"UPDATE {tableName} SET gotCredit = @Date WHERE cardNumber = @cardNumber";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();

                using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@cardNumber", cardNumber);
                    object result = checkCommand.ExecuteScalar();

                    using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                    {
                        if (result != DBNull.Value && result != null)
                        {
                            if (date != null)
                            {
                                updateCommand.Parameters.AddWithValue("@Date", date.Value);
                            }
                            else
                            {
                                updateCommand.Parameters.AddWithValue("@Date", DBNull.Value);
                            }
                        }
                        else
                        {
                            if (date != null)
                            {
                                updateCommand.Parameters.AddWithValue("@Date", date.Value);
                            }
                            else
                            {
                                updateCommand.Parameters.AddWithValue("@Date", DBNull.Value);
                            }
                        }

                        updateCommand.Parameters.AddWithValue("@cardNumber", cardNumber);
                        updateCommand.ExecuteNonQuery();
                    }
                }
            }
        }



        public void SetBalance(string tableName, string cardNumber, int balance)
        {
            string checkQuery = $"SELECT balance FROM {tableName} WHERE cardNumber = @cardNumber";
            string updateQuery = $"UPDATE {tableName} SET balance = @Balance WHERE cardNumber = @cardNumber";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@cardNumber", cardNumber);
                    object result = checkCommand.ExecuteScalar();
                    using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@Balance", balance);
                        updateCommand.Parameters.AddWithValue("@cardNumber", cardNumber);
                        updateCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        public void SetCreditCash(string tableName, string cardNumber, int creditCash)
        {
            string checkQuery = $"SELECT creditCash FROM {tableName} WHERE cardNumber = @cardNumber";
            string updateQuery = $"UPDATE {tableName} SET creditCash = @creditCash WHERE cardNumber = @cardNumber";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@cardNumber", cardNumber);
                    object result = checkCommand.ExecuteScalar();
                    using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@creditCash", creditCash);
                        updateCommand.Parameters.AddWithValue("@cardNumber", cardNumber);
                        updateCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        public void SetATMBalance(string tableName, int ID, int balance) 
        {
            string checkQuery = $"SELECT balance FROM {tableName} WHERE atmID = @id";
            string updateQuery = $"UPDATE {tableName} SET balance = @Balance WHERE atmID = @id";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@id", ID);
                    object result = checkCommand.ExecuteScalar();
                    using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@Balance", balance);
                        updateCommand.Parameters.AddWithValue("@id", ID);
                        updateCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        public void SetNewPassword(string tableName, string cardNumber, string password)
        {
            string checkQuery = $"SELECT password FROM {tableName} WHERE cardNumber = @cardNumber";
            string updateQuery = $"UPDATE {tableName} SET password = @Password WHERE cardNumber = @cardNumber";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                {
                    checkCommand.Parameters.AddWithValue("@cardNumber", cardNumber);
                    object result = checkCommand.ExecuteScalar();
                    using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                    {
                        updateCommand.Parameters.AddWithValue("@Password", password);
                        updateCommand.Parameters.AddWithValue("@cardNumber", cardNumber);
                        updateCommand.ExecuteNonQuery();
                    }
                }
            }
        }

        public void SetDateToNull(string tableName, string cardNumber)
        {
            string updateQuery = $"UPDATE {tableName} SET gotCredit = NULL WHERE cardNumber = @cardNumber";

            using (SqlConnection connection = new SqlConnection(ConnectionString))
            {
                connection.Open();
                using (SqlCommand updateCommand = new SqlCommand(updateQuery, connection))
                {
                    updateCommand.Parameters.AddWithValue("@cardNumber", cardNumber);
                    updateCommand.ExecuteNonQuery();
                }
            }
        }
    }
}
