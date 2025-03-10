﻿using System.Linq;
using System;
using System.Collections.Generic;
using System.Numerics;
namespace MMOG
{
    public class Player
    {
        #region variables
        private bool _RegisteredUser = false;
        private int _id;
        private int _userID;
        private string _username;
        private string _password;
        private Vector3 _position;
        private Quaternion _rotation;
        private LOCATIONS _currentLocation;
        private int currentFloor;
        private bool[] _inputs; // wciśnięte klawisze przez gracza
        private Inventory _inventory;
        
        public DateTime _registrationDate;
        public DateTime LastLoginDate;

        public bool RegisteredUser { get => _RegisteredUser; set => _RegisteredUser = value; }
        public int Id { get => _id; set => _id = value; }
        public int UserID { get => _userID; set => _userID = value; }
        public string Username { get => _username; set => _username = value; }
        public string Password { get => _password; set => _password = value; }
        public Vector3 Position { get => _position; set => _position = value; }
        public LOCATIONS CurrentLocation 
        { 
            get => _currentLocation; 
            set 
            {
                _currentLocation = value;

                try
                {
                    SetMapDataRefernece(Server.dungeonLobbyRooms.Where(room=>room.Players.Contains(this)).First());
                }
                catch (System.Exception)
                {
                    Console.WriteLine("gracz nie jest w fdungeonie");
                    SetMapDataRefernece();
                }
            }
        }
        public bool[] Inputs { get => _inputs; set => _inputs = value; }
        public Quaternion Rotation { get => _rotation; set => _rotation = value; }
        public int CurrentFloor { get => currentFloor; set => currentFloor = value; }

        public Inventory Inventory { get => _inventory; set => _inventory = value; }
        public DungeonLobby myDungeonLobbyRoom;
        public bool InDungeon
        { 
            get => _inDungeon; 
            set {
                _inDungeon = value; 
                if(value == false) 
                {
                    myDungeonLobbyRoom = null;
                    return;
                }
                myDungeonLobbyRoom = Server.dungeonLobbyRooms.Where(room=>room.Players.Contains(this)).FirstOrDefault();
                SetMapDataRefernece(myDungeonLobbyRoom);
                dungeonRoom = (myDungeonLobbyRoom != null)?(int)myDungeonLobbyRoom.LobbyID:0;
            } 
        }

        private bool _inDungeon = false;
        public int dungeonRoom = 0;
        //  private float moveSpeed = 5f / Constants.TICKS_PER_SEC; // dlatego że serwer odbiera 30 wiadomości na sekunde
        // odpowiadałoby to speed / time.deltatime w unity

        // Actions
        public bool PlayerTransformedIntoStairs = false;
        #endregion
        public Player(int id, string username, int userID = 0)
        {
            this.Id = id;
            this.UserID = userID == 0 ? Server.USERSDATABASE.Count() + 1000 : userID;
            this.Username = username;
            this.Position = new Vector3(0, 0, 2);
            this.Rotation = Quaternion.Identity;
            this.LastLoginDate = DateTime.Now;
            this.CurrentLocation = LOCATIONS.Start_First_Floor;
            this.CurrentFloor = 2;
            this.Inputs = new bool[4];

            Console.WriteLine($"[#{Id}][{UserID}]{Username} dostaje 10 slotow w inventory");
            Inventory = new Inventory(10);
        }
        internal void Teleport(Vector3 _locationCordinates)
        {
            _position = _locationCordinates;
            ServerSend.PlayerPositionToALL(this, true);
        }
         internal void TeleportGroup(List<Player> _group, Vector3 _locationCordinates)
        {
            _position = _locationCordinates;
            // TODO:
            //ServerSend.PlayerPositionToGroup(_group,this, true);
            ServerSend.PlayerPositionToALL(this, true);

        }
        public void Update()
        {
            Vector2 _inputDirection = Vector2.Zero;

            if (_inputs[0]) _inputDirection.Y += 1; // W
            if (_inputs[1]) _inputDirection.Y -= 1; // S
            if (_inputs[2]) _inputDirection.X -= 1; // A
            if (_inputs[3]) _inputDirection.X += 1; // D

            if (_inputDirection == Vector2.Zero) return;

            // rusz sie tylko przy zarejestrowanym ruchu roznim niz 0.0
            Move(_inputDirection);
        }
        private bool walkIntoStairs;
        public void Move(Vector2 _direction)
        {
            //Console.WriteLine("move");
            Inputs = new bool[4];

            Vector3 newPosition = Position + new Vector3(_direction.X, _direction.Y, 0);
            //  Console.WriteLine("player want move to: "+newPosition);
            if (CheckIfPlayerNewPositionIsPossible(newPosition))
            {
                Position += new Vector3(_direction.X, _direction.Y, 0);
                if (walkIntoStairs)
                {
                    Position += stairsDirection;
                    walkIntoStairs = false;
                    CurrentFloor += (int)stairsDirection.Z;
                }
                CheckForItems();
                if(InDungeon)
                {
                    // podpiąć to i ujednolicić z check for item
                    CheckForGateTrigger();
                    CheckForExitFromDungeon(Position,myDungeonLobbyRoom.Get_DUNGEONS());
                }

            }

            ServerSend.PlayerPositionToALL(this);
        }
   
