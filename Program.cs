﻿using MMOG;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace MMOG
{
    class Program
    {
        private static bool isRunning = false;

        static void Main(string[] args)
        {
            UpdateChecker.ReadDataFromFile();

            Console.Title = "Game Server";
            isRunning = true;

            Thread mainThread = new Thread(new ThreadStart(MainThread));
            mainThread.Start();

            Server.Start(50, 5555);

            while (true) {
                string consoleCommand = Console.ReadLine();
                if (consoleCommand == "cmd_help") ShowConsoleCommands();
                if (consoleCommand == "cmd_chat") GMMessagesToClients();
                if (consoleCommand == "cmd_users") ShowCurrentlyLoggedInUsers();
                if (consoleCommand.Contains("cmd_kick_")) KickUserByID(Convert.ToInt32(consoleCommand.Replace("cmd_kick_","")));
                if (consoleCommand.Contains("cmd_downloadAllMaps")) {
                    ServerSend.DownloadMapData(fromID: Convert.ToInt32(consoleCommand.Replace("cmd_downloadAllMaps_","")),MAPTYPE.Obstacle_MAP, LOCATIONS.Start_First_Floor);
                    ServerSend.DownloadMapData(fromID: Convert.ToInt32(consoleCommand.Replace("cmd_downloadAllMaps_","")),MAPTYPE.Ground_MAP,LOCATIONS.Start_First_Floor);

                    ServerSend.DownloadMapData(fromID: Convert.ToInt32(consoleCommand.Replace("cmd_downloadAllMaps_","")),MAPTYPE.Obstacle_MAP,LOCATIONS.Start_Second_Floor);
                    ServerSend.DownloadMapData(fromID: Convert.ToInt32(consoleCommand.Replace("cmd_downloadAllMaps_","")),MAPTYPE.Ground_MAP,LOCATIONS.Start_Second_Floor);
                }
                if (consoleCommand == "cmd_ChooseMapToDownload") {
                    Console.WriteLine(
                        $"0-0 = [{LOCATIONS.Start_First_Floor.ToString()}][{MAPTYPE.Ground_MAP.ToString()}] \n" +
                        $"0-1 = [{LOCATIONS.Start_First_Floor.ToString()}][{MAPTYPE.Obstacle_MAP.ToString()}] \n" +
                        $"1-0 = [{LOCATIONS.Start_Second_Floor.ToString()}][{MAPTYPE.Ground_MAP.ToString()}] \n" +
                        $"1-1 = [{LOCATIONS.Start_Second_Floor.ToString()}][{MAPTYPE.Obstacle_MAP.ToString()}]");
                    string mapchoose = Console.ReadLine();

                    Console.WriteLine("podaj jeszcze ID Admina od ktorego chcesz pobrać mape");
                    int admin = Int32.Parse(Console.ReadLine());

                    switch(mapchoose) {
                        case "0-0": ServerSend.DownloadMapData(fromID: admin, mapLocation: LOCATIONS.Start_Second_Floor, mapType: MAPTYPE.Ground_MAP); break;
                        case "0-1": ServerSend.DownloadMapData(fromID: admin, mapLocation: LOCATIONS.Start_Second_Floor, mapType: MAPTYPE.Obstacle_MAP); break;
                        case "1-0": ServerSend.DownloadMapData(fromID: admin, mapLocation: LOCATIONS.Start_Second_Floor, mapType: MAPTYPE.Ground_MAP); break;
                        case "1-1": ServerSend.DownloadMapData(fromID: admin, mapLocation: LOCATIONS.Start_Second_Floor, mapType: MAPTYPE.Obstacle_MAP); break;
                    }
                };
                if (consoleCommand == "cmd_sendMapUpdateToAll") ServerSend.SendCurrentUpdateVersionNumber(); 
                if (consoleCommand == "cmd_printAllPositions") PrintPlayersPositions();
                if (consoleCommand == "cmd_showData") Console.WriteLine(UpdateChecker.serverJsonFile);
              //  if (consoleCommand == "cmd_initData") UpdateChecker.IniciateData_TEST();
              //  if (consoleCommand == "cmd_dataTest") {
               //     TestAktualizacjiIWyswietlaniaDanychZBAzy();
               // }
            }
        }

        // private static void TestAktualizacjiIWyswietlaniaDanychZBAzy() {
        //     Console.WriteLine(UpdateChecker.GetVersionOf(_datatype: DATATYPE.Locations, _location: LOCATIONS.Start_Second_Floor, _maptype: MAPTYPE.Obstacle_MAP));
        //     Console.WriteLine(UpdateChecker.GetVersionOf(_datatype: DATATYPE.Items, _item: ITEMS.Stone));
        //     Items potek = UpdateChecker.SERVER_UPDATE_VERSIONS._Data[ITEMS.Health_Potion];
        //     potek._Name = "Mikstura życia";
        //     UpdateChecker.ChangeRecord(ITEMS.Health_Potion, potek);
        // }

        private static void PrintPlayersPositions()
        {
            foreach(var player in Server.clients.Values.Where(p=>p.player != null))
            {
                Console.WriteLine($"[{player.player.username}] => [{player.player.position}]");;
            }
        }

        private static void KickUserByID(int _userId) {

            if(Server.clients.ContainsKey(_userId) == false )
            {
                Console.WriteLine("Brak gracza o ID: "+_userId);
                return;
            }

            Console.WriteLine("Kicking user with id " + _userId + "and nick:" + Server.clients[_userId].player.username);
            Server.clients[_userId].Disconnect();
        }

        private static void ShowCurrentlyLoggedInUsers() {
            string listaGraczy = "";
            foreach(Client client in Server.clients.Values)
            {
                if (client.player == null) continue;

                Player player = client.player;
                string timePlayerIsOnline = (DateTime.Now - player.loginDate).ToString(@"hh\:mm\:ss");
                listaGraczy += $"[#{player.id}]:[{player.username}]:[{timePlayerIsOnline}]\n";
            }

            Console.WriteLine(listaGraczy);
        }

        private static void ShowConsoleCommands()
        {
            Console.WriteLine(
                  "Dostepne komendy:\n"
                + "[cmd_chat]  -> wiadomosci GM na czacie globalnym.\n"
                + "[cmd_help]  -> wyswietlenie wszystkich komend\n"
                + "[cmd_users] -> lista aktualnie zalogowanych graczy\n"
                + "[cmd_kick_<user id>] -> wywalenie gracza z ID\n"
                + "[cmd_downloadAllMaps_<admin_id>] -> pobranie na serwer danych Mapy od klienta, [2x Ground + Obstacle]\n"
                + "[cmd_sendMapUpdateToAll] -> wysłanie do wszystkich aktualnie zalogowanychgraczy info o nowej aktualizacji na serwerze czekającej do pobrania\n"
                + "[cmd_printAllPositions] -> wyswietlenie pozycji wszystkich graczy]\n");      
                //TOOD: dodać wyświetlanie sie aktualnych lokalizacji ew liczby osob w danym regionie          
        }
        private static void GMMessagesToClients()
        {
            string message = "";
            Console.WriteLine("wprowadz wiadomosc do ludu : \n[cmd_exit] -> by zakońćzyć nadawanie obwieszczeń");
            while(true) {
                message = Console.ReadLine();
                
                if(message == "cmd_exit") {
                    Console.Clear();
                    return;
                }
                ServerSend.UpdateChat(message);
            }
        }

        private static void MainThread()
        {
            //Console.WriteLine($"Main thread started. Running at {Constants.TICKS_PER_SEC} ticks per second.");
            DateTime _nextLoop = DateTime.Now;

            // init ping
            //Console.WriteLine("Ping do graczy...");
            ServerSend.Ping_ALL();
            int afkCleanerCounter = Constants.TIME_IN_SEC_TO_RESPOND_BEFORE_KICK*1000;

            // pingowanie i czyszczenie afków co 1s = 1.000ms
            while (isRunning)
            {
                while (_nextLoop < DateTime.Now)
                {
                    GameLogic.Update();

                    _nextLoop = _nextLoop.AddMilliseconds(Constants.MS_PER_TICK);

                    if (_nextLoop > DateTime.Now)
                    {
                        try { Thread.Sleep(_nextLoop - DateTime.Now); } 
                        catch(Exception ex) { Console.WriteLine(ex.Message); };
                    }

                    afkCleanerCounter -= (int)Constants.MS_PER_TICK;
                    if (afkCleanerCounter < 0) {
                        //Console.WriteLine("Kicking ghost-afk users");
                        foreach (KeyValuePair<int, string> obecnosc in Server.listaObecnosci) {
                            if (obecnosc.Value.Contains("[....]"))// jest AFKIEM nie zdazyl przyslac odpowiedzi w podanym czasie 
                            {
                                try {
                                    Console.WriteLine("brak odpowiedzi ze strony gracza [" + Server.clients[obecnosc.Key].player.username + "].");
                                } catch { };
                                ServerSend.RemoveOfflinePlayer(obecnosc.Key);
                                Server.clients[obecnosc.Key].Disconnect();
                                Server.ZaktualizujListeObecnosci(afkId: obecnosc.Key);
                            }
                        }
                        ServerSend.Ping_ALL(); // wykonac raz co x sekund na poczatku
                        afkCleanerCounter = Constants.TIME_IN_SEC_TO_RESPOND_BEFORE_KICK * 1000;
                    }
                }
            }
        }
    }
}
