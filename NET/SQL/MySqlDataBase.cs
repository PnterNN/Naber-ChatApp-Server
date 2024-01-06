using K4os.Compression.LZ4.Encoders;
using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace JavaProject___Server.NET.SQL
{
    public class MySqlDataBase
    {
        private readonly string _connectionString;
        private readonly string _connectionString2;
        private readonly string _connectionString3;
        public MySqlDataBase()
        {
            _connectionString = "Server=46.31.77.173,3306;Database=javaproject;Uid=JavaProject;Pwd=JavaProject_ICU123;";
            _connectionString2 = "Server=46.31.77.173,3306;Database=javaproject_user;Uid=JavaProject;Pwd=JavaProject_ICU123;";
            _connectionString3 = "Server=46.31.77.173,3306;Database=javaproject_group;Uid=JavaProject;Pwd=JavaProject_ICU123;";
        }
        protected MySqlConnection GetConnection()
        {
            return new MySqlConnection(_connectionString);
        }
        protected MySqlConnection GetConnection2()
        {
            return new MySqlConnection(_connectionString2);
        }
        protected MySqlConnection GetConnection3()
        {
            return new MySqlConnection(_connectionString3);
        }


        public void createGroupStorage(string groupUID)
        {
            using (MySqlConnection conn = GetConnection3())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("CREATE TABLE IF NOT EXISTS group_" + groupUID + " (Username VARCHAR(255), UID VARCHAR(36), ImageSource VARCHAR(255), Message VARCHAR(1000), Time VARCHAR(255), FirstMessage BOOLEAN)", conn);
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                conn.Close();
            }
        }

        
        public void removeFriend(string username1, string username2)
        {
            using (MySqlConnection conn = GetConnection2())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("DELETE FROM friends_" + username1 + " WHERE Username = @Username", conn);
                cmd.Parameters.AddWithValue("@Username", username2);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }
        public void cancelFriendRequest(string username1, string username2)
        {
            using (MySqlConnection conn = GetConnection2())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("DELETE FROM friends_" + username1 + " WHERE Username = @Username", conn);
                cmd.Parameters.AddWithValue("@Username", username2);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }
        public void addFriend(string username1, string username2)
        {
            //change to state
            using (MySqlConnection conn = GetConnection2())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("UPDATE friends_" + username1 + " SET FriendState = @FriendState WHERE Username = @Username", conn);
                cmd.Parameters.AddWithValue("@FriendState", true);
                cmd.Parameters.AddWithValue("@Username", username2);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }
        public void addFriendRequest(string username1, string username2, bool ownrequest)
        {
            using (MySqlConnection conn = GetConnection2())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("INSERT INTO friends_" + username1 + " (Username, OwnRequest, FriendState) VALUES (@Username, @OwnRequest, @FriendState)", conn);
                cmd.Parameters.AddWithValue("@Username", username2);
                cmd.Parameters.AddWithValue("@OwnRequest", ownrequest);

                cmd.Parameters.AddWithValue("@FriendState", false);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }
        public void createFriendStorage(Client client)
        {
            using (MySqlConnection conn = GetConnection2())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("CREATE TABLE IF NOT EXISTS friends_" + client.Username + " (Username VARCHAR(255), OwnRequest BOOLEAN, FriendState BOOLEAN)", conn);
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                }
                conn.Close();
            }
        }
        public void createUserStorage(Client client) //messeges
        {
            using (MySqlConnection conn = GetConnection2())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("CREATE TABLE IF NOT EXISTS user_" + client.Username + " (Username VARCHAR(255), UID VARCHAR(36), ImageSource VARCHAR(255), Message VARCHAR(1000), Time VARCHAR(255), FirstMessage BOOLEAN, MessageUID VARCHAR(255))", conn);
                try
                {
                    cmd.ExecuteNonQuery();
                }
                catch(Exception e)
                {
                    Console.WriteLine(e);
                }
                conn.Close();
            }
        }
        public bool checkTweet(string username, string tweetUID)
        {
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT * FROM tweets WHERE UID = @UID", conn);
                cmd.Parameters.AddWithValue("@UID", tweetUID);
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (reader["Username"].ToString() == username)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                conn.Close();
            }
            return false;
        }
        public bool checkMessage(string user, string messageUID)
        {
            using (MySqlConnection conn = GetConnection2())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT * FROM user_" + user + " WHERE MessageUID = @MessageUID", conn);
                cmd.Parameters.AddWithValue("@MessageUID", messageUID);
                MySqlDataReader reader = cmd.ExecuteReader();
                while (reader.Read())
                {
                    if (reader["Username"].ToString() == user)
                    {
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                }
                conn.Close();
            }
            return false;
        }

        public void deleteTweet(string tweetUID)
        {
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("DELETE FROM tweets WHERE UID = @UID", conn);
                cmd.Parameters.AddWithValue("@UID", tweetUID);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        public void deleteMessage(string user, string messageUID)
        {
            using (MySqlConnection conn = GetConnection2())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("DELETE FROM user_" + user + " WHERE MessageUID = @MessageUID", conn);
                cmd.Parameters.AddWithValue("@MessageUID", messageUID);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        public void LikeTweet(string UID, string like)
        {
            String Like = "";
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT Likes FROM tweets WHERE UID=@UID", conn);
                cmd.Parameters.AddWithValue("@UID", UID);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        reader.Read();
                        Like = reader.GetString(0);
                        conn.Close();
                    }
                    else
                    {
                        conn.Close();
                        Like = "";
                    }
                }
            }
            using(MySqlConnection conn2 = GetConnection())
            {
                conn2.Open();
                MySqlCommand cmd = new MySqlCommand("UPDATE tweets SET Likes=@Likes WHERE UID=@UID", conn2);
                cmd.Parameters.AddWithValue("@UID", UID);
                cmd.Parameters.AddWithValue("@Likes", Like + " " + like);
                cmd.ExecuteNonQuery();
                conn2.Close();
            }
        }

        public void InsertTweet(string username, string UID, string imageSource, string message, string like, string time)
        {
            using(MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("INSERT INTO tweets (Username, UID, ImageSource, Message, Likes, Time) VALUES (@Username, @UID, @ImageSource, @Message, @Likes, @Time)", conn);
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@UID", UID);
                cmd.Parameters.AddWithValue("@ImageSource", imageSource);
                cmd.Parameters.AddWithValue("@Message", message);
                cmd.Parameters.AddWithValue("@Likes", like);
                cmd.Parameters.AddWithValue("@Time", time);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        public void InsertMessage(string user ,string username, string ContactUID, string imageSource, string message, string time, string fistMessage, string messageUID)
        {
            using (MySqlConnection conn = GetConnection2())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("INSERT INTO user_" + user + " (Username, UID, ImageSource, Message, Time, FirstMessage, MessageUID) VALUES (@Username, @UID, @ImageSource, @Message, @Time, @FirstMessage, @MessageUID)", conn);
                cmd.Parameters.AddWithValue("@Username", username);
                cmd.Parameters.AddWithValue("@UID", ContactUID);
                cmd.Parameters.AddWithValue("@ImageSource", imageSource);
                cmd.Parameters.AddWithValue("@Message", message);
                cmd.Parameters.AddWithValue("@Time", time);
                cmd.Parameters.AddWithValue("@FirstMessage", fistMessage);
                cmd.Parameters.AddWithValue("@MessageUID", messageUID);
                cmd.ExecuteNonQuery();
                conn.Close();
            }
        }

        public Dictionary<int, List<string>> getFriend(string username)
        {
            Dictionary<int, List<string>> friends = new Dictionary<int, List<string>>();
            using (MySqlConnection conn = GetConnection2())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT * FROM friends_" + username, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        List<string> infos;
                        while (reader.Read())
                        {
                            infos = new List<string>();
                            for (int i = 0; i < 3; i++)
                            {
                                infos.Add(reader.GetString(i));
                            }
                            friends.Add(friends.Count, infos);
                        }
                    }
                }
                conn.Close();
                return friends;
            }
        }

        public Dictionary<int, List<string>> getTweets()
        {
            Dictionary<int, List<string>> tweets = new Dictionary<int, List<string>>();
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT * FROM tweets", conn);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        List<string> infos;
                        while (reader.Read())
                        {
                            infos = new List<string>();
                            for (int i = 0; i < 6; i++)
                            {
                                infos.Add(reader.GetString(i));
                            }
                            tweets.Add(tweets.Count, infos);
                        }
                    }
                }
                conn.Close();
                return tweets;
            }
        }

        public Dictionary<int, List<string>> getMessages(Client client)
        {
            Dictionary<int, List<string>> messages = new Dictionary<int, List<string>>();
            //get all rows from user table
            using (MySqlConnection conn = GetConnection2())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT * FROM user_" + client.Username, conn);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        List<string> infos;
                        while (reader.Read())
                        {
                            infos = new List<string>();
                            for(int i = 0; i < 7; i++)
                            {
                                infos.Add(reader.GetString(i));
                            }
                            messages.Add(messages.Count, infos);
                        }
                    }
                }
                conn.Close();
                return messages;
            }
        }

        public string getMail(string UID)
        {
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT Email FROM users WHERE UID=@UID", conn);
                cmd.Parameters.AddWithValue("@UID", UID);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        reader.Read();
                        string email = reader.GetString(0);
                        conn.Close();
                        return email;
                    }
                    else
                    {
                        conn.Close();
                        return null;
                    }
                }
            }

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
                            string name = reader.GetString(0);
                            conn.Close();
                            return name;
                        }
                        else
                        {
                            conn.Close();
                            return null;
                        }
                    }
              }
        }

        public string getFriends(string email)
        {
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT FriendsUIDS FROM users WHERE Email=@Email", conn);
                cmd.Parameters.AddWithValue("@email", email);
                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        reader.Read();
                        string friends = reader.GetString(0);
                        conn.Close();
                        return friends;
                    }
                    else
                    {
                        conn.Close();
                        return "";
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

        public bool CheckRegisterUser(string username, string email)
        {
            using (MySqlConnection conn = GetConnection())
            {
                conn.Open();
                MySqlCommand cmd = new MySqlCommand("SELECT * FROM users WHERE Email=@email OR Username=@username", conn);
                cmd.Parameters.AddWithValue("@email", email);
                cmd.Parameters.AddWithValue("@username", username);
                try
                {
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
                catch(Exception e)
                {
                    Console.WriteLine(e);
                    return false;
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
                conn.Close();
            }
        }
    }
}
