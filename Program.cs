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
        static void Main(string[] args)
        {
            var numberOfGames = 100;
            var maxPlayers = 6;
            var rand = new Random();
            ThreadPool.SetMaxThreads(500, 500);


            int gamesOpen = 0;
            for (int i = 0; i < numberOfGames; i++)
            {
                var currentGame = new ShuffleGame();
                var numOfPlayers = rand.Next(maxPlayers / 2,maxPlayers);
                for (int j = 0; j < numOfPlayers; j++)
                {
                    /*ThreadPool.QueueUserWorkItem(Target, new
                                                             {
                                                                 ind = j,
                                                                 log = (Action<string>) (logs)
                                                             });
                    */
                    Thread t = new Thread(Target);
                    t.Start(new
                                {
                                    ind = j,
                                    log = (Action<string>) (logs)
                                });
                    gamesOpen++;
//                     Console.ReadLine();
                }
            }

            Console.WriteLine(string.Format("{0} Games Opened", gamesOpen));

            Console.ReadLine();
        }

        private static void Target(object parameters)
        {
            Action<string> log = (Action<string>)parameters.GetType().GetProperty("log").GetValue(parameters,null);
            Random rand = new Random((int)parameters.GetType().GetProperty("ind").GetValue(parameters,null));

            var id = Guid.NewGuid().ToString();
            var socket = new Client("http://50.116.22.241:81/"); // url to nodejs 
            socket.Opened += SocketOpened;
            socket.Message += SocketMessage;
            socket.SocketConnectionClosed += SocketConnectionClosed;
            socket.Error += SocketError;

            // register for 'connect' event with io server
            socket.On("Area.Game.AskQuestion.sal", (fn) =>
            {
//                Console.WriteLine(fn.MessageText);
                log(id + " stta");
                socket.Emit("Area.Game.AnswerQuestion", new { value = 1 });
            
   //  Thread.Sleep(rand.Next(400,2400));

            });

            socket.On("Area.Game.AskQuestion", (fn) =>
            {
                //              Console.WriteLine(fn.MessageText);
                log(id + " answered");
        //        socket.Emit("Area.Game.AnswerQuestion", new { value = 1 });
         //       Thread.Sleep(rand.Next(400, 2400));

            });

            socket.On("Area.Game.GameOver", (fn) =>
            {
                //              Console.WriteLine(fn.MessageText);
                log(id + " Over");

            });
            socket.On("Area.Game.StartGame", (fn) =>
            {
    //            Console.WriteLine(fn.MessageText);
                log(id + " started");
                socket.Emit("Area.Game.AnswerQuestion", new { value = 1 });

            });

            // make the socket.io connection
            socket.Connect();
            socket.Emit("Area.Game.StartGame",new {user="dested"});
            log(id + " connected");
            
        }

        private static void logs(string j)
        {
            
            Console.WriteLine(size+"  "+j);
        }

        private static void SocketError(object sender, ErrorEventArgs e)
        {
            
        }

        private static void SocketConnectionClosed(object sender, EventArgs e)
        {
            

        }
[ThreadStatic]

        private static long size = 0;
        private static void SocketMessage(object sender, MessageEventArgs e)
        {
            size += e.Message.Json.ToJsonString().Length;

        }

        private static void SocketOpened(object sender, EventArgs e)
        {
            

        }
    }

    internal class ShuffleGame
    {

    }
}