        private void CheckForExitFromDungeon(Vector3 position, DUNGEONS dungeon)
        {
            if(DungeonLobby.dungeonExitsDict.ContainsKey(position))
            {
                if (DungeonLobby.dungeonExitsDict[position] != dungeon) return;
                
                Console.WriteLine("aktywowanie procesu wyjscia z dungeonu "+DungeonLobby.dungeonExitsDict[position].ToString());
                foreach(var player in myDungeonLobbyRoom.Players)
                {
                    ServerSend.RunExitDungeonCounter(player.Id,myDungeonLobbyRoom);
                }
            }
        }
        private void CheckForGateTrigger()
        {
            if (obstacleData_Ref.ContainsKey(Position))
            {
                // TODO: przy wczytywaniu danych mapy, kazdemu trigerkowi dopisac odpowiadający jemu gate index
                if (obstacleData_Ref[Position].Contains("trigger"))
                {
                    Console.WriteLine("trigger detected");
                    // sp[rawdzenie ktorego date'a jest to trigger
                    DungeonGate dungeonGate = myDungeonLobbyRoom.listaGateow.Where(p=>p.TriggerPosition == Position).FirstOrDefault();
                    if(dungeonGate != null)
                    {
                        var gate = myDungeonLobbyRoom.listaGateow
                            .Where(g=>g.GateID == dungeonGate.GateID)
                            .First();
                        // sprawdzenie czy brama jest aktywna
                        if(gate.GateStatus == false) 
                        {
                            Console.WriteLine("Corresponded gate is already disabled");
                            return;
                        }
                        Console.WriteLine("Corresponded gate is still active");
                        // wykonanie akcji open gate dla wskazanego gatea
                        gate.OPENGATE(ref myDungeonLobbyRoom.DungeonMAPDATA_Ground);
                    }
                    else
                    {
                        Console.WriteLine("Trigger is not connected to any gate");
                    }
                }
            }
        }
        private void CheckForItems()
        {
            if (obstacleData_Ref.ContainsKey(Position))
            {
                if (obstacleData_Ref[Position].Contains("ITEM"))
                {
                    Console.WriteLine($"|Gracz {Username} znalazl {obstacleData_Ref[Position]}");
                    Item item = null;
                   try{
                        item = Inventory.GetItem(itemName:obstacleData_Ref[Position].Split("_")[1]);
                   }
                   catch(InvalidOperationException ex)
                   {
                        Console.WriteLine($"nie znaleziono takiego itemu [itemName = {obstacleData_Ref[Position].Split("_")[1]} ] w bazie Error:"+ex.Message);
                   }                   
        
                    Inventory.TESTPOPULATEINVENTORYWITHITEMBYID(item.id);
                    ServerSend.CollectItem(ID: Id, item: item);
                    ServerSend.RemoveItemFromMap(CurrentLocation, MAPTYPE.Obstacle_MAP, Position );
                    // usuwanie itemka z mapy
                    // obstacleData_Ref.Remove(Position);
                    // send to users info that this item is collected -> and update map
                    UpdateChecker.ChangeRecord(CurrentLocation,MAPTYPE.Obstacle_MAP,Position);
                }
            }

            return;
        }
        public void SetInput(bool[] _inputs, Quaternion _rotation)
        {
            Inputs = _inputs;
            this.Rotation = _rotation;
        }
        Vector3 stairsDirection;
        Vector3 Vector3FloorUP = new Vector3(0, 0, 2);
        Vector3 Vector3FloorDown = new Vector3(0, 0, -2);

