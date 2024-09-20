using System.Text.Json;
using Camille.Enums;
using Camille.RiotGames.MatchV5;
using Team = Camille.RiotGames.Enums.Team;

namespace OTPBUILD.Models;

public class Game
{
    public long GameDuration { get; }
    public long GameStartTimestamp { get; }
    public long GameId { get; }
    public string GameVersion { get; }
    public GameType GameType { get; }

    public string MatchId { get; }
    public string PlatformId { get; }

    public Team Winner { get; }
    public List<GameParticipant> Participants { get; }

    public Game(Match match)
    {
        GameDuration = match.Info.GameDuration;
        GameStartTimestamp = match.Info.GameStartTimestamp;
        GameId = match.Info.GameId;
        GameVersion = match.Info.GameVersion;
        GameType = match.Info.GameType;

        MatchId = match.Metadata.MatchId;
        PlatformId = match.Info.PlatformId;

        Winner = match.Info.Teams.First(team => team.Win).TeamId;
        Participants = match.Info.Participants.Select(participant => new GameParticipant(participant)).ToList();
    }

    public override string ToString()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(this, options);
    }
}