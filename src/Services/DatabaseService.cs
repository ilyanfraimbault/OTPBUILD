using System.Data;
using Camille.Enums;
using Camille.RiotGames.AccountV1;
using Camille.RiotGames.MatchV5;
using Camille.RiotGames.SummonerV4;
using MySql.Data.MySqlClient;
using OTPBUILD.Configurations;
using OTPBUILD.Models;
using Team = Camille.RiotGames.Enums.Team;

namespace OTPBUILD.Services;

public class DatabaseService(DatabaseConfig databaseConfig)
{
    public int InsertGame(Game game)
    {
        using var connection = databaseConfig.GetConnection();
        connection.Open();

        var query =
            "CALL insertGame(@GameId, @GameDuration, @GameStartTimestamp, @GameVersion, @GameType, @PlatformId, @Winner, @MatchId)";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@GameId", game.GameId);
        command.Parameters.AddWithValue("@GameDuration", game.GameDuration);
        command.Parameters.AddWithValue("@GameStartTimestamp", game.GameStartTimestamp);
        command.Parameters.AddWithValue("@GameVersion", game.GameVersion);
        command.Parameters.AddWithValue("@GameType", game.GameType);
        command.Parameters.AddWithValue("@PlatformId", game.PlatformRoute);
        command.Parameters.AddWithValue("@Winner", game.Winner);
        command.Parameters.AddWithValue("@MatchId", game.MatchId);
        var result = command.ExecuteNonQuery();

        if (result <= 0) return result;

        foreach (var participant in game.Participants) InsertParticipant(participant, game);

        return result;
    }

    public int InsertAccount(Account account)
    {
        using var connection = databaseConfig.GetConnection();
        connection.Open();

        var query = "CALL insertAccount(@Puuid, @GameName, @TagLine)";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Puuid", account.Puuid);
        command.Parameters.AddWithValue("@GameName", account.GameName);
        command.Parameters.AddWithValue("@TagLine", account.TagLine);

        return command.ExecuteNonQuery();
    }

    public int InsertSummoner(Summoner summoner, PlatformRoute platformRoute, string? playerName = null)
    {
        using var connection = databaseConfig.GetConnection();
        connection.Open();

        var query =
            "CALL insertSummoner(@SummonerId, @Puuid, @Name, @AccountId, @ProfileIconId, @RevisionDate, @SummonerLevel, @PlayerName, @PlatformId)";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@SummonerId", summoner.Id);
        command.Parameters.AddWithValue("@Puuid", summoner.Puuid);
        command.Parameters.AddWithValue("@Name", DBNull.Value);
        command.Parameters.AddWithValue("@AccountId", summoner.AccountId);
        command.Parameters.AddWithValue("@ProfileIconId", summoner.ProfileIconId);
        command.Parameters.AddWithValue("@RevisionDate", summoner.RevisionDate);
        command.Parameters.AddWithValue("@SummonerLevel", summoner.SummonerLevel);
        command.Parameters.AddWithValue("@PlayerName", playerName);
        command.Parameters.AddWithValue("@PlatformId", platformRoute.ToString());
        return command.ExecuteNonQuery();
    }

