using Camille.Enums;
using Camille.RiotGames.SummonerV4;

namespace OTPBUILD.Models;

public class Player
{
    public Dictionary<PlatformRoute, List<Summoner>> Summoners { get; }
    public string Name { get; }
    public string? TwitchChannel { get; }
    public Dictionary<Champion, double> Champions { get; }

    public Player(
        Dictionary<PlatformRoute, List<Summoner>> summoners, Dictionary<Champion, double> champions, string name,
        string? twitchChannel = null
        )
    {
        Summoners = summoners;
        Champions = champions;
        Name = name;
        TwitchChannel = twitchChannel;
    }

    public Player(
        PlatformRoute platformRoute, Summoner summoner, Champion champion, double playRate, string name, string? twitchChannel = null
        )
    {
        Summoners = new Dictionary<PlatformRoute, List<Summoner>>();
        Summoners[platformRoute] = new List<Summoner> { summoner };
        Champions = new Dictionary<Champion, double> { { champion, playRate } };
        TwitchChannel = twitchChannel;
        Name = name;
    }

    public Player(string name, string? twitchChannel = null) : this([],
        new Dictionary<Champion, double>(), name: name, twitchChannel: twitchChannel)
    {
    }
}