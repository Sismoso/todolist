using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace ExUDP
{
    public partial class Program
    {
        public const string HELPMSG = "Command list :\n" +
            "/ready     : Get ready !\n" +
            "/start     : Start the game (need at least 2 players ready)\n" +
            "/exit      : Stop the server and all clients\n" +
            "/help      : Displays this message\n" +
            "/list      : Displays the cards you got left (Only works while playing)\n" +
            "";

        static private Thread _thSend;
        static private Thread _thReceive;

        public static bool _isYourTurn = false;
        public static bool _isPlaying = false;
        public static bool _isReady = false;

        public static byte[] Nick;
        public static int[] deck;


        private static void Main(string[] args)
        {
            Console.Write("Enter the server IP address (Default is localhost) :\n> ");
            string _IP = Console.ReadLine();
            if (_IP == "localhost" || _IP == "")
                _IP = "127.0.0.1";

            Console.Write("Enter the server port (Port 5656 occupied. Default is 4242) :\n> ");
            string _Port = Console.ReadLine();
            if (_Port == "")
                _Port = "4242";

            _thReceive = new Thread(new ThreadStart(listen));
            _thReceive.Start();
            _thSend = new Thread(() => cloop(_IP, _Port));
            _thSend.Start();
        }

        private static void listen()
        {
            bool _continue = true;

            UdpClient ecoute = new UdpClient();
            ecoute.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            ecoute.Client.Bind(new IPEndPoint(IPAddress.Any, 5656));

            while (_continue)
            {
                IPEndPoint ip = null;

                byte[] data = ecoute.Receive(ref ip);

                //Décryptage et affichage du message.
                string message = Encoding.Default.GetString(data);

                int turn = 0;

                switch (message.ToLower())
                {
                    case "/exit":
                        ecoute.Close();
                        System.Environment.Exit(0);
                        break;
                    case "/start":
                        if (_isReady == true)
                        { 
                            _isPlaying = true;
                            _isYourTurn = true;
                            Console.Write("Please choose one of your cards\n> ");
                        }
                        break;
                    case "/gameend":
                        _isPlaying = false;
                        _isReady = false;
                        _isYourTurn = false;
                        Console.Write("\r> ");
                        break;
                    case "/roundover":
                        _isReady = true;
                        _isYourTurn = true;
                        if (turn++ < 4)
                            Console.Write("Please choose one of your cards\n> ");
                        break;
                    default:
                        Console.Write(message);
                        break;
                }
            }
            ecoute.Close();
        }

        private static void cloop(string _IP, string _Port)
        {
            bool _continue = true;
            bool _hasNick = false;
            string message;

            UdpClient sendClient = new UdpClient();
            sendClient.Connect(_IP, Int32.Parse(_Port));

            while (_hasNick == false)
            {
                Console.Write("Choose a nickname : \n> ");
                if ((message = Console.ReadLine()) != "")
                { 
                    _hasNick = true;
                    Nick = Encoding.Default.GetBytes(message);
                }
            }

            Console.Clear();
            Console.Write("Type /help to show the list of commands\n");

            while (_continue)
            {
                if (((_isPlaying == true && _isYourTurn == true) || _isPlaying == false))
                {
                    if (_isYourTurn == false)
                        Console.Write("> ");
                    message = Console.ReadLine();
                
                    byte[] msg = Encoding.Default.GetBytes(message);

                    switch (message.ToLower())
                    {
                        case "/ready":
                            if (_isReady == false)
                            {
                                Console.Write("You are now ready\n");
                                sendClient.Send(msg, msg.Length);
                                sendClient.Send(Nick, Nick.Length);
                                deck = new int[5] { 1, 2, 3, 4, 5 };
                                _isReady = true;
                            }
                            else
                                Console.Write("You are already ready\n> ");
                            break;
                        case "/help":
                            Console.Write(HELPMSG);
                            break;
                    }

                    if (_isReady == false || (_isReady == true && message.ToLower() != "/ready"))
                    { 
                        if (_isYourTurn == true)
                        {
                            switch (message.ToLower())
                            {
                                case "1":
                                    if (deck[0] != 0)
                                    {
                                        sendClient.Send(msg, msg.Length);
                                        deck[0] = 0;
                                        _isYourTurn = false;
                                    }
                                    else
                                        Console.Write("You already used that card. Type /list to show the cards you have left\n> ");
                                    break;
                                case "2":
                                    if (deck[1] != 0)
                                    {
                                        sendClient.Send(msg, msg.Length);
                                        deck[1] = 0;
                                        _isYourTurn = false;
                                    }
                                    else
                                        Console.Write("You already used that card. Type /list to show the cards you have left\n> ");
                                    break;
                                case "3":
                                    if (deck[2] != 0)
                                    {
                                        sendClient.Send(msg, msg.Length);
                                        deck[2] = 0;
                                        _isYourTurn = false;
                                    }
                                    else
                                        Console.Write("You already used that card. Type /list to show the cards you have left\n> ");
                                    break;
                                case "4":
                                    if (deck[3] != 0)
                                    {
                                        sendClient.Send(msg, msg.Length);
                                        deck[3] = 0;
                                        _isYourTurn = false;
                                    }
                                    else
                                        Console.Write("You already used that card. Type /list to show the cards you have left\n> ");
                                    break;
                                case "5":
                                    if (deck[4] != 0)
                                    {
                                        sendClient.Send(msg, msg.Length);
                                        deck[4] = 0;
                                        _isYourTurn = false;
                                    }
                                    else
                                        Console.Write("You already used that card. Type /list to show the cards you have left\n> ");
                                    break;
                                case "/list":
                                    for (int y = 0; y <= 4; y++)
                                    {
                                        if (deck[y] != 0)
                                            Console.Write("{0}\n", deck[y]);
                                    }
                                    Console.Write("> ");
                                    break;
                                default:
                                    Console.Write("Please choose one of your cards\n> ");
                                    break;

                            }
                        }
                        else
                            sendClient.Send(msg, msg.Length);
                    }

                    if (message.ToLower() == "/exit")
                        _continue = false;
                }
            }
            sendClient.Close();
        }
    }
}