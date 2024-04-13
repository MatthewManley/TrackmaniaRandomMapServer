public class PlayerState
{
    public string NickName { get; set; }
    public bool VoteGoldSkip { get; set; } = false;
    public bool VoteSkip { get; set; } = false;
    public bool VoteQuit { get; set; } = false;
    public bool IsSpectator { get; set; } = false;
    public int NumWins { get; set; } = 0;
    public int GoodSkips { get; set; } = 0;
    public int? BestMapTime { get; set; } = null;
}
