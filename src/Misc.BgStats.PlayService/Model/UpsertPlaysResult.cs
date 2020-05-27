namespace Misc.BgStats.PlayService.Model
{
    public class UpsertPlaysResult
    {
        #region Properties
        public long MatchedCount { get; set; }
        public long ModifiedCount { get; set; }
        public long InsertedCount { get; set; }
        public bool WasSuccessful { get; set; }
        #endregion
    }
}
