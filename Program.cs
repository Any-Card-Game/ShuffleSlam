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
            var numberOfGames = 200;
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
                                    State = (j == 0 ? (ThreadState.CreateGame) : (j == numOfPlayers - 1 ? ThreadState.StartGame : ThreadState.JoinGame)),
                                    UserName = names[j]
                                });

                    gamesOpen++;
                    Console.WriteLine(string.Format("{0} Games Opened", gamesOpen));
                    //Console.ReadLine();
                    Thread.Sleep(rand.Next(150, 450));

                }
                Thread.Sleep(rand.Next(400 , 550 ));
            }

            Console.WriteLine(string.Format("{0} Games Opened", gamesOpen));

            Console.ReadLine();
        }

        private static void Target(object parameters)
        {
            var options = (ThreadParams)parameters;
            Random rand = new Random(options.Seed);

            var socket = new Client("http://50.116.22.241:81/"); // url to nodejs 
            socket.Opened += SocketOpened;
            socket.Message += SocketMessage;
            socket.SocketConnectionClosed += SocketConnectionClosed;
            socket.Error += SocketError;

            string roomID = null;
            // register for 'connect' event with io server


            socket.On("Area.Game.AskQuestion", (fn) =>
            {
                Thread.Sleep(rand.Next(400, 2000));
                Console.WriteLine("asked: " + options.RoomIndex + "  " + options.UserName);
                socket.Emit("Area.Game.AnswerQuestion", new { answer = 1, roomID = options.RoomIndex });
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
            socket.On("Area.Game.Started", (fn) =>
            {

            });

            // make the socket.io connection
            socket.Connect();
            switch (options.State)
            {
                case ThreadState.StartGame:
                    socket.Emit("Area.Game.Join", new { user = new { name = options.UserName }, roomID = options.RoomIndex });
                    socket.Emit("Area.Game.Start", new { user = new { name = options.UserName }, roomID = options.RoomIndex });
                    break;
                case ThreadState.JoinGame:
                    socket.Emit("Area.Game.Join", new { user = new { name = options.UserName }, roomID = options.RoomIndex });
                    break;
                case ThreadState.CreateGame:
                    socket.Emit("Area.Game.Create", new { user = new { name = options.UserName } });

                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static void SocketError(object sender, ErrorEventArgs e)
        {

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
        StartGame, JoinGame, CreateGame
    }

    internal class ShuffleGame
    {

    }
}
