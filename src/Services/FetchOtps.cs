using Camille.Enums;
using Camille.RiotGames;
using Camille.RiotGames.ChampionMasteryV4;
using Camille.RiotGames.LeagueV4;
using Camille.RiotGames.SummonerV4;
using OTPBUILD.Models;

namespace OTPBUILD.Services;

public class FetchOtps(RiotGamesApi riotApi)
{
    public Dictionary<PlatformRoute, List<Player>> Players { get; } = new();
    public List<Champion> Champions { get; } = [];
    public List<PlatformRoute> PlatformRoutes { get; } = [];
    public Dictionary<Summoner, ChampionMastery[]> ChampionMasteries { get; } = new();

    public FetchOtps(List<Champion> champions, List<PlatformRoute> platformRoutes, RiotGamesApi riotApi)
        : this(riotApi)
    {
        Champions = champions;
        PlatformRoutes = platformRoutes;
    }

    public int FindPlayers()
    {
        var count = 0;
        foreach (var platform in PlatformRoutes)
        {
            List<Player> list = [];

            foreach (var champion in Champions)
            {
                foreach (var summoner in ChampionMasteries.Keys.Where(summoner => IsMain(summoner, champion)))
                {
                    var player = new Player(summoner, champion);
                    list.Add(player);
                    count++;
                }
            }
            Players.Add(platform, list);
        }

        return count;
    }

    private List<LeagueItem> GetEntries(PlatformRoute platform)
    {
        var challengerLeague = riotApi.LeagueV4().GetChallengerLeague(platform, QueueType.RANKED_SOLO_5x5);
        var grandmasterLeague = riotApi.LeagueV4().GetGrandmasterLeague(platform, QueueType.RANKED_SOLO_5x5);
        var masterLeague = riotApi.LeagueV4().GetMasterLeague(platform, QueueType.RANKED_SOLO_5x5);
        var leagueEntriesList = challengerLeague.Entries
            .Concat(grandmasterLeague.Entries).ToList()
            .Concat(masterLeague.Entries).ToList();

        return leagueEntriesList;
    }

    public void SetSummoners(int? limit = null, List<string>? summonerIds = null)
    {
        Dictionary<PlatformRoute, List<LeagueItem>> entries = [];

        foreach (var platform in PlatformRoutes)
        {
            List<LeagueItem> leagueEntries = [];

            try
            {
                leagueEntries = GetEntries(platform);
            }
            catch (Exception)
            {
                Console.WriteLine("Error while fetching entries");
            }

            if (summonerIds is not null)
                leagueEntries = leagueEntries
                    .Where(entry => !summonerIds.Contains(entry.SummonerId))
                    .OrderByDescending(entry => entry.LeaguePoints)
                    .ToList();
            else
                leagueEntries = leagueEntries
                    .OrderByDescending(entry => entry.LeaguePoints)
                    .ToList();

            if (limit is not null) leagueEntries = leagueEntries.Take(limit.Value).ToList();
            Console.WriteLine($"Platform: {platform} total entries: {leagueEntries.Count}");

            entries.Add(platform, leagueEntries);
        }

        Console.WriteLine("Starting to fetch summoners...");
        var countPlatform = 0;
        foreach (var (platform, platformEntries) in entries)
        {
            countPlatform++;
            var countLeagueItem = 0;
            foreach (var leagueItem in platformEntries)
            {
                countLeagueItem++;
                Console.WriteLine($"Platform: {countPlatform}/{entries.Count} leagueItem: {countLeagueItem}/{platformEntries.Count}");
                try
                {
                    var summoner = riotApi.SummonerV4().GetBySummonerId(platform, leagueItem.SummonerId);

                    var championMasteries = riotApi.ChampionMasteryV4().GetAllChampionMasteriesByPUUID(platform, summoner.Puuid);
                    ChampionMasteries.Add(summoner, championMasteries);
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
    }

    private bool IsMain(Summoner summoner, Champion champion)
    {
        var championMasteries = ChampionMasteries[summoner].FirstOrDefault(mastery => mastery.ChampionId == champion);
        if (championMasteries is null) return false;
        var lastPlayTime = DateTimeOffset.FromUnixTimeMilliseconds(championMasteries.LastPlayTime);
        return championMasteries.ChampionPoints > 300000 && lastPlayTime > DateTimeOffset.Now.AddDays(-7);
    }
}