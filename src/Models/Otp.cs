using Camille.Enums;
using Camille.RiotGames.SummonerV4;

namespace OTPBUILD.Models;

public class Otp
{
    private List<Summoner> _summoners;
    private Champion _champion;
    private PlatformRoute _platformRoute;
    private string? _name;

    public Otp(List<Summoner> summoners, Champion champion, PlatformRoute platformRoute, string? name = null)
    {
        _summoners = summoners;
        _champion = champion;
        _name = name;
        _platformRoute = platformRoute;
    }

    public Otp(Summoner summoner, Champion champion, PlatformRoute platformRoute)
        : this([summoner], champion, platformRoute)
    {
    }
}