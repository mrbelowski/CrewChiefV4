﻿using CrewChiefV4.GameState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV4
{
    public enum GameEnum
    {
        RACE_ROOM, PCARS_64BIT, PCARS_32BIT, PCARS_NETWORK, RF1, ASSETTO
    }
    public class GameDefinition
    {
        public static GameDefinition pCars64Bit = new GameDefinition(GameEnum.PCARS_64BIT, Configuration.getUIString("pcars_64_bit"), "pCARS64",
            "CrewChiefV4.PCars.PCarsSpotterv2", "pcars64_launch_exe", "pcars64_launch_params", "launch_pcars");
        public static GameDefinition pCars32Bit = new GameDefinition(GameEnum.PCARS_32BIT, Configuration.getUIString("pcars_32_bit"), "pCARS",
            "CrewChiefV4.PCars.PCarsSpotterv2", "pcars32_launch_exe", "pcars32_launch_params", "launch_pcars");
        public static GameDefinition raceRoom = new GameDefinition(GameEnum.RACE_ROOM, Configuration.getUIString("race_room"), "RRRE", "CrewChiefV4.RaceRoom.R3ESpotterv2",
            "r3e_launch_exe", "r3e_launch_params", "launch_raceroom");
        public static GameDefinition pCarsNetwork = new GameDefinition(GameEnum.PCARS_NETWORK, Configuration.getUIString("pcars_udp"), null, "CrewChiefV4.PCars.PCarsSpotterv2",
            null, null, null);
        public static GameDefinition rFactor1 = new GameDefinition(GameEnum.RF1, Configuration.getUIString("rfactor1"), "rFactor", "CrewChiefV4.rFactor1.RF1Spotter",
            "rf1_launch_exe", "rf1_launch_params", "launch_rfactor1");
        public static GameDefinition gameStockCar = new GameDefinition(GameEnum.RF1, Configuration.getUIString("gamestockcar"), "GSC", "CrewChiefV4.rFactor1.RF1Spotter",
            "gsc_launch_exe", "gsc_launch_params", "launch_gsc");
        public static GameDefinition automobilista = new GameDefinition(GameEnum.RF1, Configuration.getUIString("automobilista"), "AMS", "CrewChiefV4.rFactor1.RF1Spotter",
            "ams_launch_exe", "ams_launch_params", "launch_ams");
        public static GameDefinition marcas = new GameDefinition(GameEnum.RF1, Configuration.getUIString("marcas"), "MARCAS", "CrewChiefV4.rFactor1.RF1Spotter",
            "marcas_launch_exe", "marcas_launch_params", "launch_marcas");
        public static GameDefinition ftruck = new GameDefinition(GameEnum.RF1, Configuration.getUIString("ftruck"), "FTRUCK", "CrewChiefV4.rFactor1.RF1Spotter",
            "ftruck_launch_exe", "ftruck_launch_params", "launch_ftruck");
        public static GameDefinition assetto = new GameDefinition(GameEnum.ASSETTO, Configuration.getUIString("assetto"), "acs", "CrewChiefV4.assetto.ACSSpotter",
            "acs_launch_exe", "acs_launch_params", "launch_acs");


        public static List<GameDefinition> getAllGameDefinitions()
        {
            List<GameDefinition> definitions = new List<GameDefinition>();
            definitions.Add(automobilista); definitions.Add(gameStockCar); definitions.Add(marcas); definitions.Add(ftruck);
            definitions.Add(pCars64Bit); definitions.Add(pCars32Bit); definitions.Add(raceRoom); definitions.Add(pCarsNetwork); definitions.Add(rFactor1);
            definitions.Add(assetto);
            return definitions;
        }

        public static GameDefinition getGameDefinitionForFriendlyName(String friendlyName)
        {
            List<GameDefinition> definitions = getAllGameDefinitions();
            foreach (GameDefinition def in definitions)
            {
                if (def.friendlyName == friendlyName)
                {
                    return def;
                }
            }
            return null;
        }

        public static GameDefinition getGameDefinitionForEnumName(String enumName)
        {
            List<GameDefinition> definitions = getAllGameDefinitions();
            foreach (GameDefinition def in definitions)
            {
                if (def.gameEnum.ToString() == enumName)
                {
                    return def;
                }
            }
            return null;
        }

        public static String[] getGameDefinitionFriendlyNames()
        {
            List<String> names = new List<String>();
            foreach (GameDefinition def in getAllGameDefinitions())
            {
                names.Add(def.friendlyName);
            }
            return names.ToArray();
        }

        public GameEnum gameEnum;
        public String friendlyName;
        public String processName;
        public String spotterName;
        public String gameStartCommandProperty;
        public String gameStartCommandOptionsProperty;
        public String gameStartEnabledProperty;

        public GameDefinition(GameEnum gameEnum, String friendlyName, String processName, 
            String spotterName, String gameStartCommandProperty, String gameStartCommandOptionsProperty, String gameStartEnabledProperty)
        {
            this.gameEnum = gameEnum;
            this.friendlyName = friendlyName;
            this.processName = processName;            
            this.spotterName = spotterName;
            this.gameStartCommandProperty = gameStartCommandProperty;
            this.gameStartCommandOptionsProperty = gameStartCommandOptionsProperty;
            this.gameStartEnabledProperty = gameStartEnabledProperty;
        }
    }
}
