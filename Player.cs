﻿using System.Net;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace MMOG
{
    class Player
    {
        public int id;
        public string username;
        public DateTime loginDate;
        public Vector3 position;
        public Quaternion rotation;

        private Locations _currentLocation;
        public Locations CurrentLocation { get => _currentLocation; set => _currentLocation = value; }
        //  private float moveSpeed = 5f / Constants.TICKS_PER_SEC; // dlatego że serwer odbiera 30 wiadomości na sekunde
        // odpowiadałoby to speed / time.deltatime w unity
        private bool[] inputs; // wciśnięte klawisze przez gracza
        private bool walkIntoStairs;

        public Player(int _id, string _username, Vector3 _spawnPosition)
        {
            id = _id;
            username = _username;
            position = _spawnPosition;
            rotation = Quaternion.Identity;
            loginDate = DateTime.Now;
            inputs = new bool[4];
        }
        public void Update()
        {
            Vector2 _inputDirection = Vector2.Zero;

            if (inputs[0]) _inputDirection.Y += 1; // W
            if (inputs[1]) _inputDirection.Y -= 1; // S
            if (inputs[2]) _inputDirection.X -= 1; // A
            if (inputs[3]) _inputDirection.X += 1; // D

            if (_inputDirection == Vector2.Zero) return;

            // rusz sie tylko przy zarejestrowanym ruchu roznim niz 0.0
            Move(_inputDirection);
        }
        private void Move(Vector2 _direction)
        {
            inputs = new bool[4];

            Vector3 newPosition = position + new Vector3(_direction.X, _direction.Y, 0);
          //  Console.WriteLine("player want move to: "+newPosition);
            if (CheckIfPlayerNewPositionIsPossible(newPosition))
            {
                position += new Vector3(_direction.X, _direction.Y, 0);
                if (walkIntoStairs)
                {
                    position += stairsDirection;
                    walkIntoStairs = false;
                }
                CheckForItems();
                
            }

            ServerSend.PlayerPosition(this);
        }
        private void CheckForItems()
        {
            // trzymac w pamieci liste samych itemkow i ich lokalizacjiss
            // tyczy sie to itemkow juz lezacych na ziemi - znajdziek 

            // drop bedzie sie pojawiac losowo z puli, ale to poznmiej
            int ObstacleMap_key = Constants.GetKeyFromMapLocationAndType(CurrentLocation, MAPTYPE.Obstacle_MAP);
            
            if(Server.BazaWszystkichMDanychMap[ObstacleMap_key].ContainsKey(position))
            {
                if(Server.BazaWszystkichMDanychMap[ObstacleMap_key][position].Contains("ITEM"))
                {
                    //Console.WriteLine($"|Gracz {username} znalazl {Server.GLOBAL_MAPDATA[position]}");

                    // TODO: ServerSend.CollectItem 
                        // usuniecie z bazy serwera oznaczenie jako nieaktywny?
                        //dodanie do gracza 
                }
            }

            return;
        }
        public void SetInput(bool[] _inputs, Quaternion _rotation)
        {
            inputs = _inputs;
            rotation = _rotation;
        }
        Vector3 stairsDirection;
        Vector3 Vector3FloorUP = new Vector3(0,0,2);
        Vector3 Vector3FloorDown = new Vector3(0,0,-2);
        Vector3 Vector3RIGHT = new Vector3(1,0,0);
        Vector3 Vector3LEFT = new Vector3(-1,0,0);
        Vector3 Vector3UP = new Vector3(0,1,0);
        Vector3 Vector3DOWN = new Vector3(0,-1,0);

        

        private bool CheckIfPlayerNewPositionIsPossible(Vector3 _newPosition)
        {
            int GroundMap_key = Constants.GetKeyFromMapLocationAndType(CurrentLocation, MAPTYPE.Ground_MAP);
            int ObstacleMap_key = Constants.GetKeyFromMapLocationAndType(CurrentLocation, MAPTYPE.Obstacle_MAP);

            // proste sprawdzenie czy nastepna pozycją jest ściana lub czy istnieje
            Vector3 _groundPosition = _newPosition + Vector3FloorDown; // bo interesuje nas podłoga na ktorą gracz wchodzi
            Vector3 _downstairPosition = _newPosition + Vector3FloorDown + Vector3FloorDown; // bo interesuje nas podłoga na ktorą gracz wchodzi
            Vector3 currentPosition = position + Vector3FloorDown;
            bool output = false;


            // jezeli chodzimy po ziemi jest git ;d
            if (Server.BazaWszystkichMDanychMap[GroundMap_key].ContainsKey(_groundPosition))
            {  
                if(Server.BazaWszystkichMDanychMap[GroundMap_key][_groundPosition].Contains("ground"))
                {
                    output= true;
                }
            }

                        // jezeli chodzimy po ziemi jest git ;d
            if (Server.BazaWszystkichMDanychMap[ObstacleMap_key].ContainsKey(_groundPosition))
            {  
                if(Server.BazaWszystkichMDanychMap[ObstacleMap_key][_groundPosition].Contains("schody"))
                {
                    Console.WriteLine("Graczwchodzi na schodek -2L = poziom gracza");// => zwykłe przemeiszczenie sie w poziomo, w razie gdyby shcvody w pewnym momencie sie wydluzaly prosto ?
                    output = true;
                }
            }
            
            if (Server.BazaWszystkichMDanychMap[ObstacleMap_key].ContainsKey(_newPosition))
            {
                // jezeli przed nami jest klocek sciany => 
                 if (Server.BazaWszystkichMDanychMap[ObstacleMap_key][_newPosition].Contains("WALL")) {
                    output= false;
                 }

                // gracz ma przed sobą schodek i chce na neigo wejsc
                if(Server.BazaWszystkichMDanychMap[ObstacleMap_key][_newPosition].Contains("schody"))
                {
                    Console.WriteLine("Gracz wchodzi na schody +0L");
                    walkIntoStairs = true;
                    stairsDirection = Vector3FloorUP;
                    return output = true;
                }
            }
            
                //---------------------------------------------------
                // gracz schodzi na dol opuszczajac schody
            // jezeli w docelowym miejjscu za schodkiem jest ziemia
            if(Server.BazaWszystkichMDanychMap[GroundMap_key].ContainsKey(_downstairPosition))
            {
                // jezeli aktualnie stoimy na schodach
                if(Server.BazaWszystkichMDanychMap[ObstacleMap_key].ContainsKey(currentPosition))
                {
                    if(Server.BazaWszystkichMDanychMap[ObstacleMap_key][currentPosition].Contains("schody"))
                    {
                        Console.WriteLine("gracz chce zejść ze schodow na ziemie");
                        walkIntoStairs = true;
                        stairsDirection = Vector3FloorDown;
                        return  output = true;
                    }
                }
            }

            if (Server.BazaWszystkichMDanychMap[ObstacleMap_key].ContainsKey(_downstairPosition))
            {
                // gracz ma na dole schodek i chce na neigo zejsc
                if(Server.BazaWszystkichMDanychMap[ObstacleMap_key][_downstairPosition].Contains("schody"))
                {
                    stairsDirection = Vector3FloorDown;
                    walkIntoStairs = true;
                  /////////  stairsDirection = Vector3FloorDown;
                    return output = true;
                }
            }




            return output;

                                         
                                            //     //----------------------------------------------------------------------------------------------------------------------------
                                            //     // Sprawdzanie klocka przed sobą
                                            //     if (Server.GLOBAL_MAPDATA.ContainsKey(_newPosition))
                                            //     {
                                            //         if (Server.GLOBAL_MAPDATA[_newPosition].Contains("WALL")) {
                                            //       //      Console.WriteLine("nie mozna isc trafisz na sciane");
                                            //             return false;
                                            //         }

                                            //         if (Server.GLOBAL_MAPDATA[_newPosition].Contains("schody")) {
                                            //         //    Console.WriteLine("masz przed soba schodek, do gory, mozesz na niego wejsc");
                                            //             walkIntoStairs = true;
                                            //             stairsDirection = Vector3FloorUP; ;
                                            //             return true;
                                            //         }
                                            //     } else {
                                            //         if (walkIntoStairs == true) {
                                            //        //     Console.WriteLine("Nie masz niczego przed sobą a stoisz na schodach, nie mozesz z nich spac");
                                            //             output = false;
                                            //         } else {
                                            //       //      Console.WriteLine("nie masz nic przed soba, nic ni eblokuje ci drogi, mozesz isc");
                                            //             output = true;
                                            //         }
                                            //     }

                                            //   //  -------------------------------------------------------------------------------------------------------------------------------
                                            //     // sprawdzanie klocka na ktorym sie stoi
                                            //     if (Server.GROUND_MAPDATA.ContainsKey(_groundPosition)) {
                                            //         if (Server.GROUND_MAPDATA[_groundPosition].Contains("ground")) {
                                            //       //      mozna isc jest po czym
                                            //         //    Console.WriteLine("mozesz isc bedziesz miec pod sobą ziemie");
                                            //             output = true;
                                            //         }


                                            //         if(Server.GLOBAL_MAPDATA.ContainsKey(_groundPosition))
                                            //         {
                                                    
                                            //         if (Server.GLOBAL_MAPDATA[_groundPosition].Contains("schody")) 
                                            //         {
                                            //             if (walkIntoStairs == true) 
                                            //             {
                                            //           //      Console.WriteLine("jestesmy juz na schodach, a przed nami na ziemi jest kolejny schodek, idziemy na dol");
                                            //            //     Console.WriteLine("masz pod soba schodek, na dół, mozesz zejsc na niego");
                                            //                 walkIntoStairs = true;
                                            //                 stairsDirection = new Vector3(0, 0, -2);
                                            //                 return true;
                                            //             }

                                            //          //   Console.WriteLine("Mozesz spokojnie wejsc na pole schodka ;d");
                                            //             output = true;
                                            //         }
                                            //         else 
                                            //         { 
                                            //                 if (walkIntoStairs == true) {
                                            //             //    Console.WriteLine("nie masz nic przed soba ale wlasnie schodzi po schodach wiec cos bedzie na dole xD");
                                            //                 output = true;
                                            //             } else {
                                            //             //   Console.WriteLine("nie mozesz isc przed siebie, stracisz grunt pod nogami");
                                            //                 output = false;
                                            //             }
                                            //         }
                                            //     }
                                            // }
                                            //  //   ----------------------------------------------------------------------------------------------------------------------------
                                            //     if (Server.GLOBAL_MAPDATA.ContainsKey(_downstairPosition)) {
                                            //     //jezeli juz jestesmy na schodku, i chcemy isc dalej w dol trzeba sprawdzic czy nizej cos tam ejst
                                            //        if (Server.GLOBAL_MAPDATA[_downstairPosition].Contains("schody")) {
                                            //        //     Console.WriteLine("schodzisz dalej w dół, nizej jest jeszcze schodek, mozesz isc");

                                            //             walkIntoStairs = true;
                                            //             stairsDirection = Vector3FloorDown;

                                            //             return true;
                                            //         }
                                            //     }
                                            //     Console.WriteLine(output);
            //return output;
        }


    }
}