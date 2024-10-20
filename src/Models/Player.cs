using Camille.Enums;
using Camille.RiotGames.SummonerV4;

namespace OTPBUILD.Models;

public class Player
{
    public List<Summoner> Summoners { get; }
    public string? Name { get; }
    public string? TwitchChannel { get; }
    public Dictionary<Champion, double> Champions { get; }

    public Player(List<Summoner> summoners, Dictionary<Champion, double> champions, string? name = null, string? twitchChannel = null)
    {
        Summoners = summoners;
        Champions = champions;
        Name = name;
        TwitchChannel = twitchChannel;
    }

    public Player(Summoner summoner, Champion champion, double playRate, string? name = null, string? twitchChannel = null)
    {
        Summoners = [summoner];
        Champions = new Dictionary<Champion, double> {{champion, playRate}};
        TwitchChannel = twitchChannel;
        Name = name;
    }
}