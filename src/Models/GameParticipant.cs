using System.Text.Json;
using Camille.RiotGames.MatchV5;
using Champion = Camille.Enums.Champion;
using Team = Camille.RiotGames.Enums.Team;

namespace OTPBUILD.Models;

public class GameParticipant
{
    public string SummonerName { get; }
    public string SummonerId { get; }
    public int SummonerLevel { get; }
    public string Puuid { get; }
    public string? RiotIdGameName { get; }
    public string? RiotIdTagline { get; }

    public Champion Champion { get; }
    public Team TeamId { get; }
    public string TeamPosition { get; }

    public int Kills { get; }
    public int Deaths { get; }
    public int Assists { get; }

    public List<int> Items { get; }
    public List<int> SpellsCasts { get; }
    public (int, int) SummonerSpells { get; }
    public Perks Perks { get; }

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
        SpellsCasts =
            [participant.Spell1Casts, participant.Spell2Casts, participant.Spell3Casts, participant.Spell4Casts];
        SummonerSpells = (participant.Summoner1Id, participant.Summoner2Id);
        Perks = participant.Perks;
        TeamPosition = participant.TeamPosition;

        SummonerName = participant.SummonerName;
        SummonerId = participant.SummonerId;
        SummonerLevel = participant.SummonerLevel;
        Puuid = participant.Puuid;
        RiotIdGameName = participant.RiotIdGameName;
        RiotIdTagline = participant.RiotIdTagline;
    }

    public GameParticipant(
        string summonerName, string summonerId, int summonerLevel, string puuid, Champion champion, Team teamId,
        string teamPosition, int kills, int deaths, int assists, List<int> items, List<int> spellsCasts,
        (int, int) summonerSpells, Perks perks, string? riotIdGameName, string riotIdTagline
        )
    {
        SummonerName = summonerName;
        SummonerId = summonerId;
        SummonerLevel = summonerLevel;
        Puuid = puuid;
        Champion = champion;
        TeamId = teamId;
        TeamPosition = teamPosition;
        Kills = kills;
        Deaths = deaths;
        Assists = assists;
        Items = items;
        SpellsCasts = spellsCasts;
        SummonerSpells = summonerSpells;
        Perks = perks;
        RiotIdTagline = riotIdTagline;
        RiotIdGameName = riotIdGameName;
    }

    public override string ToString()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(this, options);
    }
}