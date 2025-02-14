using Camille.Enums;
using Camille.RiotGames;
using Camille.RiotGames.LeagueV4;
using Camille.RiotGames.SummonerV4;
using OTPBUILD.Models;

namespace OTPBUILD.Services;

public class FetchOtps
{
    public Dictionary<PlatformRoute, List<Summoner>> Mains { get; }
    public List<Player> Players { get; }
    public Champion Champion { get; }
    private readonly RiotGamesApi _riotApi;
    public List<PlatformRoute> PlatformRoutes { get; }

    public FetchOtps(Champion champion, RiotGamesApi riotApi)
    {
        Champion = champion;
        _riotApi = riotApi;
        PlatformRoutes = [];
        Mains = new();
        Players = [];
    }

    public FetchOtps(Champion champion, RiotGamesApi riotApi, List<PlatformRoute> platformRoutes)
        : this(champion, riotApi)
    {
        PlatformRoutes = platformRoutes;
    }

    public FetchOtps(Champion champion, RiotGamesApi riotApi, PlatformRoute platformRoute)
        : this(champion, riotApi)
    {
        PlatformRoutes = [platformRoute];
    }

    public int FindMains()
    {
        var count = 0;
        foreach (var platform in PlatformRoutes)
        {
            List<Summoner> list = [];
            Console.WriteLine($"Fetching Mains for {platform}");

            var leagueEntriesList = GetEntries(platform, 20);

            Console.WriteLine($"Found {leagueEntriesList.Count} entries");
            Summoner summoner;
            foreach (var entry in leagueEntriesList)
            {
                Console.WriteLine($"Checking Main for {entry.SummonerId}");
                var summonerId = entry.SummonerId;
                try
                {
                    summoner = _riotApi.SummonerV4().GetBySummonerId(platform, summonerId);
                }
                catch (Exception e)
                {
                    continue;
                }

                if (IsMain(summoner, platform))
                {
                    list.Add(summoner);
                    count++;
                    Console.WriteLine(
                        $"Found Main AccountId : {summoner.AccountId}\n LP : {entry.LeaguePoints}\n isVeteran : {entry.Veteran}\n Ratio : {entry.Wins}/{entry.Losses} ({entry.Wins / (entry.Wins + entry.Losses)})");
                }
            }

            Mains.Add(platform, list);
        }

        return count;
    }

    private List<LeagueItem> GetEntries(PlatformRoute platform, int amount)
    {
        var challengerLeague = _riotApi.LeagueV4().GetChallengerLeague(platform, QueueType.RANKED_SOLO_5x5);
        var grandmasterLeague = _riotApi.LeagueV4().GetGrandmasterLeague(platform, QueueType.RANKED_SOLO_5x5);
        var masterLeague = _riotApi.LeagueV4().GetMasterLeague(platform, QueueType.RANKED_SOLO_5x5);
        var leagueEntriesList = challengerLeague.Entries
            .Concat(grandmasterLeague.Entries).ToList()
            .Concat(masterLeague.Entries).ToList();
        leagueEntriesList.Sort((a, b) => b.LeaguePoints.CompareTo(a.LeaguePoints));
        leagueEntriesList = leagueEntriesList[..Math.Min(leagueEntriesList.Count, amount)];
        return leagueEntriesList;
    }

    public int FindPlayers()
    {
        var count = 0;
        foreach (var entry in Mains)
        {
            List<Player> list = [];
            foreach (var main in entry.Value)
            {
                Console.WriteLine($"Fetching Player : {main.Puuid}");
                var playRate = GetPlayRate(main, entry.Key.ToRegional(), 20);
                Console.WriteLine($"PlayRate : {playRate}");
                if (playRate > 0.1)
                {
                    var accountName = _riotApi.AccountV1().GetByPuuid(entry.Key.ToRegional(), main.Puuid).GameName;
                    if (accountName is null) continue;
                    var player =
                        new Player(
                            new Dictionary<PlatformRoute, List<Summoner>>
                                { { entry.Key, new List<Summoner> { main } } },
                            new Dictionary<Champion, double> { { Champion, playRate } },
                            accountName);
                    list.Add(player);
                    count++;
                }
            }

            Players.AddRange(list);
        }

        return count;
    }

    public double GetPlayRate(Summoner summoner, RegionalRoute route, int amount = 50)
    {
        var matchList = _riotApi.MatchV5().GetMatchIdsByPUUID(route, summoner.Puuid, amount);
        var games = GetGames(route, matchList);

        var gamesPlayed = games.Count;
        var gamesPlayedWithChampion = GetGamesPlayingChampion(summoner, route, matchList).Count;
        return (double)gamesPlayedWithChampion / gamesPlayed;
    }

    public bool IsMain(Summoner summoner, PlatformRoute platform)
    {
        var championMasteries = _riotApi.ChampionMasteryV4()
            .GetChampionMasteryByPUUID(platform, summoner.Puuid, Champion);
        if (championMasteries is null) return false;
        var lastPlayTime = DateTimeOffset.FromUnixTimeMilliseconds(championMasteries.LastPlayTime);
        return championMasteries.ChampionPoints > 100000 && lastPlayTime > DateTimeOffset.Now.AddDays(-7);
    }

    public List<Game> GetGames(RegionalRoute platform, string[] matchIds)
    {
        List<Game> games = [];
        foreach (var matchId in matchIds)
        {
            var match = _riotApi.MatchV5().GetMatch(platform, matchId);
            if (match is not null) games.Add(new Game(match));
        }

        return games;
    }

    public List<Game> GetGamesPlayingChampion(Summoner summoner, RegionalRoute platform, string[]? matchIds = null)
    {
        List<Game> games = [];
        foreach (var matchId in matchIds ?? _riotApi.MatchV5().GetMatchIdsByPUUID(platform, summoner.Puuid, 50))
        {
            var match = _riotApi.MatchV5().GetMatch(platform, matchId);
            if (match is not null)
            {
                var game = new Game(match);
                if (game.IsPlayingChampion(Champion, summoner.Id)) games.Add(game);
            }
        }

        return games;
    }
}