﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace MMOG
{
    class Constants
    {
        public const int TICKS_PER_SEC = 30;
        public const float MS_PER_TICK = 1000f / TICKS_PER_SEC;
        //public const string MAP_DATA_FILE_PATH = @"OBSTACLE_MAPDATA_SERVER.txt";
        //public const string GROUND_MAP_DATA_FILE_PATH = @"GROUND_MAPDATA_SERVER.txt";
        public static int TIME_IN_SEC_TO_RESPOND_BEFORE_KICK = 10;
        public static string PATH_NOTES_SERVER = @"DATA\Server_UpdateNotes.json";

        public static string GetFilePath(DATATYPE dataType, LOCATIONS locations, MAPTYPE mapType)
        {
            CreateFolder(DATATYPE.Locations,locations);
            return $"DATA\\{dataType.ToString()}\\{locations.ToString()}\\{mapType.ToString()}.txt";
        }
          public static void CreateFolder(DATATYPE? dataType, LOCATIONS? locations)
        {
        //    Directory.CreateDirectory($"{Application.persistentDataPath}\\DATA\\{dataType.ToString()}\\{locations.ToString()}");
            Directory.CreateDirectory($"DATA\\{dataType.ToString()}\\{locations.ToString()}");
    }

        public static int GetKeyFromMapLocationAndType(LOCATIONS location, MAPTYPE mapType) => (int)location * 10 + (int)mapType + 1;
    }

    // DATA\Locations\Start_First_Floor/...
    public enum LOCATIONS
    {
        Start_First_Floor,
        Start_Second_Floor,
        DUNGEON_1,
        DUNGEON_2
}
    public enum DUNGEONS // nazwa jednakowa z tą w locations !
    {
        DUNGEON_1 = 1,
        DUNGEON_2
    }
    public enum MAPTYPE
    {
        Ground_MAP,
        Obstacle_MAP
    }
    public enum DATATYPE
    {
       Locations,
       Items
         // ...
    }
    public enum ITEMS
    {
        Armor,
        Stone,
        Health_Potion
    }
}
