using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Security.Permissions;

namespace ServerUDP
{
    class DB
    {
        MySqlConnection connection = new MySqlConnection("server=localhost;port=3306;username=root;password=root;database=besdushka");


        #region Юзер и юзерское

        public Boolean CheckUniqueLogin(string lgn)
        {
            DataTable table = new DataTable();
            MySqlDataAdapter adapter = new MySqlDataAdapter();
            MySqlCommand command = new MySqlCommand("SELECT * FROM users WHERE login = @lgn", connection);
            command.Parameters.Add("@lgn", MySqlDbType.VarChar).Value = lgn;
            adapter.SelectCommand = command;
            adapter.Fill(table);

            if (table.Rows.Count > 0)
            {
                return false;
            }
            else
                return true;
        }

        public void CreateUser(string login, string _password, string _username = "NULL")
        {
            MySqlCommand command = new MySqlCommand("INSERT INTO users (login, password, username) VALUES (@login, @password, @username)", connection);

            command.Parameters.Add("@login", MySqlDbType.VarChar).Value = login;
            command.Parameters.Add("@password", MySqlDbType.VarChar).Value = _password;
            command.Parameters.Add("@username", MySqlDbType.VarChar).Value = _username;

            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();

            //command = new MySqlCommand("SELECT mysql_insert_id() FROM users", connection);
            //object userID = command.ExecuteScalar();
            //return Convert.ToInt32(userID);
        }

        public Boolean CheckUserPassword(string login, string _password)
        {
            DataTable table = new DataTable();
            MySqlDataAdapter adapter = new MySqlDataAdapter();
            MySqlCommand command = new MySqlCommand("SELECT * FROM users WHERE login = @login AND password = @password", connection);
            command.Parameters.Add("@login", MySqlDbType.VarChar).Value = login;
            command.Parameters.Add("@password", MySqlDbType.VarChar).Value = _password;
            adapter.SelectCommand = command;
            adapter.Fill(table);

            if (table.Rows.Count > 0)
            {
                return true;
            }
            else
                return false;
        }

        public List<int> GetUserRoomList(int userID)
        {
            List<int> userRoomList = new List<int>();

            MySqlCommand command = new MySqlCommand("SELECT roomID FROM `users in rooms` WHERE userID = @userID", connection);
            command.Parameters.Add("@userID", MySqlDbType.Int32).Value = userID;

            connection.Open();
            MySqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                userRoomList.Add(reader.GetInt32(0));
            }
            connection.Close();

            return userRoomList;
        }

        public object[] GetUserInfo(string login)
        {
            object[] info = new object[2];
            MySqlCommand command = new MySqlCommand("SELECT * FROM `users` WHERE login = @login", connection);
            command.Parameters.Add("@login", MySqlDbType.VarChar).Value = login;

            connection.Open();
            MySqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                info[0] = reader.GetValue(0);
                info[1] = reader.GetValue(3);
            }
            connection.Close();

