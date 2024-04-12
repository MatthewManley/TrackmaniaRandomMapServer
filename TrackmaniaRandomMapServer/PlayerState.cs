// See https://aka.ms/new-console-template for more information
class PlayerState
{
    public string NickName { get; set; }
    public bool VoteGoldSkip { get; set; } = false;
    public bool VoteSkip { get; set; } = false;
    public bool VoteQuit { get; set; } = false;
}
