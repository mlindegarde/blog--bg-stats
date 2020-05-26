using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;
using Flurl;
using Misc.BgStats.PlayService.Config;
using Misc.BgStats.PlayService.Model;
using Serilog;

namespace Misc.BgStats.PlayService.Services
{
    public class BoardGameGeekService
    {
        #region Member Variables
        private ProgramConfig _config;
        private readonly ILogger _logger;
        #endregion

        #region Constructor
        public BoardGameGeekService(ProgramConfig config, ILogger logger)
        {
            _config = config;
            _logger = logger;
        }
        #endregion

        public async Task<GetPlaysResult> GetPlaysAsync(int id, int page)
        {
            Url url =
                "https://www.boardgamegeek.com"
                    .AppendPathSegment("xmlapi2")
                    .AppendPathSegment("plays")
                    .SetQueryParam("id", id)
                    .SetQueryParam("page", page);

            var (statusCode, totalPlays, plays) = await GetPageResultsAsync(id, url.ToUri());

            if ((int) statusCode == 429)
                return new GetPlaysResult {TooManyRequests = true};

            if (statusCode != HttpStatusCode.OK)
                return new GetPlaysResult {WasSuccessful = false};

            return
                new GetPlaysResult
                {
                    WasSuccessful = true,
                    TotalCount = totalPlays,
                    Page = page,
                    Plays = plays
                };
        }

        public async Task<GetPlaysResult> GetPlaysAsync(int id, DateTime minDate, DateTime maxDate, int page)
        {
            Url url =
                "https://www.boardgamegeek.com"
                    .AppendPathSegment("xmlapi2")
                    .AppendPathSegment("plays")
                    .SetQueryParam("id", id)
                    .SetQueryParam("page", page)
                    .SetQueryParam("mindate", minDate.ToString("yyyy-MM-dd"))
                    .SetQueryParam("maxdate", maxDate.ToString("yyyy-MM-dd"));

            var (statusCode, totalPlays, plays) = await GetPageResultsAsync(id, url.ToUri());

            if ((int)statusCode == 429)
                return new GetPlaysResult { TooManyRequests = true };

            if (statusCode != HttpStatusCode.OK)
                return new GetPlaysResult { WasSuccessful = false };

            return
                new GetPlaysResult
                {
                    WasSuccessful = true,
                    TotalCount = totalPlays,
                    MinDate = minDate,
                    MaxDate = maxDate,
                    Page = page,
                    Plays = plays
                };
        }

        #region Utility Methods
        private async Task<(HttpStatusCode, int, List<Play>)> GetPageResultsAsync(int id, Uri uri)
        {
            _logger.Information("Loading plays from {Uri}", uri);

            using (HttpClient client = new HttpClient())
            {
                HttpResponseMessage response = await client.GetAsync(uri);

                if (!response.IsSuccessStatusCode)
                    return (response.StatusCode, 0, new List<Play>());

                byte[] bytes = await response.Content.ReadAsByteArrayAsync();
                string xml = Encoding.UTF8.GetString(bytes);

                XElement plays = XElement.Parse(xml);

                return
                (
                    response.StatusCode,
                    Int32.Parse(plays.Attribute("total")?.Value ?? "0"),
                    plays.Descendants("play")
                        .Select(
                            play =>
                                new Play
                                {
                                    Id = Int32.Parse(play.Attribute("id")?.Value ?? "0"),
                                    ObjectId = id,
                                    Date = DateTime.TryParse(play.Attribute("date")?.Value ?? "1981-10-09", out DateTime parsedDate)? parsedDate : DateTime.MinValue,
                                    Quantity = Int32.Parse(play.Attribute("quantity")?.Value ?? "0"),
                                    Location = play.Attribute("location")?.Value,
                                    Players =
                                        play.XPathSelectElements("players/player")
                                            .Select(
                                                player =>
                                                    new Player
                                                    {
                                                        Username = player.Attribute("username")?.Value,
                                                        UserId = Int32.Parse(player.Attribute("userid")?.Value ?? "0"),
                                                        Name = player.Attribute("name")?.Value,
                                                        Score = Int32.TryParse(player.Attribute("score")?.Value ?? "0", out int parsedScore)? parsedScore : 0,
                                                        Rating = Int32.Parse(player.Attribute("rating")?.Value ?? "0"),
                                                        DidWin = player.Attribute("win")?.Value == "1"
                                                    })
                                            .ToList()
                                })
                        .ToList()
                );
            }
        }
        #endregion
    }
}
