using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JavaProject___Server.NET.SQL
{
    public class MySqlDataBase
    {
        private readonly string _connectionString;
        public MySqlDataBase()
        {
            _connectionString = "Server=46.31.77.173,3306;Database=javaproject;Uid=JavaProject;Pwd=JavaProject_ICU;";
        }
        protected MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }
        public string getName(string email)
        {
              using (MySqlConnection conn = GetConnection())
                {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT Username FROM users WHERE Email=@Email", conn);
                cmd.Parameters.AddWithValue("@Email", email);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        reader.Read();
                        string username = reader.GetString(0);
                        conn.Close();
                        return username;
                    }
                    else
                    {
                        conn.Close();
                        return null;
                    }
                }
            }
        }
        public string getUID(string email)
        {
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT UID FROM users WHERE Email=@Email", conn);
                cmd.Parameters.AddWithValue("@email", email);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        reader.Read();
                        string uid = reader.GetString(0);
                        conn.Close();
                        return uid;
                    }
                    else
                    {
                        Console.WriteLine("test2");
                        conn.Close();
                        return null;
                    }
                }
            }
        }
        public bool CheckLoginUser(string email, string password)
        {
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE Email=@email AND Password=@password", conn);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@password", password);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
            }
        }

        public bool CheckRegisterUser(string username, string email)
        {
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE Username=@username AND Email=@email", conn);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@email", email);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        conn.Close();
                        return true;
                    }
                    else
                    {
                        conn.Close();
                        return false;
                    }
                }
            }
        }
        public void InsertUser(string username, string uid, string email, string password)
        {
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("INSERT INTO users (Username, UID, Email, Password) VALUES (@username, @uid, @email, @password)", conn);
                cmd.Parameters.AddWithValue("@username", username);
                cmd.Parameters.AddWithValue("@uid", uid);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@password", password);
                cmd.ExecuteNonQuery();
            }
        }
    }
}
