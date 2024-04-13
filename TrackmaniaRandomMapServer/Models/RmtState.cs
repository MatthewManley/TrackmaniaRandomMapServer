namespace TrackmaniaRandomMapServer.Models
{
    public class RmtAction
    {
        public readonly string Action;

        public RmtAction(string action)
        {
            Action = action;
        }
    }

    public class PlayerConnectedAction : RmtAction
    {
        public readonly string PlayerLogin;

        public PlayerConnectedAction(string playerLogin) : base("PlayerConnected")
        {
            PlayerLogin = playerLogin;
        }
    }

    public class PlayerNicknameUpdatedAction : RmtAction
    {
        public readonly string PlayerLogin;
        public readonly string PlayerNickname;

        public PlayerNicknameUpdatedAction(string playerLogin, string playerNickname) : base("PlayerNicknameUpdated")
        {
            PlayerLogin = playerLogin;
            PlayerNickname = playerNickname;
        }
    }



    public class RmtState
    {
        private bool Running = false;
    }
}
