namespace TrackmaniaRandomMapServer.Models
{
    public enum Difficulty : int
    {
        None, //TODO: what is none? why did I add this?
        Finish,
        Bronze,
        Silver,
        Gold,
        Author
    }

    public static class DifficultyExtensions
    {
        public static string MedalString(this Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.None => "MedalSlot",
                Difficulty.Finish => "MedalSlot",
                Difficulty.Bronze => "MedalBronze",
                Difficulty.Silver => "MedalSilver",
                Difficulty.Gold => "MedalGold",
                Difficulty.Author => "MedalNadeo",
                _ => throw new System.NotImplementedException()
            };
        }

        public static string HexColor(this Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.None => "101010",
                Difficulty.Finish => "101010",
                Difficulty.Bronze => "784427",
                Difficulty.Silver => "899A9A",
                Difficulty.Gold => "B59232",
                Difficulty.Author => "005142",
                _ => throw new System.NotImplementedException()
            };
        }

        public static string DisplayName(this Difficulty difficulty)
        {
            return difficulty switch
            {
                Difficulty.None => "Finish",
                Difficulty.Finish => "Finish",
                Difficulty.Bronze => "Bronze",
                Difficulty.Silver => "Silver",
                Difficulty.Gold => "Gold",
                Difficulty.Author => "Author",
                _ => throw new System.NotImplementedException()
            };
        }
    }
}