            return info;
        }



        #endregion

        #region Чат и всё, что с ним связано

        public void ChatLogAdd(int roomID, string msg, DateTime dateTime, int userID, string username)
        {
            MySqlCommand command = new MySqlCommand("INSERT INTO `chat logs` (roomID, `chat message`, userID, username, datetime) VALUES (@roomID, @msg, @userID, @username, @dtt)", connection);

            command.Parameters.Add("@roomID", MySqlDbType.Int32).Value = roomID;
            command.Parameters.Add("@msg", MySqlDbType.Text).Value = msg;
            command.Parameters.Add("@userID", MySqlDbType.VarChar).Value = userID;
            command.Parameters.Add("@username", MySqlDbType.VarChar).Value = username;
            command.Parameters.Add("@dtt", MySqlDbType.DateTime).Value = dateTime;

            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();
        }

        public object[,] GetChatLogs(int roomID, int count, int offset)
        {
            object[,] info = new object[count+1, 3];
            
            MySqlCommand command = new MySqlCommand($@"SELECT * FROM `chat logs` WHERE roomID = @roomID ORDER BY datetime DESC LIMIT {count} OFFSET {offset}", connection);
            command.Parameters.Add("@roomID", MySqlDbType.Int32).Value = roomID;

            connection.Open();
            MySqlDataReader reader = command.ExecuteReader();
            for (int i = 1; reader.Read(); i++)
            {
                info[0, 0] = i;
                info[i, 0] = reader.GetValue(2);
                info[i, 1] = reader.GetValue(4);
                info[i, 2] = reader.GetValue(5);
            }
            connection.Close();

            return info;
        }

        #endregion

        #region Комнаты и всё, что с ними связано

        public int CreateRoomReturnID(int userID, string name, string description, string worldID = null)
        {
            MySqlCommand command = null;
            object roomID = 0;

            if (worldID != null)
            {
                command = new MySqlCommand("INSERT INTO rooms (name, description, worldID) VALUES (@name, @description, @worldID)", connection);
                command.Parameters.Add("@name", MySqlDbType.VarChar).Value = name;
                command.Parameters.Add("@description", MySqlDbType.Text).Value = description;
                command.Parameters.Add("@worldID", MySqlDbType.Int32).Value = Convert.ToInt32(worldID);


                MySqlCommand _command = new MySqlCommand($@"SELECT id FROM rooms WHERE worldID = @newWorldID ORDER BY id DESC LIMIT 1", connection);
                _command.Parameters.Add("@newWorldID", MySqlDbType.Int32).Value = Convert.ToInt32(worldID);
                connection.Open();
                _command.ExecuteNonQuery();
                roomID = command.ExecuteScalar();
                connection.Close();
            }
            else
            {
                command = new MySqlCommand("INSERT INTO worlds (name, creatorID, private) VALUES (@name, @creatorID, @isPrivate)", connection);
                command.Parameters.Add("@name", MySqlDbType.VarChar).Value = name;
                command.Parameters.Add("@creatorID", MySqlDbType.Int32).Value = userID;
                command.Parameters.Add("@isPrivate", MySqlDbType.Int32).Value = true;

                MySqlCommand _command = new MySqlCommand($@"SELECT id FROM worlds WHERE creatorID = @userID ORDER BY id DESC LIMIT 1", connection);
                _command.Parameters.Add("@userID", MySqlDbType.Int32).Value = userID;
                connection.Open();
                command.ExecuteNonQuery();
                object newWorldID = _command.ExecuteScalar();
                connection.Close();

                MySqlCommand Command = new MySqlCommand("INSERT INTO rooms (name, description, worldID) VALUES (@name, @description, @worldID)", connection);
                Command.Parameters.Add("@name", MySqlDbType.VarChar).Value = name;
                Command.Parameters.Add("@description", MySqlDbType.Text).Value = description;
                Command.Parameters.Add("@worldID", MySqlDbType.Int32).Value = Convert.ToInt32(newWorldID);

                command = new MySqlCommand($@"SELECT id FROM rooms WHERE worldID = @newWorldID ORDER BY id DESC LIMIT 1", connection);
                command.Parameters.Add("@newWorldID", MySqlDbType.Int32).Value = Convert.ToInt32(newWorldID);
                connection.Open();
                Command.ExecuteNonQuery();
                roomID = command.ExecuteScalar();
                connection.Close();
            }

            command = new MySqlCommand("INSERT INTO `users in rooms` (userID, roomID, status) VALUES (@userID, @roomID, @status)", connection);
            command.Parameters.Add("@userID", MySqlDbType.Int32).Value = Convert.ToInt32(userID);
            command.Parameters.Add("@roomID", MySqlDbType.Int32).Value = Convert.ToInt32(roomID);
            command.Parameters.Add("@status", MySqlDbType.VarChar).Value = "OWNER";

            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();

            return Convert.ToInt32(roomID);
        }

        public Boolean DoesRoomExist(int roomID)
        {
            DataTable table = new DataTable();
            MySqlDataAdapter adapter = new MySqlDataAdapter();
            MySqlCommand command = new MySqlCommand("SELECT * FROM rooms WHERE id = @roomID", connection);
            command.Parameters.Add("@roomID", MySqlDbType.VarChar).Value = roomID;
            adapter.SelectCommand = command;
            adapter.Fill(table);

            if (table.Rows.Count > 0)
            {
                return true;
            }
            else
                return false;
        }

        public void EnterTheRoom(int roomID, int userID)
        {
            MySqlCommand command = new MySqlCommand("INSERT INTO `users in rooms` (userID, roomID, status) VALUES (@userID, @roomID, @status)", connection);
            command.Parameters.Add("@userID", MySqlDbType.Int32).Value = Convert.ToInt32(userID);
            command.Parameters.Add("@roomID", MySqlDbType.Int32).Value = roomID;
            command.Parameters.Add("@status", MySqlDbType.VarChar).Value = "ACTIVE";
            connection.Open();
            command.ExecuteNonQuery();
            connection.Close();
        }

        public object[] GetRoomInfo(int roomID)
        {
            object[] info = new object[3];
            MySqlCommand command = new MySqlCommand("SELECT * FROM `rooms` WHERE id = @roomID", connection);
            command.Parameters.Add("@roomID", MySqlDbType.Int32).Value = roomID;

            connection.Open();
            MySqlDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                info[0] = reader.GetValue(1);
                info[1] = reader.GetValue(2);
                info[2] = reader.GetValue(3);
            }
            connection.Close();

            return info;
        }

        public object[,] GetRoomList(int count, int offset = 0)
        {
            object[,] info = new object[count, 4];
            MySqlCommand command = new MySqlCommand($@"SELECT * FROM `rooms` LIMIT {count} OFFSET {offset}", connection);

            connection.Open();
            MySqlDataReader reader = command.ExecuteReader();

            for (int i = 0; reader.Read(); i++)
            {
                info[i, 0] = reader.GetValue(0);
                info[i, 1] = reader.GetValue(1);
                info[i, 2] = reader.GetValue(2);
                info[i, 3] = reader.GetValue(3);
            }

            connection.Close();

            return info;
        }

        public bool LeaveRoomReturnWorldPrivacy(int userID, int roomID)
        {
            bool isPrivate = false;
            int worldID = 0;
            MySqlCommand command;
            DataTable table = new DataTable();
            MySqlDataAdapter adapter = new MySqlDataAdapter();
            MySqlDataReader reader;

            command = new MySqlCommand("SELECT * FROM `users in rooms` WHERE roomID = @roomID", connection);
            command.Parameters.Add("@roomID", MySqlDbType.Int32).Value = roomID;
            adapter.SelectCommand = command;
            adapter.Fill(table);

            if (table.Rows.Count < 2)
            {
                command = new MySqlCommand("DELETE FROM `users in rooms` WHERE userID = @userID AND roomID = @roomID", connection);
                command.Parameters.Add("@userID", MySqlDbType.Int32).Value = Convert.ToInt32(userID);
                command.Parameters.Add("@roomID", MySqlDbType.Int32).Value = roomID;

                MySqlCommand _command = new MySqlCommand("SELECT worldID FROM `rooms` WHERE id = @roomID", connection);
                _command.Parameters.Add("@roomID", MySqlDbType.Int32).Value = roomID;
                connection.Open();
                command.ExecuteNonQuery();
                worldID = Convert.ToInt32(_command.ExecuteScalar());
                connection.Close();

                command = new MySqlCommand("SELECT * FROM `worlds` WHERE id = @worldID", connection);
                command.Parameters.Add("@worldID", MySqlDbType.Int32).Value = worldID;

                _command = new MySqlCommand("DELETE FROM `rooms` WHERE id = @roomID", connection);
                command.Parameters.Add("@roomID", MySqlDbType.Int32).Value = roomID;

                connection.Open();
                _command.ExecuteNonQuery();
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    isPrivate = reader.GetBoolean(4);
                }
                connection.Close();
            } else
            {
                int nextUserID = 0;
                bool isOwner = false;


                command = new MySqlCommand("SELECT * FROM `users in rooms` WHERE userID = @userID AND roomID = @roomID", connection);
                command.Parameters.Add("@userID", MySqlDbType.Int32).Value = userID;
                command.Parameters.Add("@roomID", MySqlDbType.Int32).Value = roomID;

                MySqlCommand _command = new MySqlCommand("DELETE FROM `users in rooms` WHERE userID = @userID AND roomID = @roomID", connection);
                _command.Parameters.Add("@userID", MySqlDbType.Int32).Value = Convert.ToInt32(userID);
                _command.Parameters.Add("@roomID", MySqlDbType.Int32).Value = roomID;

                connection.Open();
                reader = command.ExecuteReader();
                while (reader.Read())
                {
                    if (reader.GetString(3) == "OWNER")
                        isOwner = true;
                }
                _command.ExecuteNonQuery();
                connection.Close();

                if (isOwner == true)
                {
                    command = new MySqlCommand("SELECT * FROM `users in rooms` WHERE roomID = @roomID", connection);
                    command.Parameters.Add("@roomID", MySqlDbType.Int32).Value = roomID;
                    connection.Open();
                    reader = command.ExecuteReader();
                    while (reader.Read())
                    {
                        nextUserID = reader.GetInt32(1);
                    }
                    connection.Close();

                    command = new MySqlCommand("UPDATE `users in rooms` SET status = 'OWNER' WHERE userID = @nextUserID AND roomID = @roomID", connection);
                    command.Parameters.Add("@nextUserID", MySqlDbType.Int32).Value = nextUserID;
                    command.Parameters.Add("@roomID", MySqlDbType.Int32).Value = roomID;
                    connection.Open();
                    command.ExecuteNonQuery();
                    connection.Close();
                }
            }

            return isPrivate;

        }

        #endregion

        public Boolean DoesWorldExist(int worldID)
        {
            DataTable table = new DataTable();
            MySqlDataAdapter adapter = new MySqlDataAdapter();
            MySqlCommand command = new MySqlCommand("SELECT * FROM worlds WHERE id = @worldID", connection);
            command.Parameters.Add("@worldID", MySqlDbType.VarChar).Value = worldID;
            adapter.SelectCommand = command;
            adapter.Fill(table);

            if (table.Rows.Count > 0)
            {
                return true;
            }
            else
                return false;
        }

        

    }
}