        public Dictionary<Vector3,string> groundData_Ref;
        public Dictionary<Vector3,string> obstacleData_Ref;
        private void SetMapDataRefernece(DungeonLobby _dungeonLobby = null)
        {
            Console.WriteLine("podstawienie mapek - referencji gracza");
            groundData_Ref = new Dictionary<Vector3, string>();
            obstacleData_Ref = new Dictionary<Vector3, string>();

            if(_dungeonLobby == null)
            {
                int GroundMap_key = Constants.GetKeyFromMapLocationAndType(CurrentLocation, MAPTYPE.Ground_MAP);
                int ObstacleMap_key = Constants.GetKeyFromMapLocationAndType(CurrentLocation, MAPTYPE.Obstacle_MAP);
                
                try
                {
                    groundData_Ref = Server.BazaWszystkichMDanychMap[GroundMap_key];
                    obstacleData_Ref = Server.BazaWszystkichMDanychMap[ObstacleMap_key];
                }
                catch (System.NullReferenceException)
                {
                    Console.WriteLine("error : dane map nie zostały jeszcze zainicializowane, [null reference exceprtion]");
                }
                
            }
            else
            {
                if(! InDungeon) 
                {
                    Console.WriteLine("cos nie tak ?..."); return;
                }
                Console.WriteLine("podmiana referencji map na dungeonowe odpowiednikiL:");
                groundData_Ref = _dungeonLobby.DungeonMAPDATA_Ground;
                obstacleData_Ref = _dungeonLobby.DungeonMAPDATA;
            }
        }

        private bool CheckIfPlayerNewPositionIsPossible(Vector3 _newPosition)
        {
 

            // proste sprawdzenie czy nastepna pozycją jest ściana lub czy istnieje
            Vector3 _groundPosition = _newPosition + Vector3FloorDown; // bo interesuje nas podłoga na ktorą gracz wchodzi
            Vector3 _downstairPosition = _newPosition + Vector3FloorDown + Vector3FloorDown; // bo interesuje nas podłoga na ktorą gracz wchodzi
            Vector3 currentPosition = Position + Vector3FloorDown;
            bool output = false;


            // jezeli chodzimy po ziemi jest git ;d
            if (groundData_Ref.ContainsKey(_groundPosition))
            {
                if (groundData_Ref[_groundPosition].Contains("ground"))
                {
                    output = true;
                }
            }

            if (obstacleData_Ref.ContainsKey(_newPosition))
            {
                // jezeli przed nami jest klocek sciany => 
                if (obstacleData_Ref[_newPosition].Contains("WALL"))
                {
                    output = false;
                }

                // gracz ma przed sobą schodek i chce na neigo wejsc
                if (obstacleData_Ref[_newPosition].Contains("schody"))
                {

                     // sprawdzenie czy nie udezy sie na sciane ze schodow
                    Vector3 _upstairsPosition = _newPosition + Vector3FloorUP;
                     if (obstacleData_Ref.ContainsKey(_upstairsPosition))
                     {
                         if (obstacleData_Ref[_upstairsPosition].Contains("schody"))
                         {
                             // udezenie w sciane
                            return output = false;
                         }
                     }
                    walkIntoStairs = true;
                    stairsDirection = Vector3FloorUP;
                    return output = true;
                }
            }

            // jezeli chodzimy po ziemi jest git ;d
            if (obstacleData_Ref.ContainsKey(_groundPosition))
            {
                if (obstacleData_Ref[_groundPosition].Contains("schody"))
                {
                   
                    // Console.WriteLine("Graczwchodzi na schodek -2L = poziom gracza");// => zwykłe przemeiszczenie sie w poziomo, w razie gdyby shcvody w pewnym momencie sie wydluzaly prosto ?
                    walkIntoStairs = true;
                    stairsDirection = Vector3.Zero;
                    return output = true;
                }
            }
            //---------------------------------------------------
            // gracz schodzi na dol opuszczajac schody
            // jezeli w docelowym miejjscu za schodkiem jest ziemia
            if (groundData_Ref.ContainsKey(_downstairPosition))
            {
                // jezeli aktualnie stoimy na schodach
                if (obstacleData_Ref.ContainsKey(currentPosition))
                {
                    if (obstacleData_Ref[currentPosition].Contains("schody"))
                    {
                        //                        Console.WriteLine("gracz chce zejść ze schodow na ziemie");
                        walkIntoStairs = true;
                        stairsDirection = Vector3FloorDown;
                        return output = true;
                    }
                }
            }

            if (obstacleData_Ref.ContainsKey(_downstairPosition))
            {
                // gracz ma na dole schodek i chce na neigo zejsc
                if (obstacleData_Ref[_downstairPosition].Contains("schody"))
                {
                    stairsDirection = Vector3FloorDown;
                    walkIntoStairs = true;
                    /////////  stairsDirection = Vector3FloorDown;
                    return output = true;
                }
            }
            return output;
        }
    }
}