using System;
using System.Collections.Generic;

namespace Misc.BgStats.PlayService.Model
{
    public class Play
    {
        #region Properties
        public int Id { get; set; }
        public int ObjectId { get; set; }
        public DateTime Date { get; set; }
        public int Quantity { get; set; }
        public string Location { get; set; }
        #endregion

        #region Navigation Properties
        public List<Player> Players { get; set; }
        #endregion
    }
}
