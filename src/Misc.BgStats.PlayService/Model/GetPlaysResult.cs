using System;
using System.Collections.Generic;

namespace Misc.BgStats.PlayService.Model
{
    public class GetPlaysResult
    {
        #region Properties
        public bool WasSuccessful { get; set; }
        public bool TooManyRequests { get; set; }
        public int TotalCount { get; set; }
        public DateTime? MinDate { get; set; }
        public DateTime? MaxDate { get; set; }
        public int Page { get; set; }
        #endregion

        #region Navigation Properties
        public List<Play> Plays { get; set; }
        #endregion
    }
}
