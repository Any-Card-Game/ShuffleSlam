using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using SocketIOClient;

namespace ShuffleSlam
{
    class Program
    {
        private static string[] names = new string[] { "joe", "mike", "ana", "sal", "jeff", "todd" };
        static void Main(string[] args)
        {
            var numberOfGames = 100;
            var maxPlayers = 6;
            var rand = new Random();

            int gamesOpen = 0;
            for (int i = 0; i < numberOfGames; i++)
            {
                var numOfPlayers = rand.Next(maxPlayers / 2, maxPlayers);
                for (int j = 0; j < numOfPlayers; j++)
                {
                    /*ThreadPool.QueueUserWorkItem(Target, new
                                                             {
                                                                 ind = j,
                                                                 log = (Action<string>) (logs)
                                                             });
                    */
                    Thread t = new Thread(Target);
                    t.Start(new ThreadParams()
                                {
                                    Seed = j * 6 + i * 20,
                                    RoomIndex = i,
                                    State = (j == 0 ? (ThreadState.CreateGame) : ThreadState.JoinGame),
                                    UserName = names[j]
                                });
                    Thread.Sleep(rand.Next(300, 750));


                }
                Thread.Sleep(rand.Next(1400, 1550));
                gamesOpen++;

                Console.WriteLine(string.Format("{0} Games Opened", gamesOpen));
            }

            Console.ReadLine();
        }

        private static void Target(object parameters)
        {
            var options = (ThreadParams)parameters;
            Random rand = new Random(options.Seed);

            if (options.State == ThreadState.CreateGame)
            {
                Thread.Sleep(rand.Next(1000, 25000));
            }

            if (options.State == ThreadState.JoinGame)
            {
                Thread.Sleep(rand.Next(3000, 45000));
            }




            Console.WriteLine(string.Format("Begin {0}", options.State));


            var socket = new Client("http://50.116.22.241:81/"); // url to nodejs 
            socket.Opened += SocketOpened;
            socket.Message += SocketMessage;
            socket.SocketConnectionClosed += SocketConnectionClosed;
            socket.Error += SocketError;

            string roomID = null;
            // register for 'connect' event with io server 

            socket.On("Area.Game.AskQuestion", (fn) =>
            {
                Thread.Sleep(rand.Next(400, 2500));
                Console.WriteLine("asked: " + options.RoomIndex + "  " + options.UserName);
                if (socket == null) return;
                socket.Emit("Area.Game.AnswerQuestion", new { answer = 1, roomID });
            });


            socket.On("Area.Game.UpdateState", (fn) =>
            {

            });

            socket.On("Area.Game.RoomInfo", (fn) =>
                                                {
                                                    roomID = fn.Json.Args[0].roomID.ToString();

                                                });

            socket.On("Area.Game.GameOver", (fn) =>
            {
                socket.Close();
                socket = null;

            });

            socket.On("Area.Game.RoomInfos", (data) =>
                                                 {


                                                     foreach (var room in data.Json.Args[0].Children())
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

                                                         switch (options.State)
                                                         {
                                                             case ThreadState.JoinGame:
                                                                 roomID = room["roomID"].Value;
                                                                 socket.Emit("Area.Game.Join", new { user = new { name = options.UserName }, roomID = room["roomID"].Value });
                                                                 Console.WriteLine(options.RoomIndex+" Joined");
                                                                 if (plys + 1 >= room["maxUsers"].Value)
                                                                 {
                                                                     Thread.Sleep(rand.Next(750, 1250));
                                                                     socket.Emit("Area.Game.Start", new { roomID = room["roomID"].Value });
                                                                     Console.WriteLine(options.RoomIndex + " Started");
                                                                     return;
                                                                 }
                                                                 return;
                                                                 break;
                                                             default:
                                                                 throw new ArgumentOutOfRangeException();
                                                         }
                                                     }

                                                     Thread.Sleep(rand.Next(1000, 2000));
                                                     socket.Emit("Area.Game.GetGames", true);
                                                 });
            socket.On("Area.Game.Started", (fn) =>
            {

            });

            // make the socket.io connection
            socket.Connect();
            switch (options.State)
            {
                case ThreadState.JoinGame:
                    socket.Emit("Area.Game.GetGames", true);
                    break;
                case ThreadState.CreateGame:
                    Console.WriteLine(options.RoomIndex + " Created");

                    socket.Emit("Area.Game.Create", new { user = new { name = options.UserName } });

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

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
        public int Seed { get; set; }
        public ThreadState State { get; set; }
        public string UserName { get; set; }

        public int RoomIndex { get; set; }
    }
    public enum ThreadState
    {
        JoinGame, CreateGame
    }

    internal class ShuffleGame
    {

    }
}
