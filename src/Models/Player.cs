using Camille.Enums;
using Camille.RiotGames.SummonerV4;

namespace OTPBUILD.Models;

public class Player(Summoner summoner, Champion champion)
{
    public Summoner Summoner { get; } = summoner;
    public Champion Champion { get; } = champion;
}