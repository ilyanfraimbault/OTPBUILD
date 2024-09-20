using System.Text.Json;
using Camille.RiotGames.MatchV5;
using Champion = Camille.Enums.Champion;
using Team = Camille.RiotGames.Enums.Team;

namespace OTPBUILD.Models;

public class GameParticipant
{
    public string SummonerName { get; }
    public string SummonerId { get; }

    public Champion Champion { get; }
    public Team TeamId { get; }
    public string TeamPosition { get; }
    
    public int Kills { get; }
    public int Deaths { get; }
    public int Assists { get; }
    
    public List<int> Items { get; }
    public (int, int) SummonerSpells { get; }
    public Perks GamePerks { get; }

    public GameParticipant(Participant participant)
    {
        Champion = participant.ChampionId;
        TeamId = participant.TeamId;
        Kills = participant.Kills;
        Deaths = participant.Deaths;
        Assists = participant.Assists;
        Items =
        [
            participant.Item0, participant.Item1, participant.Item2, participant.Item3, participant.Item4,
            participant.Item5, participant.Item6
        ];
        SummonerSpells = (participant.Summoner1Casts, participant.Summoner2Casts);
        GamePerks = participant.Perks;
        TeamPosition = participant.TeamPosition;

        SummonerName = participant.SummonerName;
        SummonerId = participant.SummonerId;
    }

    public override string ToString()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(this, options);
    }
}