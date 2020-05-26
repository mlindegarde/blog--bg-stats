namespace Misc.BgStats.PlayService.Model
{
    public class Player
    {
        #region Properties
        public string Username { get; set; }
        public int UserId { get; set; }
        public string Name { get; set; }
        public int Score { get; set; }
        public int Rating { get; set; }
        public bool DidWin { get; set; }
        #endregion
    }
}
