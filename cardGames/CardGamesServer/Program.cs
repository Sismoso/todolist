using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace UDPServeur
{

    public class Program
    {
        private static Thread _thEcoute;
        public static string[] clientList = new string[12];
        public static int[] clientPoints = new int[4];

        private static void Main(string[] args)
        {
            //Préparation et démarrage du thread en charge d'écouter.
            _thEcoute = new Thread(new ThreadStart(Ecouter));
            _thEcoute.Start();
        }

        private static void Ecouter()
        {
            bool _continue = true;
            bool _nick = false;
            int  _nbplayer = 0;

            Console.Write("Please assign the server port  (Port 5656 occupied. Default is 4242) :\n> ");
            string _Port = Console.ReadLine();
            if (_Port == "")
                _Port = "4242";

            Console.WriteLine("DEBUG -- Préparation à l'écoute...");

            //On crée le serveur en lui spécifiant le port sur lequel il devra écouter.
            UdpClient serveur = new UdpClient(Int32.Parse(_Port));
            //serveur.Connect("127.0.0.1", Int32.Parse(_Port));

            //Création d'une boucle infinie qui aura pour tâche d'écouter.
            initClientList();
            while (_continue)
            {
                //Création d'un objet IPEndPoint qui recevra les données du Socket distant.
                IPEndPoint client = null;
                Console.WriteLine("DEBUG -- ÉCOUTE...");

                //On écoute jusqu'à recevoir un message.
                byte[] data = serveur.Receive(ref client);
                Console.WriteLine("DEBUG -- Données reçues en provenance de {0}:{1}.", client.Address, client.Port);

                //Décryptage et affichage du message.
                string message = Encoding.Default.GetString(data);
                Console.WriteLine("DEBUG -- CONTENU DU MESSAGE : {0}", message);
                if (_nick == true)
                {
                    clientList[_nbplayer + 3] = message;
                    _nick = false;
                    Console.WriteLine("DEBUG -- Client port is {0} and nick is {1}", clientList[_nbplayer - 1], clientList[_nbplayer + 3]);
                }
                switch (message.ToLower())
                {
                    case "/exit":
                        Send("/exit", client.Address.ToString());
                        _continue = false;
                        break;
                    case "/ready":
                        if (_nbplayer < 4)
                        {
                            _nbplayer++;
                            Send("\r", client.Address.ToString());
                            Send(_nbplayer.ToString(), client.Address.ToString());
                            switch (_nbplayer)
                            {
                                case 1:
                                    Send(" player is ready !\n> ", client.Address.ToString());
                                    break;
                                default:
                                    Send(" players are ready !\n> ", client.Address.ToString());
                                    break;
                            }
                            clientList[_nbplayer - 1] = client.Port.ToString();
                            clientList[_nbplayer + 7] = client.Address.ToString();
                            _nick = true;
                        }
                        else
                            Send("\rMaximum number of players ready. Wait for the next round ! ;)\n> ", client.Address.ToString());
                        break;
                    case "/start":
                        if (_nbplayer > 1)
                        {
                            Send("\rStarting the game now...\n\n", client.Address.ToString());
                            Send("/start", client.Address.ToString());
                            _nbplayer = gameLoop(_nbplayer, serveur);
                            initClientList();
                        }
                        else
                            Send("\rNot enough players to start a game (need at least 2)\n> ", client.Address.ToString());
                        break;
                }
            }
        }

        private static void initClientList()
        {
            int i = 0;

            while (i < 12)
            {
                clientList[i] = "null";
                if (i < 4)
                    clientPoints[i] = 0;
                i++;
            }
        }

        private static void compareCards(int _nbplayers, UdpClient serveur)
        {
            int x = 0;
            int i = 0;
            int[] card = { 0, 0, 0, 0 };
            int[] roundPoints = { 0, 0, 0, 0 };

            Console.Write("NB players = {0}\n", _nbplayers);
            while (i < _nbplayers)
            {
                Console.Write("NIKE\n");
                IPEndPoint client = null;
                byte[] data = serveur.Receive(ref client);
                string message = Encoding.Default.GetString(data);


                if (client.Port.ToString() == clientList[0])
                {
                    Console.Write("1\n");
                    card[0] = Int32.Parse(message);
                    i++;
                }
                if (client.Port.ToString() == clientList[1])
                {
                    Console.Write("2\n");

                    card[1] = Int32.Parse(message);
                    i++;
                }
                if (client.Port.ToString() == clientList[2])
                {
                    Console.Write("3\n");

                    card[2] = Int32.Parse(message);
                    i++;
                }
                if (client.Port.ToString() == clientList[3])
                {
                    Console.Write("4\n");
                    card[3] = Int32.Parse(message);
                    i++;
                }
            }

            Console.Write("A\n");

            while (x < _nbplayers)
            {
                i = 0;
                while (i < _nbplayers)
                {
                    if (x != i && card[x] >= card[i])
                        roundPoints[x] = roundPoints[x] + 1;
                    i++;
                }
                x++;
            }

            Console.Write("B\n");

            roundWinners(roundPoints, _nbplayers, card);
        }

        private static void roundWinners(int[] roundPoints, int _nbplayers, int[] card)
        {
            int x = 0;
            int i;
            int _nbrWinners = 0;
            Console.Write("DEBUG -- KEK\n");
            while (x < _nbplayers)
            {
                i = 0;
                while (i < _nbplayers)
                {
                    if (roundPoints[x] < roundPoints[i] && x != i)
                        roundPoints[x] = 0;
                    i++;
                }
                x++;
            }
            Console.Write("DEBUG -- KEK\n");
            x = 0;
            i = 0;
            while (x < _nbplayers)
            {
                //Console.Write("DEBUG -- RoundPoints {0] == {1}", x, roundPoints[x].ToString());
                if (roundPoints[x] > 0)
                    _nbrWinners++;
                x++;
            }

            x = 0;
            while (x < _nbplayers)
            {
                i = 0;
                if (x == 0)
                {
                    if (_nbrWinners == 1)
                    {
                        Send("This turn winner is :\n", clientList[8]);
                        while (i < _nbplayers)
                        {
                            if (roundPoints[i] != 0)
                            {
                                Send(clientList[i + 4], clientList[8]);
                                Send(" with a ", clientList[8]);
                                Send(card[i].ToString(), clientList[8]);
                                Send("\n\n", clientList[8]);
                                clientPoints[i] = clientPoints[i] + 1;
                            }
                            i++;
                        }
                    }
                    else
                    {
                        Send("This turn winners are :\n", clientList[8]);
                        while (i < _nbplayers)
                        {
                            if (roundPoints[i] != 0)
                            {
                                Send(clientList[i + 4], clientList[8]);
                                Send(" with a ", clientList[8]);
                                Send(card[i].ToString(), clientList[8]);
                                Send("\n", clientList[8]);
                                clientPoints[i] = clientPoints[i] + 1;
                            }
                            i++;
                        }
                        Send("\n", clientList[8 + x]);
                    }
                }
                else if (clientList[8 + x] != clientList[8])
                {
                    if (_nbrWinners == 1)
                    {
                        Send("This turn winner is :\n", clientList[8 + x]);
                        while (i < _nbplayers)
                        {
                            if (roundPoints[i] != 0)
                            {
                                Send(clientList[i + 4], clientList[8 + x]);
                                Send(" with a ", clientList[8 + x]);
                                Send(card[i].ToString(), clientList[8 + x]);
                                Send("\n\n", clientList[8 + x]);
                                clientPoints[i] = clientPoints[i] + 1;
                            }
                            i++;
                        }
                    }
                    else
                    {
                        Send("This turn winners are :\n", clientList[8 + x]);
                        while (i < _nbplayers)
                        {
                            if (roundPoints[i] != 0)
                            {
                                Send(clientList[i + 4], clientList[8 + x]);
                                Send(" with a ", clientList[8 + x]);
                                Send(card[i].ToString(), clientList[8 + x]);
                                Send("\n", clientList[8 + x]);
                                clientPoints[i] = clientPoints[i] + 1;
                            }
                            i++;
                        }
                        Send("\n", clientList[8 + x]);
                    }
                }
                x++;
            }
        }

        private static int gameLoop(int _nbplayer, UdpClient serveur)
        {
            int turn = 0;
            int x;

            while (turn <= 4)
            {
                x = 1;
                compareCards(_nbplayer, serveur);
                Send("/roundover", clientList[8]);
                while (x < _nbplayer)
                {
                    if (clientList[8 + x] != clientList[8])
                        Send("/roundover", clientList[8 + x]);
                    x++;
                }
                turn++;
            }
            gameWinners(_nbplayer);
            Send("/gameend", clientList[8]);
            x = 1;
            while (x <= _nbplayer)
            {
                if (clientList[8 + x] != clientList[8])
                    Send("/gameend", clientList[8 + x]);
                x++;
            }
            return 0;
        }

        private static void gameWinners(int _nbplayers)
        {
            int x = 0;
            int i;
            int _nrbWinners = 1;
            int maxScore = 0;

            while (x < _nbplayers)
            {
                if (clientPoints[x] > maxScore)
                    maxScore = clientPoints[x];
                x++;
            }

            x = 0;
            while (x < _nbplayers)
            {
                i = 0;
                while (i < _nbplayers)
                {
                    if (i != x && clientPoints[x] == clientPoints[i])
                        _nrbWinners++;
                    i++;
                }
                x++;
            }

            x = 0;
            while (x < _nbplayers)
            {
                i = 0;
                if (x == 0)
                {
                    if (_nrbWinners == 1)
                    {
                        Send("This game winner is :\n", clientList[8]);
                        while (i < _nbplayers)
                        {
                            if (clientPoints[i] == maxScore)
                            {
                                Send(clientList[i + 4], clientList[8]);
                                Send(" with a grand total of ", clientList[8]);
                                Send(clientPoints[i].ToString(), clientList[8]);
                                Send(" points.\n\n", clientList[8]);
                            }
                            i++;
                        }
                    }
                    else
                    {
                        Send("This game winners are :\n", clientList[8]);
                        while (i < _nbplayers)
                        {
                            if (clientPoints[i] == maxScore)
                            {
                                Send(clientList[i + 4], clientList[8]);
                                Send(" with a grand total of ", clientList[8]);
                                Send(clientPoints[i].ToString(), clientList[8]);
                                Send(" points.\n", clientList[8]);
                            }
                            i++;
                        }
                        Send("\n", clientList[8 + x]);
                    }
                }
                else if (clientList[8 + x] != clientList[8])
                {
                    if (_nrbWinners == 1)
                    {
                        Send("This game winner is :\n", clientList[8 + x]);
                        while (i < _nbplayers)
                        {
                            if (clientPoints[i] == maxScore)
                            {
                                Send(clientList[i + 4], clientList[8 + x]);
                                Send(" with a grand total of ", clientList[8 + x]);
                                Send(clientPoints[i].ToString(), clientList[8 + x]);
                                Send(" points.\n\n", clientList[8 + x]);
                            }
                            i++;
                        }
                    }
                    else
                    {
                        Send("This game winners are :\n", clientList[8 + x]);
                        while (i < _nbplayers)
                        {
                            if (clientPoints[i] == maxScore)
                            {
                                Send(clientList[i + 4], clientList[8 + x]);
                                Send(" with a grand total of ", clientList[8 + x]);
                                Send(clientPoints[i].ToString(), clientList[8 + x]);
                                Send(" points.\n", clientList[8 + x]);
                            }
                            i++;
                        }
                        Send("\n", clientList[8 + x]);
                    }
                }
                x++;
            }
        }

        private static void Send(string message, string _IP)
        {
            UdpClient sendToClient = new UdpClient();

            sendToClient.EnableBroadcast = true;

            sendToClient.Connect(new IPEndPoint(IPAddress.Broadcast, 5656));

            byte[] msg = Encoding.Default.GetBytes(message);

            //Console.WriteLine("1");

            //La méthode Send envoie un message UDP.
            sendToClient.Send(msg, msg.Length);
            sendToClient.Close();

            //Console.WriteLine("2");

        }
    }
}