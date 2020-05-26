using System;
using MongoDB.Bson;

namespace Misc.BgStats.PlayService.Model
{
    public class BoardGameStatus
    {
        #region Properties
        public ObjectId Id { get; set; }

        public int ObjectId { get; set; }
        public string BoardGameName { get; set; }
        public bool ImportSuccessful { get; set; }
        public DateTime LastUpdated { get; set; }
        #endregion
    }
}
