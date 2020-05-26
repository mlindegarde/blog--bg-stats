using MongoDB.Bson;

namespace Misc.BgStats.PlayService.Model
{
    public class BoardGame
    {
        #region Properties
        public ObjectId Id { get; set; }

        public int ObjectId { get; set; }
        public string Name { get; set; }
        #endregion
    }
}
