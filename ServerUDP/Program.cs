using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerUDP
{
    class Program
    {
        const string ip = "25.89.55.32";
        const int port = 8080;
        static EndPoint udpEndPoint = new IPEndPoint(IPAddress.Parse(ip), port);

        public class user
        {
            public int userID;
            public EndPoint endPoint;
            public string login;
            public string password;
            public int inRoom = 0;
            public List<int> rooms;
            public bool isSearchingRooms;
            public string username;

            public user(string _login, string _password, EndPoint _endPoint, List<int> _rooms, int _userID, string _username)
            {
                isSearchingRooms = false;
                rooms = _rooms;
                userID = _userID;
                login = _login;
                password = _password;
                username = _username;
                endPoint = _endPoint;
            }
        }

        public static Dictionary<EndPoint, user> usersDictionary = new Dictionary<EndPoint, user>();
        public static Socket udpSocket;
        static void Main(string[] userCommands)
        {

            udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpSocket.Bind(udpEndPoint);

            cmdAsync();

            while (true)
            {
                var buffer = new byte[256];
                var size = 0;
                var data = new StringBuilder();
                EndPoint senderEndPoint = new IPEndPoint(IPAddress.Any, 0);
                do
                {
                    size = udpSocket.ReceiveFrom(buffer, ref senderEndPoint);
                    data.Append(Encoding.UTF8.GetString(buffer), 0, size);
                }
                while (udpSocket.Available > 0);

                var userCommand = data.ToString();
                ClientCommandsAsync(userCommand, senderEndPoint);
            }
        }

        public static void ClientCommands(string uCmd, EndPoint endPoint = null)
        {
            var userCommand = uCmd.Split("\n");

            switch (userCommand[0])
            {
                case "Register":
                    {
                        if (userCommand.Length != 4)
                        {
                            udpSocket.SendTo(Encoding.UTF8.GetBytes("RegisterAns\nError\nНеправильная команда"), endPoint);
                            break;
                        }
                        RegisterAsync(endPoint, userCommand[1], userCommand[2], userCommand[3]);
                        break;
                    }
                case "Connect":
                    if (userCommand.Length != 3)
                    {
                        udpSocket.SendTo(Encoding.UTF8.GetBytes("ConnectAns\nError\nНеправильная команда"), endPoint);
                        break;
                    }
                    ConnectAsync(endPoint, userCommand[1], userCommand[2]);
                    break;
                case "LogOut":
                    LogOutAsync(endPoint);
                    break;
                case "EnterTheRoom":
                    if (userCommand.Length != 2)
                    {
                        udpSocket.SendTo(Encoding.UTF8.GetBytes("EnterTheRoomAns\nError\nНеправильная команда"), endPoint);
                        break;
                    }
                    EnterTheRoomAsync(int.Parse(userCommand[1]), endPoint);
                    break;
                case "LeaveTheRoom":
                    if (userCommand.Length != 2)
                    {
                        udpSocket.SendTo(Encoding.UTF8.GetBytes("LeaveTheRoomAns\nError\nНеправильная команда"), endPoint);
                        break;
                    }
                    LeaveTheRoomAsync(endPoint, int.Parse(userCommand[1]));
                    break;
                case "CreateRoom":
                    if (userCommand.Length != 3)
                    {
                        udpSocket.SendTo(Encoding.UTF8.GetBytes("CreateRoomAns\nError\nНеправильная команда"), endPoint);
                        break;
                    }
                    CreateRoomAsync(endPoint, userCommand[1], userCommand[2]);
                    break;
                case "IsSearchingRooms":
                    isSearchingRoomsAsync(endPoint);
                    break;
                case "GetRoomInfo":
                    GetRoomInfoAsync(endPoint, int.Parse(userCommand[1]));
                    break;
                case "GetRoomList":
                    if (userCommand.Length != 3)
                    {
                        udpSocket.SendTo(Encoding.UTF8.GetBytes("GetRoomListAns\nError\nНеправильная команда"), endPoint);
                        break;
                    }
                    GetRoomListAsync(endPoint, int.Parse(userCommand[1]), int.Parse(userCommand[2]));
                    break;
                case "SendRoomMsg":
                    if (userCommand.Length != 3)
                    {
                        udpSocket.SendTo(Encoding.UTF8.GetBytes("SendRoomMsgAns\nError\nНеправильная команда"), endPoint);
                        break;
                    }
                    SendRoomMessageAsync(false, int.Parse(userCommand[1]), userCommand[2], endPoint);
                    break;
                case "GetChatLog":
                    if (userCommand.Length < 4)
                    {
                        udpSocket.SendTo(Encoding.UTF8.GetBytes("GetChatLogAns\nError\nНеправильная команда"), endPoint);
                        break;
                    }
                    GetChatLogAsync(int.Parse(userCommand[1]), int.Parse(userCommand[2]), int.Parse(userCommand[3]), endPoint);
                    break;
                default:
                    udpSocket.SendTo(Encoding.UTF8.GetBytes("Error\nНеправильная команда"), endPoint);
                    break;
            }
        }
        public static async void ClientCommandsAsync(string uCmd, EndPoint endPoint = null)
        {
            await Task.Run(() => ClientCommands(uCmd, endPoint));
        }
        public static void SendMsgTo(string login, string message)
        {
            try
            {
                udpSocket.SendTo(Encoding.UTF8.GetBytes(message), GetUserKey(login));
            }
            catch
            {
                Console.WriteLine("Пользователя не существует");
            }
        }
        public static void cmd(string cmd)
        {
            var cmdVar = cmd.ToString().Split(" ");
            switch (cmdVar[0])
            {
                case "send":
                    {
                        string login;
                        string msg;
                        if (cmdVar.Length < 2)
                        {
                            Console.Write("login = ");
                            login = Console.ReadLine();
                        }
                        else
                            login = cmdVar[1];
                        if (cmdVar.Length < 3)
                        {
                            Console.Write("Сообщение : ");
                            msg = Console.ReadLine();
                        }
                        else
                            msg = cmdVar[2];
                        try
                        {
                            SendMsgTo(login, msg);
                            Console.WriteLine("\n");
                            break;
                        }
                        catch
                        {
                            Console.WriteLine("Нет такого пользователя");
                            Console.WriteLine("\n");
                            break;
                        }
                    }
                case "list":
                    {
                        if (usersDictionary.Keys.Count == 0)
                        {
                            Console.WriteLine("Нет подключений");
                            return;

                        }
                        else
                        {
                            Console.WriteLine(usersDictionary.Keys.Count.ToString() + " подключений");
                        }
                        var i = 0;
                        foreach (EndPoint endPoint in usersDictionary.Keys)
                        {
                            Console.WriteLine(i + " - " + usersDictionary[endPoint].login);
                            i++;
                        }
                        Console.WriteLine("\n");

                        break;
                    }
                case "userinfo":
                    {
                        string login;
                        if (cmdVar.Length < 2)
                        {
                            Console.Write("login = ");
                            login = Console.ReadLine();
                        }
                        else
                            login = cmdVar[1];
                        try
                        {
                            EndPoint key = GetUserKey(login);
                            Console.Write(login + "\nip: " + key + "\nКомната на данный момент: " + usersDictionary[key].inRoom
                            + "\nИщет ли комнаты:" + (usersDictionary[key].isSearchingRooms ? true : false) + "\nПароль: " + usersDictionary[key].password + "\nКомнаты: ");
                            bool firsttime = true;
                            foreach (int i in usersDictionary[key].rooms)
                            {
                                Console.Write((!firsttime ? ", " : "") + i);
                                firsttime = false;
                            }
                            Console.WriteLine("\n");
                            break;
                        }
                        catch
                        {
                            Console.WriteLine("Нет такого пользователя");
                            Console.WriteLine("\n");
                            break;
                        }

                    }
                case "ucc":
                    {
                        Console.WriteLine("Введите команду: ");
                        string uCmd = Console.ReadLine().Replace(" ", "\n");
                        ClientCommandsAsync(uCmd);
                        break;
                    }
            }
        }
        public static async void cmdAsync()
        {
            while (true)
            {
                await Task.Run(() => cmd(Console.ReadLine()));
            }
        }

        public static void Register(EndPoint endPoint, string login, string password, string username)
        {
            if (login == "" || password == "" || login == null || password == null)
            {
                udpSocket.SendTo(Encoding.UTF8.GetBytes("RegisterAns\nError\nОшибка подключения"), endPoint);
                return;
            }
            DB database = new DB();
            if (database.CheckUniqueLogin(login))
            {
                database.CreateUser(login, password, username);
                udpSocket.SendTo(Encoding.UTF8.GetBytes("RegisterAns\nSuccess\nРегистрация успешна"), endPoint);
            }
            else
            {
                udpSocket.SendTo(Encoding.UTF8.GetBytes("RegisterAns\nError\nТакой логин уже существует"), endPoint);
                return;
            }
        }
        public static async void RegisterAsync(EndPoint endPoint, string login, string password, string username)
        {
            await Task.Run(() => Register(endPoint, login, password, username));
        }

        public static void Connect(EndPoint endPoint, string login, string password)
        {
            if (login == "" || password == "" || login == null || password == null)
            {
                udpSocket.SendTo(Encoding.UTF8.GetBytes("ConnectAns\nError\nОшибка подключения"), endPoint);
                return;
            }

            if (!usersDictionary.ContainsKey(endPoint))
            {
                DB database = new DB();
                if (database.CheckUserPassword(login, password))
                {
                    int userID = Convert.ToInt32(database.GetUserInfo(login)[0]);
                    string username = Convert.ToString(database.GetUserInfo(login)[1]);
                    usersDictionary[endPoint] = new user(login, password, endPoint, database.GetUserRoomList(userID), userID, username);
                }
                else
                {
                    udpSocket.SendTo(Encoding.UTF8.GetBytes("ConnectAns\nError\nНеправильный логин или пароль"), endPoint);
                    return;
                }
            }
            else
            {
                DB database = new DB();
                if (database.CheckUserPassword(login, password))
                {
                    int userID = Convert.ToInt32(database.GetUserInfo(login)[0]);
                    string username = Convert.ToString(database.GetUserInfo(login)[1]);
                    usersDictionary[endPoint] = new user(login, password, endPoint, database.GetUserRoomList(userID), userID, username);
                }
                else
                {
                    udpSocket.SendTo(Encoding.UTF8.GetBytes("ConnectAns\nError\nНеправильный логин или пароль"), endPoint);
                    return;
                }
            }

            string msg = "ConnectAns\nSuccess\nПодключение прошло успешно";
            foreach (int room in usersDictionary[endPoint].rooms)
            {
                msg += "\n" + room;
            }
            udpSocket.SendTo(Encoding.UTF8.GetBytes(msg), endPoint);

        }
        public static async void ConnectAsync(EndPoint endPoint, string login, string password)
        {
            await Task.Run(() => Connect(endPoint, login, password));
        }

        public static void LogOut(EndPoint endPoint)
        {
            usersDictionary.Remove(endPoint);
            udpSocket.SendTo(Encoding.UTF8.GetBytes("LogOutAns\nSuccess\nВыход из профиля"), endPoint);
        }
        public static async void LogOutAsync(EndPoint endPoint)
        {
            await Task.Run(() => LogOut(endPoint));
        }

        public static void EnterTheRoom(int roomID, EndPoint endPoint = null)
        {
            DB database = new DB();
            if (!database.DoesRoomExist(roomID))
            {
                udpSocket.SendTo(Encoding.UTF8.GetBytes("EnterTheRoomAns\nError\nКоманата не существует"), endPoint);
                return;
            }
            if (!usersDictionary.ContainsKey(endPoint))
            {
                udpSocket.SendTo(Encoding.UTF8.GetBytes("EnterTheRoomAns\nError\nНезарегистрированный пользователь"), endPoint);
                return;
            }
            usersDictionary[endPoint].isSearchingRooms = false;
            usersDictionary[endPoint].inRoom = roomID;
            if (!usersDictionary[endPoint].rooms.Contains(roomID))
            {
                usersDictionary[endPoint].rooms.Add(roomID);
                database.EnterTheRoom(roomID, usersDictionary[endPoint].userID);
                SendRoomMessage(true, roomID, "Пользователь вошел");
            }
            else
                udpSocket.SendTo(Encoding.UTF8.GetBytes("EnterTheRoomAns\nВы уже в этой комнате"), endPoint);

        }
        public static async void EnterTheRoomAsync(int roomID, EndPoint endPoint = null)
        {
            await Task.Run(() => EnterTheRoom(roomID, endPoint));
        }

        public static void LeaveTheRoom(EndPoint endPoint, int roomID)
        {
            //удаление человека из комнаты в бд
            //если пользователей не осталось, то удалить комнату
            if (!usersDictionary.ContainsKey(endPoint))
            {
                udpSocket.SendTo(Encoding.UTF8.GetBytes("LeaveTheRoomAns\nError\nНезарегистрированный пользователь"), endPoint);
                return;
            }
            if (usersDictionary[endPoint].rooms.Contains(roomID))
            {
                DB database = new DB();
                string msg = "LeaveTheRoomAns\nSuccess\nПользователь вышел";
                msg += "\n" + Convert.ToString(database.LeaveRoomReturnWorldPrivacy(usersDictionary[endPoint].userID, roomID));

                udpSocket.SendTo(Encoding.UTF8.GetBytes(msg), endPoint);
                usersDictionary[endPoint].rooms.Remove(roomID);
                SendRoomMessage(true, roomID, "Пользователь вышел");
            }
        }
        public static async void LeaveTheRoomAsync(EndPoint endPoint, int roomID)
        {
            await Task.Run(() => LeaveTheRoom(endPoint, roomID));
        }

        public static void CreateRoom(EndPoint endPoint, string roomName, string description, string worldID = null)
        {
            if (endPoint == null ? false : !usersDictionary.ContainsKey(endPoint))
            {
                udpSocket.SendTo(Encoding.UTF8.GetBytes("CreateRoomAns\nError\nНезарегистрированный пользователь"), endPoint);
                return;
            }
            else if (endPoint != null)
                usersDictionary[endPoint].isSearchingRooms = false;



            DB database = new DB();
            int roomID = database.CreateRoomReturnID(usersDictionary[endPoint].userID, roomName, description);

            usersDictionary[endPoint].rooms.Add(roomID);
            usersDictionary[endPoint].inRoom = roomID;
            udpSocket.SendTo(Encoding.UTF8.GetBytes("CreateRoomAns\nSuccess\nКомната сохранена\n" + roomID.ToString() + '\n'), endPoint);

        }
        public static async void CreateRoomAsync(EndPoint endPoint, string roomName, string description)
        {
            await Task.Run(() => CreateRoom(endPoint, roomName, description));
        }

        public static void IsSearchingRooms(EndPoint endPoint)
        {
            if (!usersDictionary.ContainsKey(endPoint))
            {
                udpSocket.SendTo(Encoding.UTF8.GetBytes("IsSearchingRoomsAns\nError\nНезарегистрированный пользователь"), endPoint);
                return;
            }
            usersDictionary[endPoint].isSearchingRooms = true;
            udpSocket.SendTo(Encoding.UTF8.GetBytes("IsSearchingRoomsAns\nSuccess\nПоиск комнат"), endPoint);
        }
        public static async void isSearchingRoomsAsync(EndPoint endPoint)
        {
            await Task.Run(() => IsSearchingRooms(endPoint));
        }

        public static void GetRoomInfo(EndPoint endPoint, int roomID)
        {
            DB database = new DB();

            if (database.DoesRoomExist(roomID))
            {
                object[] info = database.GetRoomInfo(roomID);

                string msg = "GetRoomInfoAns\nSuccess\n" + Convert.ToString(info[0]) + "\n" + Convert.ToString(info[1]) + "\n" + Convert.ToString(info[2]);

                udpSocket.SendTo(Encoding.UTF8.GetBytes(msg), endPoint);
            }
            else
            {
                udpSocket.SendTo(Encoding.UTF8.GetBytes("GetRoomInfoAns\nError\nКомнаты не существует"), endPoint);
            }
        }

        public static async void GetRoomInfoAsync(EndPoint endPoint, int roomID)
        {
            await Task.Run(() => GetRoomInfo(endPoint, roomID));
        }

        public static void GetRoomList(EndPoint endPoint, int count, int offset = 0)
        {
            DB database = new DB();

            string msg = "GetRoomListAns\nSuccess\n";
            object[,] info = database.GetRoomList(count, offset);
            for (int i = 0; (i < count && Convert.ToString(info[i, 0]) != ""); i++)
                msg += Convert.ToString(info[i, 0]) + "\n" + Convert.ToString(info[i, 1]) + "\n" + Convert.ToString(info[i, 2]) + "\n" + Convert.ToString(info[i, 3]) + "\n";

            udpSocket.SendTo(Encoding.UTF8.GetBytes(msg), endPoint);

        }

        public static async void GetRoomListAsync(EndPoint endPoint, int count, int offset = 0)
        {
            await Task.Run(() => GetRoomList(endPoint, count, offset));
        }

        public static void SendRoomMessage(bool isSystem, int roomID, string message, EndPoint endPoint = null)
        {
            Dictionary<EndPoint, user>.ValueCollection users = usersDictionary.Values;

            var selectedUsers = from u in usersDictionary.Values
                                where u.rooms.Contains(roomID)
                                select u;

            DB database = new DB();
            database.ChatLogAdd(roomID, message, DateTime.Now, isSystem ? 0 : usersDictionary[endPoint].userID, isSystem ? "System" : usersDictionary[endPoint].username);

            foreach (user user in selectedUsers)
            {
                udpSocket.SendTo(Encoding.UTF8.GetBytes("NewMessage\nToRoom\n" + roomID + "\n" + (isSystem ? "System: " : usersDictionary[endPoint].username) + "\n" + message + '\n'), user.endPoint);
            }
        }
        public static async void SendRoomMessageAsync(bool isSystem, int roomID, string message, EndPoint endPoint)
        {
            await Task.Run(() => SendRoomMessage(isSystem, roomID, message, endPoint));
        }

        public static EndPoint GetUserKey(string login)
        {
            return usersDictionary.Values.Single(u => u.login == login).endPoint;
        }
        public static string GetUserLogin(EndPoint endPoint)
        {
            return usersDictionary[endPoint].login;
        }

        public static void GetChatLog(int roomID, int count, int offset, EndPoint endPoint)
        {
            DB database = new DB();
            object[,] chatLogs = database.GetChatLogs(roomID, count, offset);
            string msg = "GetChatLogAns\nSuccess\n";

            for (int i = Convert.ToInt32(chatLogs[0, 0]); i > 0; i--)
                msg += Convert.ToString(chatLogs[i, 2]) + "\n" + Convert.ToString(chatLogs[i, 1]) + "\n" + Convert.ToString(chatLogs[i, 0]) + "\n";

            udpSocket.SendTo(Encoding.UTF8.GetBytes(msg), endPoint);
        }

        public static async void GetChatLogAsync(int roomID, int count, int offset, EndPoint endPoint)
        {
            await Task.Run(() => GetChatLog(roomID, count, offset, endPoint));
        }

    }
}
