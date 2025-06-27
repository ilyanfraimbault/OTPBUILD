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
    public PlatformRoute PlatformRoute { get; }

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
        PlatformRoute = Enum.Parse<PlatformRoute>(match.Info.PlatformId);

        Winner = match.Info.Teams.First(team => team.Win).TeamId;
        Participants = match.Info.Participants.Select(participant => new GameParticipant(participant)).ToList();
    }

    public Game(Game game)
    {
        GameDuration = game.GameDuration;
        GameStartTimestamp = game.GameStartTimestamp;
        GameId = game.GameId;
        GameVersion = game.GameVersion;
        GameType = game.GameType;

        MatchId = game.MatchId;
        PlatformRoute = game.PlatformRoute;

        Winner = game.Winner;
        Participants = new List<GameParticipant>(game.Participants);
    }

    public Game(Match match, Timeline timeline) : this(match)
    {
        var participantIdToPuuid = new Dictionary<int, string>();

        foreach (var participant in match.Info.Participants)
            participantIdToPuuid[participant.ParticipantId] = participant.Puuid;

        foreach (var frameTimeLine in timeline.Info.Frames)
        {
            foreach (var eventsTimeLine in frameTimeLine.Events)
            {
                if ((eventsTimeLine.Type is not ("ITEM_PURCHASED" or "ITEM_SOLD" or "ITEM_DESTROYED")) ||
                    eventsTimeLine is not { ParticipantId: not null, ItemId: not null }) continue;

                var itemEvent = new ItemEvent(
                    participantIdToPuuid[eventsTimeLine.ParticipantId.Value], eventsTimeLine.Type,
                    (int)eventsTimeLine.ItemId, eventsTimeLine.Timestamp);

                var participant = Participants.FirstOrDefault(p => p.Puuid == itemEvent.Puuid);

                if (participant != null)
                {
                    participant.ItemEvents ??= new List<ItemEvent>();
                    participant.ItemEvents.Add(itemEvent);
                }
            }
        }
    }

    public Game(Game game, Timeline timeline) : this(game)
    {
        var gameParticipants = new Dictionary<int, GameParticipant>();
        foreach (var participantTime in timeline.Info.Participants)
        {
            try
            {
                var participant = game.Participants.First(p => p.Puuid == participantTime.Puuid);
                if (participant != null)
                {
                    gameParticipants[participantTime.ParticipantId] = participant;
                }
            }
            catch (Exception e)
            {
                throw new Exception($"Error while mapping participants from timeline to game participants. {participantTime.Puuid} : {game.GameId}", e);
            }
        }


        foreach (var frameTimeLine in timeline.Info.Frames)
        {
            foreach (var eventsTimeLine in frameTimeLine.Events)
            {
                if (eventsTimeLine.Type is not ("ITEM_PURCHASED" or "ITEM_SOLD" or "ITEM_DESTROYED") ||
                    eventsTimeLine is not { ParticipantId: not null, ItemId: not null }) continue;

                if (eventsTimeLine.ParticipantId.Value < 0 ||
                    eventsTimeLine.ParticipantId.Value >= timeline.Info.Participants.Length)
                    continue;

                var itemEvent = new ItemEvent(
                    timeline.Info.Participants[eventsTimeLine.ParticipantId.Value].Puuid, eventsTimeLine.Type,
                    (int)eventsTimeLine.ItemId, eventsTimeLine.Timestamp);

                var participant = gameParticipants[eventsTimeLine.ParticipantId.Value];

                if (participant == null) continue;
                participant.ItemEvents ??= new List<ItemEvent>();
                participant.ItemEvents.Add(itemEvent);
            }
        }
    }

    public Game(
        long gameDuration, long gameStartTimestamp, long gameId, string gameVersion, GameType gameType, string matchId,
        PlatformRoute platformRoute, Team winner, List<GameParticipant> participants
        )
    {
        GameDuration = gameDuration;
        GameStartTimestamp = gameStartTimestamp;
        GameId = gameId;
        GameVersion = gameVersion;
        GameType = gameType;
        MatchId = matchId;
        PlatformRoute = platformRoute;
        Winner = winner;
        Participants = participants;
    }

    public override string ToString()
    {
        var options = new JsonSerializerOptions { WriteIndented = true };
        return JsonSerializer.Serialize(this, options);
    }
}