    public int InsertParticipant(GameParticipant participant, Game game)
    {
        var perksId = InsertPerks(participant.Perks);

        using var connection = databaseConfig.GetConnection();
        connection.Open();

        var participantQuery =
            "CALL insertParticipant(@GameId, @SummonerPuuid, @SummonerId, @GameName, @TagLine, @Champion, @TeamId, @Kills, @Deaths, @Assists, " +
            "@Item0, @Item1, @Item2, @Item3, @Item4, @Item5, @Item6, " +
            "@SpellCast1, @SpellCast2, @SpellCast3, @SpellCast4, @SummonerSpell1, @SummonerSpell2, @Perks, @TeamPosition, @PlatformId)";
        using var participantCommand = new MySqlCommand(participantQuery, connection);
        participantCommand.Parameters.AddWithValue("@GameId", game.GameId);
        participantCommand.Parameters.AddWithValue("@SummonerPuuid", participant.Puuid);
        participantCommand.Parameters.AddWithValue("@SummonerId", participant.SummonerId);
        participantCommand.Parameters.AddWithValue("@GameName", participant.RiotIdGameName);
        participantCommand.Parameters.AddWithValue("@TagLine", participant.RiotIdTagline);
        participantCommand.Parameters.AddWithValue("@Champion", participant.Champion);
        participantCommand.Parameters.AddWithValue("@TeamId", participant.TeamId);
        participantCommand.Parameters.AddWithValue("@Kills", participant.Kills);
        participantCommand.Parameters.AddWithValue("@Deaths", participant.Deaths);
        participantCommand.Parameters.AddWithValue("@Assists", participant.Assists);
        for (var i = 0; i < 7; i++)
            participantCommand.Parameters.AddWithValue($"@Item{i}", participant.Items[i]);
        for (var i = 0; i < 4; i++)
            participantCommand.Parameters.AddWithValue($"@SpellCast{i + 1}", participant.SpellsCasts[i]);
        participantCommand.Parameters.AddWithValue("@SummonerSpell1", participant.SummonerSpells.Item1);
        participantCommand.Parameters.AddWithValue("@SummonerSpell2", participant.SummonerSpells.Item2);
        participantCommand.Parameters.AddWithValue("@Perks", perksId);
        participantCommand.Parameters.AddWithValue("@TeamPosition", participant.TeamPosition);
        participantCommand.Parameters.AddWithValue("@PlatformId", game.PlatformRoute);

        return participantCommand.ExecuteNonQuery();
    }

