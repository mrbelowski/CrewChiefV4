using CrewChiefV4.GameState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV4
{
    public enum GameEnum
    {
        RACE_ROOM, PCARS_64BIT, PCARS_32BIT, PCARS_NETWORK, RF1, ASSETTO_64BIT, ASSETTO_32BIT, RF2_64BIT
    }
    public class GameDefinition
    {
        public static GameDefinition pCars64Bit = new GameDefinition(GameEnum.PCARS_64BIT, "pcars_64_bit", "pCARS64",
            "CrewChiefV4.PCars.PCarsSpotterv2", "pcars64_launch_exe", "pcars64_launch_params", "launch_pcars", new String[] { "pCARS2", "pCARS2Gld", "pCARS2QA" });
        public static GameDefinition pCars32Bit = new GameDefinition(GameEnum.PCARS_32BIT, "pcars_32_bit", "pCARS",
            "CrewChiefV4.PCars.PCarsSpotterv2", "pcars32_launch_exe", "pcars32_launch_params", "launch_pcars");
        public static GameDefinition raceRoom = new GameDefinition(GameEnum.RACE_ROOM, "race_room", "RRRE", "CrewChiefV4.RaceRoom.R3ESpotterv2",
            "r3e_launch_exe", "r3e_launch_params", "launch_raceroom", new String[] {"RRRE64"});
        public static GameDefinition pCarsNetwork = new GameDefinition(GameEnum.PCARS_NETWORK, "pcars_udp", null, "CrewChiefV4.PCars.PCarsSpotterv2",
            null, null, null);
        public static GameDefinition rFactor1 = new GameDefinition(GameEnum.RF1, "rfactor1", "rFactor", "CrewChiefV4.rFactor1.RF1Spotter",
            "rf1_launch_exe", "rf1_launch_params", "launch_rfactor1", "rFactor");
        public static GameDefinition gameStockCar = new GameDefinition(GameEnum.RF1, "gamestockcar", "GSC", "CrewChiefV4.rFactor1.RF1Spotter",
            "gsc_launch_exe", "gsc_launch_params", "launch_gsc");
        public static GameDefinition automobilista = new GameDefinition(GameEnum.RF1, "automobilista", "AMS", "CrewChiefV4.rFactor1.RF1Spotter",
            "ams_launch_exe", "ams_launch_params", "launch_ams", "Automobilista");
        public static GameDefinition marcas = new GameDefinition(GameEnum.RF1, "marcas", "MARCAS", "CrewChiefV4.rFactor1.RF1Spotter",
            "marcas_launch_exe", "marcas_launch_params", "launch_marcas");
        public static GameDefinition ftruck = new GameDefinition(GameEnum.RF1, "ftruck", "FTRUCK", "CrewChiefV4.rFactor1.RF1Spotter",
            "ftruck_launch_exe", "ftruck_launch_params", "launch_ftruck");
        public static GameDefinition assetto64Bit = new GameDefinition(GameEnum.ASSETTO_64BIT, "assetto_64_bit", "acs", "CrewChiefV4.assetto.ACSSpotter",
            "acs_launch_exe", "acs_launch_params", "launch_acs", "assettocorsa");
        public static GameDefinition assetto32Bit = new GameDefinition(GameEnum.ASSETTO_32BIT, "assetto_32_bit", "acs_x86", "CrewChiefV4.assetto.ACSSpotter",
            "acs_launch_exe", "acs_launch_params", "launch_acs", "assettocorsa");
        public static GameDefinition rfactor2_64bit = new GameDefinition(GameEnum.RF2_64BIT, "rfactor2_64_bit", "rFactor2", "CrewChiefV4.rFactor2.RF2Spotter",
            "rf2_launch_exe", "rf2_launch_params", "launch_rfactor2", "rFactor 2");




        public static List<GameDefinition> getAllGameDefinitions()
        {
            List<GameDefinition> definitions = new List<GameDefinition>();
            definitions.Add(automobilista); definitions.Add(gameStockCar); definitions.Add(marcas); definitions.Add(ftruck);
            definitions.Add(pCars64Bit); definitions.Add(pCars32Bit); definitions.Add(raceRoom); definitions.Add(pCarsNetwork); definitions.Add(rFactor1);
            definitions.Add(assetto64Bit); definitions.Add(assetto32Bit); definitions.Add(rfactor2_64bit);
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
            names.Sort();
            return names.ToArray();
        }

        public GameEnum gameEnum;
        public String friendlyName;
        public String lookupName;
        public String processName;
        public String spotterName;
        public String gameStartCommandProperty;
        public String gameStartCommandOptionsProperty;
        public String gameStartEnabledProperty;
        public String gameInstallDirectory;
        public String[] alternativeProcessNames;

        public GameDefinition(GameEnum gameEnum, String lookupName, String processName,
            String spotterName, String gameStartCommandProperty, String gameStartCommandOptionsProperty, String gameStartEnabledProperty, String gameInstallDirectory = "")
        {
            this.gameEnum = gameEnum;
            this.lookupName = lookupName;
            this.friendlyName = Configuration.getUIString(lookupName);
            this.processName = processName;
            this.spotterName = spotterName;
            this.gameStartCommandProperty = gameStartCommandProperty;
            this.gameStartCommandOptionsProperty = gameStartCommandOptionsProperty;
            this.gameStartEnabledProperty = gameStartEnabledProperty;
            this.gameInstallDirectory = gameInstallDirectory;
        }

        public GameDefinition(GameEnum gameEnum, String lookupName, String processName,
            String spotterName, String gameStartCommandProperty, String gameStartCommandOptionsProperty, String gameStartEnabledProperty, String[] alternativeProcessNames,
            String gameInstallDirectory = "")
        {
            this.gameEnum = gameEnum;
            this.lookupName = lookupName;
            this.friendlyName = Configuration.getUIString(lookupName);
            this.processName = processName;
            this.spotterName = spotterName;
            this.gameStartCommandProperty = gameStartCommandProperty;
            this.gameStartCommandOptionsProperty = gameStartCommandOptionsProperty;
            this.gameStartEnabledProperty = gameStartEnabledProperty;
            this.alternativeProcessNames = alternativeProcessNames;
            this.gameInstallDirectory = gameInstallDirectory;
        }

        public bool HasAnyProcessNameAssociated()
        {
            return processName != null
                || (alternativeProcessNames != null && alternativeProcessNames.Length > 0);
        }
    }
}