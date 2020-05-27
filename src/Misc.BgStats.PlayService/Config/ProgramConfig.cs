namespace Misc.BgStats.PlayService.Config
{
    public class ProgramConfig
    {
        #region Properties
        public int UpdateDelayInMinutes { get; set; }
        public int IncrementalSpanInDays { get; set; }
        public bool OnlyUpdateOncePerDay { get; set; }
        #endregion
    }
}