    public int InsertStatPerks(PerkStats stats)
    {
        using var connection = databaseConfig.GetConnection();
        connection.Open();

        var query = "CALL insertStatPerks(@Defense, @Flex, @Offense, @Id)";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Defense", stats.Defense);
        command.Parameters.AddWithValue("@Flex", stats.Flex);
        command.Parameters.AddWithValue("@Offense", stats.Offense);

        var idParam = new MySqlParameter("@Id", MySqlDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(idParam);

        command.ExecuteNonQuery();

        return (int)idParam.Value;
    }

    private int InsertStyleSelection(PerkStyleSelection styleSelection)
    {
        using var connection = databaseConfig.GetConnection();
        connection.Open();

        var query = "CALL insertStyleSelection(@Perk, @Var1, @Var2, @Var3, @Id)";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Perk", styleSelection.Perk);
        command.Parameters.AddWithValue("@Var1", styleSelection.Var1);
        command.Parameters.AddWithValue("@Var2", styleSelection.Var2);
        command.Parameters.AddWithValue("@Var3", styleSelection.Var3);

        var idParam = new MySqlParameter("@Id", MySqlDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(idParam);

        command.ExecuteNonQuery();

        return (int)idParam.Value;
    }

    private int InsertPerksStyle(PerkStyle style)
    {
        var styleSelectionIds = new List<int>();

        foreach (var perkStyleSelection in style.Selections)
            styleSelectionIds.Add(InsertStyleSelection(perkStyleSelection));

        using var connection = databaseConfig.GetConnection();
        connection.Open();

        var query =
            "CALL insertPerksStyle(@Description, @Style, @StyleSelection1, @StyleSelection2, @StyleSelection3, @StyleSelection4, @Id)";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Description", style.Description);
        command.Parameters.AddWithValue("@Style", style.Style);
        for (var i = 0; i < 4; i++)
            command.Parameters.AddWithValue($"@StyleSelection{i + 1}",
                styleSelectionIds.Count > i ? styleSelectionIds[i] : DBNull.Value);

        var idParam = new MySqlParameter("@Id", MySqlDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(idParam);

        command.ExecuteNonQuery();

        return (int)idParam.Value;
    }

    private int InsertPerks(Perks perks)
    {
        var statPerksId = InsertStatPerks(perks.StatPerks);
        var primaryStyleId = InsertPerksStyle(perks.Styles[0]);
        var secondaryStyleId = InsertPerksStyle(perks.Styles[1]);

        using var connection = databaseConfig.GetConnection();
        connection.Open();

        var query = "CALL insertPerks(@StatPerks, @PrimaryStyle, @SecondaryStyle, @Id)";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@StatPerks", statPerksId);
        command.Parameters.AddWithValue("@PrimaryStyle", primaryStyleId);
        command.Parameters.AddWithValue("@SecondaryStyle", secondaryStyleId);

        var idParam = new MySqlParameter("@Id", MySqlDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(idParam);

        command.ExecuteNonQuery();

        return (int)idParam.Value;
    }

    public int InsertPlayer(Player player)
    {
        using var connection = databaseConfig.GetConnection();
        connection.Open();

        var query = "CALL insertPlayer(@SummonerPuuid, @Champion)";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@SummonerPuuid", player.Summoner.Puuid);
        command.Parameters.AddWithValue("@Champion", (int)player.Champion);

        var res = command.ExecuteNonQuery();

        return res;
    }

    public Game? GetGame(long gameId)
    {
        using var connection = databaseConfig.GetConnection();
        connection.Open();

        var query = "CALL GetGame(@GameId)";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@GameId", gameId);

        using var reader = command.ExecuteReader();
        if (!reader.HasRows) return null;
        Game? game = null;

        reader.Read();

        game ??= new Game(
            reader.GetInt32("GameDuration"),
            reader.GetInt64("GameStartTimestamp"),
            reader.GetInt64("GameId"),
            reader.GetString("GameVersion"),
            Enum.Parse<GameType>(reader.GetString("GameType")),
            reader.GetString("MatchId"),
            Enum.Parse<PlatformRoute>(reader.GetString("PlatformId")),
            Enum.Parse<Team>(reader.GetInt32("Winner").ToString()),
            []
        );

        var perkStats = new PerkStats
        {
            Defense = reader.GetInt32("defense"),
            Flex = reader.GetInt32("flex"),
            Offense = reader.GetInt32("offense")
        };

        var primaryStyle = new PerkStyle
        {
            Style = reader.GetInt32("primaryStyle"),
            Description = reader.GetString("primaryStyleDescription"),
            Selections =
            [
                new PerkStyleSelection
                {
                    Perk = reader.GetInt32("primStyleSelection1"),
                    Var1 = reader.GetInt32("primStyleSelection1Var1"),
                    Var2 = reader.GetInt32("primStyleSelection1Var2"),
                    Var3 = reader.GetInt32("primStyleSelection1Var3")
                },
                new PerkStyleSelection
                {
                    Perk = reader.GetInt32("primStyleSelection2"),
                    Var1 = reader.GetInt32("primStyleSelection2Var1"),
                    Var2 = reader.GetInt32("primStyleSelection2Var2"),
                    Var3 = reader.GetInt32("primStyleSelection2Var3")
                },
                new PerkStyleSelection
                {
                    Perk = reader.GetInt32("primStyleSelection3"),
                    Var1 = reader.GetInt32("primStyleSelection3Var1"),
                    Var2 = reader.GetInt32("primStyleSelection3Var2"),
                    Var3 = reader.GetInt32("primStyleSelection3Var3")
                },
                new PerkStyleSelection
                {
                    Perk = reader.GetInt32("primStyleSelection4"),
                    Var1 = reader.GetInt32("primStyleSelection4Var1"),
                    Var2 = reader.GetInt32("primStyleSelection4Var2"),
                    Var3 = reader.GetInt32("primStyleSelection4Var3")
                }
            ]
        };

        var secondaryStyle = new PerkStyle
        {
            Style = reader.GetInt32("secondaryStyle"),
            Description = reader.GetString("secondaryStyleDescription"),
            Selections =
            [
                new PerkStyleSelection
                {
                    Perk = reader.GetInt32("secStyleSelection1"),
                    Var1 = reader.GetInt32("secStyleSelection1Var1"),
                    Var2 = reader.GetInt32("secStyleSelection1Var2"),
                    Var3 = reader.GetInt32("secStyleSelection1Var3")
                },
                new PerkStyleSelection
                {
                    Perk = reader.GetInt32("secStyleSelection2"),
                    Var1 = reader.GetInt32("secStyleSelection2Var1"),
                    Var2 = reader.GetInt32("secStyleSelection2Var2"),
                    Var3 = reader.GetInt32("secStyleSelection2Var3")
                }
            ]
        };

        var perks = new Perks
        {
            StatPerks = perkStats,
            Styles = [primaryStyle, secondaryStyle]
        };

        var participant = new GameParticipant(
            reader.GetString("SummonerName"),
            reader.GetString("SummonerId"),
            reader.GetInt32("SummonerLevel"),
            reader.GetString("SummonerPuuid"),
            Enum.Parse<Champion>(reader.GetInt32("Champion").ToString()),
            Enum.Parse<Team>(reader.GetInt32("TeamId").ToString()),
            reader.GetString("TeamPosition"),
            reader.GetInt32("Kills"),
            reader.GetInt32("Deaths"),
            reader.GetInt32("Assists"),
            [
                reader.GetInt32("Item0"), reader.GetInt32("Item1"), reader.GetInt32("Item2"),
                reader.GetInt32("Item3"),
                reader.GetInt32("Item4"), reader.GetInt32("Item5"), reader.GetInt32("Item6")
            ],
            [
                reader.GetInt32("SpellCast1"), reader.GetInt32("SpellCast2"), reader.GetInt32("SpellCast3"),
                reader.GetInt32("SpellCast4")
            ],
            (reader.GetInt32("SummonerSpell1"), reader.GetInt32("SummonerSpell2")),
            perks,
            reader.GetString("GameName"),
            reader.GetString("TagLine")
        );
        game.Participants.Add(participant);

        return game;
    }

    public List<(string, PlatformRoute)> GetMatchIds()
    {
        using var connection = databaseConfig.GetConnection();
        connection.Open();

        var query = "SELECT MatchId, PlatformId FROM Games";
        using var command = new MySqlCommand(query, connection);

        using var reader = command.ExecuteReader();
        List<(string, PlatformRoute)> matchIds = [];
        while (reader.Read())
        {
            var id = reader.GetString("MatchId");
            var platform = Enum.Parse<PlatformRoute>(reader.GetString("PlatformId"));
            matchIds.Add((id, platform));
        }

        return matchIds;
    }

    public Player? GetPlayer(string summonerPuuid)
    {
        using var connection = databaseConfig.GetConnection();
        connection.Open();

        var query =
            "SELECT S.*, Champion FROM Players P JOIN OTPBUILD.Summoners S on P.SummonerPuuid = S.Puuid WHERE SummonerPuuid = @SummonerPuuid";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@SummonerPuuid", summonerPuuid);

        using var reader = command.ExecuteReader();
        if (!reader.HasRows) return null;

        reader.Read();
        var champion = Enum.Parse<Champion>(reader.GetInt32("Champion").ToString());
        var summoner = CreateSummonerFromReader(reader);

        return new Player(summoner, champion);
    }

    public Dictionary<PlatformRoute, List<Player>> GetPlayers()
    {
        using var connection = databaseConfig.GetConnection();
        connection.Open();

        var query = "SELECT S.*, Champion FROM Players P JOIN OTPBUILD.Summoners S on P.SummonerPuuid = S.Puuid";
        using var command = new MySqlCommand(query, connection);

        using var reader = command.ExecuteReader();
        if (!reader.HasRows) return [];

        var players = new Dictionary<PlatformRoute, List<Player>>();

        while (reader.Read())
        {
            var champion = Enum.Parse<Champion>(reader.GetInt32("Champion").ToString());
            var summoner = CreateSummonerFromReader(reader);

            var platform = Enum.Parse<PlatformRoute>(reader.GetString("PlatformId"));

            if (!players.ContainsKey(platform)) players[platform] = [];

            players[platform].Add(new Player(summoner, champion));
        }

        return players;
    }

    public List<Game> GetPlayerGames(string playerName, Champion? champion = null)
    {
        using var connection = databaseConfig.GetConnection();
        connection.Open();

        var query = "CALL getPlayerGamesIds(@PlayerName, @Champion)";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@PlayerName", playerName);
        command.Parameters.AddWithValue("@Champion", champion != null ? (int)champion.Value : DBNull.Value);

        using var reader = command.ExecuteReader();

        var games = new List<Game>();
        if (!reader.HasRows) return games;

        while (reader.Read())
        {
            var game = GetGame(reader.GetInt64("GameId"));
            if (game != null) games.Add(game);
        }

        return games;
    }

    public List<(Summoner, PlatformRoute)> GetSummoners()
    {
        using var connection = databaseConfig.GetConnection();
        connection.Open();

        var query = "SELECT * FROM Summoners";
        using var command = new MySqlCommand(query, connection);

        using var reader = command.ExecuteReader();

        List<(Summoner, PlatformRoute)> summoners = [];
        if (!reader.HasRows) return summoners;

        while (reader.Read())
        {
            var summoner = CreateSummonerFromReader(reader);
            var platform = Enum.Parse<PlatformRoute>(reader.GetString("PlatformId"));

            summoners.Add((summoner, platform));
        }

        return summoners;
    }

    public List<(Summoner, PlatformRoute)> GetSummonerIdsOrderedByGamesPlayed()
    {
        using var connection = databaseConfig.GetConnection();
        connection.Open();

        var query = @"
            SELECT S.*
            FROM Summoners S
            LEFT JOIN Participants P on P.SummonerPuuid = S.Puuid
            GROUP BY S.Puuid
            ORDER BY COUNT(P.GameId)
            ";

        using var command = new MySqlCommand(query, connection);
        using var reader = command.ExecuteReader();

        var summoners = new List<(Summoner, PlatformRoute)>();

        while (reader.Read())
        {
            var summoner = CreateSummonerFromReader(reader);
            var platform = Enum.Parse<PlatformRoute>(reader.GetString("PlatformId"));

            summoners.Add((summoner, platform));
        }

        return summoners;
    }

    public List<string> GetSummonerIds()
    {
        using var connection = databaseConfig.GetConnection();
        connection.Open();

        var query = "SELECT Id FROM Summoners";
        using var command = new MySqlCommand(query, connection);

        using var reader = command.ExecuteReader();

        List<string> summonerIds = [];
        while (reader.Read()) summonerIds.Add(reader.GetString("Id"));

        return summonerIds;
    }

    private Summoner CreateSummonerFromReader(MySqlDataReader reader)
    {
        return new Summoner
        {
            SummonerLevel = reader.IsDBNull(reader.GetOrdinal("Level")) ? 0 : reader.GetInt32("Level"),
            Id = reader.IsDBNull(reader.GetOrdinal("Id")) ? string.Empty : reader.GetString("Id"),
            RevisionDate = reader.IsDBNull(reader.GetOrdinal("RevisionDate")) ? 0 : reader.GetInt64("RevisionDate"),
            Puuid = reader.IsDBNull(reader.GetOrdinal("Puuid")) ? string.Empty : reader.GetString("Puuid"),
            ProfileIconId = reader.IsDBNull(reader.GetOrdinal("ProfileIconId")) ? 0 : reader.GetInt32("ProfileIconId"),
            AccountId = reader.IsDBNull(reader.GetOrdinal("AccountId")) ? string.Empty : reader.GetString("AccountId")
        };
    }
}