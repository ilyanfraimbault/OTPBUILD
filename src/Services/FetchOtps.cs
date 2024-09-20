using Camille.Enums;
using Camille.RiotGames;
using Camille.RiotGames.MatchV5;
using Camille.RiotGames.SummonerV4;
using OTPBUILD.Models;

namespace OTPBUILD.Services;

public class FetchOtps
{
    private List<Otp> _otps = [];
    private Champion _champion;
    private RiotGamesApi _riotApi;
    private List<PlatformRoute> _platformRoutes;

    public FetchOtps(Champion champion, RiotGamesApi riotApi)
    {
        _champion = champion;
        _riotApi = riotApi;
        _platformRoutes = [];
    }

    public FetchOtps(Champion champion, RiotGamesApi riotApi, List<PlatformRoute> platformRoutes)
        : this(champion, riotApi)
    {
        _platformRoutes = platformRoutes;
    }

    public FetchOtps(Champion champion, RiotGamesApi riotApi, PlatformRoute platformRoute)
        : this(champion, riotApi)
    {
        _platformRoutes = [platformRoute];
    }

    public void FindOtps()
    {
        foreach (var platform in _platformRoutes)
        {
            Console.WriteLine($"Fetching OTPs for {platform}");
            var leagueList = _riotApi.LeagueV4().GetChallengerLeague(platform, QueueType.RANKED_SOLO_5x5);
            foreach (var leagueListEntry in leagueList.Entries)
            {
                var summonerId = leagueListEntry.SummonerId;
                var summoner = _riotApi.SummonerV4().GetBySummonerId(platform, summonerId);
                string[] matchIds = _riotApi.MatchV5().GetMatchIdsByPUUID(platform.ToRegional(), summoner.Puuid);
                var matches = GetMatchesWherePlayingChampion(summoner, platform.ToRegional(), matchIds);
                return;
                if (IsOtp(summoner, platform))
                {
                    Console.WriteLine($"Found OTP {summoner.Id}");
                    _otps.Add(new Otp(summoner, _champion, platform));
                }
            }
        }
    }

    public bool IsOtp(Summoner summoner, PlatformRoute platform)
    {
        var championMastery =
            _riotApi.ChampionMasteryV4().GetChampionMasteryByPUUID(platform, summoner.Puuid, _champion);
        if (championMastery == null)
        {
            return false;
        }

        return championMastery.ChampionPoints > 100000 &&
               DateTimeOffset.FromUnixTimeMilliseconds(championMastery.LastPlayTime).UtcDateTime >
               DateTimeOffset.Now.AddDays(-10);
    }

    public List<Match> GetMatchesWherePlayingChampion(Summoner summoner, RegionalRoute platform, string[] matchIds)
    {
        List<Match> matches = [];
        foreach (var matchId in matchIds)
        {
            var match = _riotApi.MatchV5().GetMatch(platform, matchId);
            if (match is not null && IsPlayingChampion(match, summoner)) matches.Add(match);
            if (match != null)
            {
                Game game = new Game(match);
                Console.WriteLine(game);
                return matches;
            }
        }

        return matches;
    }

    public bool IsPlayingChampion(Match Match, Summoner summoner)
    {
        foreach (var participant in Match.Info.Participants)
        {
            if (participant.Puuid == summoner.Puuid && participant.ChampionId == _champion)
            {
                return true;
            }
        }

        return false;
    }
}