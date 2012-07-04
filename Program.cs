using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient;
using ErrorEventArgs = SocketIOClient.ErrorEventArgs;

namespace ShuffleSlam
{
    class Program
    {
        private static string[] names = new string[] { "joe", "mike", "ana", "sal", "jeff", "todd" };
        static void Main(string[] args)
        {
            var maxPlayers = 6;
            var rand = new Random();





             

            while (true) 
            {

                var numOfPlayers = rand.Next(maxPlayers / 2, maxPlayers);
                Task[] tasks = new Task[numOfPlayers];
                for (int j = 0; j < numOfPlayers; j++)
                {
                    int j1 = j;

                    tasks[j] = Task.Factory.StartNew(() => Target(new ThreadParams()
                                                                      {
                                                                          MaxUsers=numOfPlayers,
                                                                          Seed = j1 * 6,
                                                                          State = (j1 == 0 ? (ThreadState.CreateGame) : ThreadState.JoinGame),
                                                                          UserName = names[j1]
                                                                      }));
                }
                while (tasks.Any(t => !t.IsCompleted)) { } //spin wait


                 

        }
             
        }

        public static int foos = 0;
        private static void Target(object parameters)
        {
            var options = (ThreadParams)parameters;
            Random rand = new Random(options.Seed);

            if (options.State == ThreadState.CreateGame)
            {
                Thread.Sleep(rand.Next(0, 500));
            }

            if (options.State == ThreadState.JoinGame)
            {
                Thread.Sleep(rand.Next(1500, 2500));
            }

            Console.WriteLine(string.Format("Begin {0}", options.State));
            string ip=null;

            WebClient client = new WebClient();
            ip = client.DownloadString("http://50.116.22.241:8844");
           

            var socket = new Client(ip); // url to nodejs 
            socket.Opened += SocketOpened;
            socket.Message += SocketMessage;
            socket.SocketConnectionClosed += SocketConnectionClosed;
            socket.Error += SocketError;
            string gameServer=null;
            string roomID = null;
            // register for 'connect' event with io server 

            Dictionary<string, Action<dynamic>> dct = new Dictionary<string, Action<dynamic>>();



            socket.On("Client.Message", (fn) =>
                                            {
                                                var cn = fn.Json.Args[0].channel.Value;
                                                var cnt = fn.Json.Args[0].content;

                                                dct[cn](cnt);

                                            });


            

            dct.Add("Area.Game.AskQuestion", (fn) =>
            {
                Thread.Sleep(rand.Next(300, 800));
                Console.WriteLine("asked: " + "  " + options.UserName);
                if (socket == null) return;
                try
                {
                    emit(socket, "Area.Game.AnswerQuestion", new { answer = 1, roomID }, gameServer);
                }catch(Exception)
                {
                    Console.WriteLine("failed for some reason");
                }
            });


            dct.Add("Area.Game.UpdateState", (fn) =>
            {

            });

            dct.Add("Area.Game.RoomInfo", (fn) =>
                                                {
                                                    roomID = fn.roomID.ToString();
                                                    gameServer = fn.gameServer;
                                                });

            bool gameover = false;
            dct.Add("Area.Game.GameOver", (fn) =>
                                              {
                                                  socket.Close();
                                                  socket.Dispose();
                                                  gameover = true;

                                              }); 
            dct.Add("Area.Game.RoomInfos", (data) =>
                                                 {


                                                     foreach (var room in data.Children())
                                                     {
                                                         var plys = 0;
                                                         foreach (var child in room["players"].Children())
                                                         {
                                                             plys++;
                                                         }
                                                         if (room["started"].Value)
                                                         { 
                                                             continue;
                                                         }
                                                         gameServer = room["gameServer"].Value;
                                                         switch (options.State)
                                                         {
                                                             case ThreadState.JoinGame:
                                                                 roomID = room["roomID"].Value;
                                                                 emit(socket, "Area.Game.Join", new { user = new { userName = options.UserName }, roomID = room["roomID"].Value }, gameServer);

                                                                 if (plys + 1 >= options.MaxUsers)
                                                                 {
                                                                     Thread.Sleep(rand.Next(750, 1250));
                                                                     emit(socket, "Area.Game.Start", new { roomID = room["roomID"].Value }, gameServer);
                                                                     return;
                                                                 }
                                                                 return;
                                                                 break;
                                                             default:
                                                                 throw new ArgumentOutOfRangeException();
                                                         }
                                                     }

                                                     Thread.Sleep(rand.Next(600, 900));
                                                     emit(socket, "Area.Game.GetGames", true, gameServer);
                                                 });
            dct.Add("Area.Game.Started", (fn) =>
            {

            });

            // make the socket.io connection
            socket.Connect();

            socket.Emit("Gateway.Login", new {userName = options.UserName + " " + Guid.NewGuid().ToString()});

            switch (options.State)
            {
                case ThreadState.JoinGame:
                    emit(socket, "Area.Game.GetGames", true, gameServer);
                    break;
                case ThreadState.CreateGame:
                    Console.WriteLine( "Created");

                    emit(socket, "Area.Game.Create", new { gameName = "Sevens", name = "game " + Guid.NewGuid().ToString()  , user = new { name = options.UserName } }, gameServer);

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            while (!gameover)
            {
                Thread.Sleep(1000);
            }

        }
        private static void emit(Client cli, string chan, object obj, string gameServer)
        {
            cli.Emit("Gateway.Message", new { channel = chan, content = obj, gameServer=gameServer });

        }

        private static void SocketError(object sender, ErrorEventArgs e)
        {
            Console.WriteLine(e.Message+"   "+ e.Exception.ToString());
        }

        private static void SocketConnectionClosed(object sender, EventArgs e)
        {

        }

        private static void SocketMessage(object sender, MessageEventArgs e)
        {

        }

        private static void SocketOpened(object sender, EventArgs e)
        {


        }
    }
    public class ThreadParams
    {
        public int MaxUsers { get; set; }
        public int Seed { get; set; }
        public ThreadState State { get; set; }
        public string UserName { get; set; }
         
    }
    public enum ThreadState
    {
        JoinGame, CreateGame
    }

    internal class ShuffleGame
    {

    }
}
