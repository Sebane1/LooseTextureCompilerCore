using FFXIVLooseTextureCompiler.DataTypes;

namespace FFXIVLooseTextureCompiler.Racial
{
    public static class RaceInfo
    {
        private static List<string> subRaces = new List<string>() { "Midlander" , "Highlander","Wildwood","Duskwight","Plainsfolk", "Dunesfolk","Seeker",
            "Keeper", "Sea Wolf", "Hellsguard",  "Raen", "Xaela","Helions", "The Lost", "Rava", "Veena" };
        private static List<string> races = new List<string>() { "Midlander","Highlander","Elezen", "Lalafell", "Miqo'te","Roegadyn",
           "Raen","Xaela","Hrothgar","Viera"};
        private static List<string> modelRaces = new List<string>() { "Midlander","Highlander","Elezen","Lalafell","Miqote","Roegadyn",
            "Aura","Hrothgar","Viera",};

        private static RaceCode raceCodeBody = new RaceCode(new string[] {
            "0101","0301","0101","1101","0101","0901","1301","1301","1501","1701"}, new string[] {
            "0201","0401","0201","1101","0201","0401","1401","1401","1601","1801"});

        private static RaceCode raceCodeFace = new RaceCode(new string[] {
                "0101", "0301", "0501", "0501","1101", "1101", "0701",
                "0701", "0901", "0901",
                "1301", "1301", "1501", "1501", "1701", "1701" }, new string[] {
                "0201", "0401", "0601", "0601","1201", "1201", "0801",
                "0801", "1001", "1001",
                "1401", "1401", "1601", "1601", "1801", "1801" });

        private static List<RacialBodyIdentifiers> bodyIdentifiers = new List<RacialBodyIdentifiers>(){
            new RacialBodyIdentifiers("VANILLA",
                new List<string>() { "201", "401", "201", "Invalid", "201", "401", "1101", "1401", "1401", "1601", "1801" }),
            new RacialBodyIdentifiers("BIBO+",
                new List<string>() { "mid", "high", "mid", "Invalid", "mid", "high",  "raen", "xaela", "hroth", "viera" }),
            new RacialBodyIdentifiers("GEN3",
                new List<string>() { "mid", "high", "mid", "Invalid", "mid", "high", "raen", "xaela", "hroth", "viera" }),
            new RacialBodyIdentifiers("TBSE/HRBODY",
                new List<string>() { "0101", "0301", "0101", "Invalid", "0101", "0901", "1301", "1301", "1501", "1701" }),
            new RacialBodyIdentifiers("TAIL",
                new List<string>() { "1", "2", "3", "4", "5", "6", "7", "8", "", "" }) };

        public static RaceCode RaceCodeBody { get => raceCodeBody; set => raceCodeBody = value; }
        public static RaceCode RaceCodeFace { get => raceCodeFace; set => raceCodeFace = value; }
        internal static List<RacialBodyIdentifiers> BodyIdentifiers { get => bodyIdentifiers; set => bodyIdentifiers = value; }
        public static List<string> SubRaces { get => subRaces; set => subRaces = value; }
        public static List<string> Races { get => races; set => races = value; }
        public static List<string> ModelRaces { get => modelRaces; set => modelRaces = value; }

        public static string ReverseBodyLookup(string internalPath)
        {
            if (internalPath.Contains("bibo"))
            {
                return "bibo";
            }
            else if (internalPath.Contains("eve") || internalPath.Contains("gen3"))
            {
                return "gen3";
            }
            else if (internalPath.Contains("body"))
            {
                return "gen2";
            }
            else if (internalPath.Contains("skin_otopop") || internalPath.Contains("v01_c1101b0001_g"))
            {
                return "otopop";
            }
            else if (internalPath.Contains("1_b_d"))
            {
                return "tbse";
            }
            return "";
        }
        public static int ReverseFaceLookup(string path)
        {
            if (path.Contains("0001") || path.Contains("0101"))
            {
                return 1;
            }
            if (path.Contains("0002") || path.Contains("0102"))
            {
                return 2;
            }
            if (path.Contains("0003") || path.Contains("0103"))
            {
                return 3;
            }
            if (path.Contains("0004") || path.Contains("0104"))
            {
                return 4;
            }
            if (path.Contains("0005") || path.Contains("0105"))
            {
                return 5;
            }
            if (path.Contains("0006") || path.Contains("0106"))
            {
                return 5;
            }
            if (path.Contains("0007") || path.Contains("0107"))
            {
                return 5;
            }
            return 0;
        }

        public static int ReverseRaceLookup(string path)
        {
            if (!string.IsNullOrEmpty(path))
            {
                for (int i = 0; i < 10; i++)
                {
                    string bibo = bodyIdentifiers[1].RaceIdentifiers[i];
                    string gen3 = bodyIdentifiers[2].RaceIdentifiers[i];
                    string tbse = "c" + bodyIdentifiers[3].RaceIdentifiers[i];
                    if (path.Contains(bibo) || path.Contains(gen3) || path.Contains(tbse))
                    {
                        return i;
                    }
                }
                for (int i = 0; i < 10; i++)
                {
                    string vanilla = bodyIdentifiers[0].RaceIdentifiers[i];
                    if (!vanilla.Contains("Invalid"))
                    {
                        if (path.Contains("c" + NumberPadder(int.Parse(vanilla))))
                        {
                            if (path.Contains("c1401b0001"))
                            {
                                return 6;
                            }
                            else if (path.Contains("c1401b0101"))
                            {
                                return 7;
                            }
                            else
                            {
                                return i;
                            }
                        }
                    }
                }
                if (path.Contains("1101") || path.Contains("otopop"))
                {
                    return 5;
                }
            }
            return -1;
        }
        public static string NumberPadder(int value)
        {
            return value.ToString().PadLeft(4, '0');
        }
        public static int SubRaceToMainRace(int subRace)
        {
            switch (subRace)
            {
                case 0:
                    return 0;
                case 1:
                    return 1;
                case 2:
                case 3:
                    return 2;
                case 4:
                case 5:
                    return 3;
                case 6:
                case 7:
                    return 4;
                case 8:
                case 9:
                    return 5;
                case 10:
                    return 6;
                case 11:
                    return 7;
                case 12:
                case 13:
                    return 8;
                case 14:
                case 15:
                    return 9;
            }
            return -1;
        }
        public static int SubRaceToModelRace(int subRace)
        {
            switch (subRace)
            {
                case 0:
                    return 0;
                case 1:
                    return 1;
                case 2:
                case 3:
                    return 2;
                case 4:
                case 5:
                    return 3;
                case 6:
                case 7:
                    return 4;
                case 8:
                case 9:
                    return 5;
                case 10:
                case 11:
                    return 6;
                case 12:
                case 13:
                    return 7;
                case 14:
                case 15:
                    return 8;
            }
            return -1;
        }
        public enum RaceTypes
        {
            Midlander = 0, Highlander = 1, Elezen = 2, Lalafell = 3, Miqote = 4, Roegadyn = 5,
            Raen = 6, Xaela = 7, Hrothgar = 8, Viera = 9
        }
        public enum SubRaceTypes
        {
            Midlander = 0, Highlander = 1, Wildwood = 2, Duskwight = 3, Plainsfolk = 4, Dunesfolk = 5, Seeker = 6,
            Keeper = 7, SeaWolf = 8, Hellsguard = 9, Raen = 10, Xaela = 11, Helions = 12, TheLost = 13, Rava = 14, Veena = 15
        }
    }
}
