using System.Data;
using Camille.Enums;
using Camille.RiotGames.AccountV1;
using Camille.RiotGames.MatchV5;
using MySql.Data.MySqlClient;
using OTPBUILD.Configurations;
using OTPBUILD.Models;
using Team = Camille.RiotGames.Enums.Team;

namespace OTPBUILD.Services;

public class DatabaseService
{
    private readonly DatabaseConfig _databaseConfig;

    public DatabaseService(DatabaseConfig databaseConfig)
    {
        _databaseConfig = databaseConfig;
    }

    public int InsertGame(Game game)
    {
        using var connection = _databaseConfig.GetConnection();
        connection.Open();

        var query = "CALL insertGame(@GameId, @GameDuration, @GameStartTimestamp, @GameVersion, @GameType, @PlatformId, @Winner, @MatchId)";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@GameId", game.GameId);
        command.Parameters.AddWithValue("@GameDuration", game.GameDuration);
        command.Parameters.AddWithValue("@GameStartTimestamp", game.GameStartTimestamp);
        command.Parameters.AddWithValue("@GameVersion", game.GameVersion);
        command.Parameters.AddWithValue("@GameType", game.GameType);
        command.Parameters.AddWithValue("@PlatformId", game.PlatformId);
        command.Parameters.AddWithValue("@Winner", game.Winner);
        command.Parameters.AddWithValue("@MatchId", game.MatchId);
        var result = command.ExecuteNonQuery();

        if (result <= 0) return result;

        foreach (var participant in game.Participants) InsertParticipant(participant, game);

        return result;
    }

    public int InsertAccount(Account account)
    {
        using var connection = _databaseConfig.GetConnection();
        connection.Open();

        var query = "CALL insertAccount(@Puuid, @GameName, @TagLine)";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Puuid", account.Puuid);
        command.Parameters.AddWithValue("@GameName", account.GameName);
        command.Parameters.AddWithValue("@TagLine", account.TagLine);

        return command.ExecuteNonQuery();
    }

    public int InsertParticipant(GameParticipant participant, Game game)
    {
        var perksId = InsertPerks(participant.Perks);

        using var connection = _databaseConfig.GetConnection();
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
        for (int i = 0; i < 7; i++)
            participantCommand.Parameters.AddWithValue($"@Item{i}", participant.Items[i]);
        for (int i = 0; i < 4; i++)
            participantCommand.Parameters.AddWithValue($"@SpellCast{i + 1}", participant.SpellsCasts[i]);
        participantCommand.Parameters.AddWithValue("@SummonerSpell1", participant.SummonerSpells.Item1);
        participantCommand.Parameters.AddWithValue("@SummonerSpell2", participant.SummonerSpells.Item2);
        participantCommand.Parameters.AddWithValue("@Perks", perksId);
        participantCommand.Parameters.AddWithValue("@TeamPosition", participant.TeamPosition);
        participantCommand.Parameters.AddWithValue("@PlatformId", game.PlatformId);

        return participantCommand.ExecuteNonQuery();
    }

    public int InsertStatPerks(PerkStats stats)
    {
        using var connection = _databaseConfig.GetConnection();
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
        using var connection = _databaseConfig.GetConnection();
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
        List<int> styleSelectionIds = new List<int>();

        foreach (var perkStyleSelection in style.Selections)
        {
            styleSelectionIds.Add(InsertStyleSelection(perkStyleSelection));
        }

        using var connection = _databaseConfig.GetConnection();
        connection.Open();

        var query = "CALL insertPerksStyle(@Description, @Style, @StyleSelection1, @StyleSelection2, @StyleSelection3, @StyleSelection4, @Id)";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Description", style.Description);
        command.Parameters.AddWithValue("@Style", style.Style);
        for (var i = 0; i < 4; i++)
        {
            command.Parameters.AddWithValue($"@StyleSelection{i + 1}", styleSelectionIds.Count > i ? styleSelectionIds[i] : DBNull.Value);
        }

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

        using var connection = _databaseConfig.GetConnection();
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
        using var connection = _databaseConfig.GetConnection();
        connection.Open();

        var query = "CALL insertPlayer(@PlayerName, @TwitchChannel)";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@PlayerName", player.Name);
        command.Parameters.AddWithValue("@TwitchChannel", player.TwitchChannel);

        return command.ExecuteNonQuery() + player.Champions.Sum(
            champion => InsertPlayerChampion(player, champion.Key, champion.Value)
            );
    }

    private int InsertPlayerChampion(Player player, Champion champion, double playRate)
    {
        using var connection = _databaseConfig.GetConnection();
        connection.Open();

        var query = "CALL insertPlayerChampion(@PlayerName, @Champion, @PlayRate)";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@PlayerName", player.Name);
        command.Parameters.AddWithValue("@Champion", champion);
        command.Parameters.AddWithValue("@PlayRate", playRate);

        return command.ExecuteNonQuery();
    }

    public Game? GetGame(long gameId)
    {
        using var connection = _databaseConfig.GetConnection();
        connection.Open();

        var query = "CALL GetGame(@GameId)";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@GameId", gameId);

        using var reader = command.ExecuteReader();
        if (!reader.HasRows) return null;
        Game? game = null;
        while (reader.Read())
        {
            game ??= new Game(
                reader.GetInt32("GameDuration"),
                reader.GetInt64("GameStartTimestamp"),
                reader.GetInt64("GameId"),
                reader.GetString("GameVersion"),
                Enum.Parse<GameType>(reader.GetString("GameType")),
                reader.GetString("MatchId"),
                reader.GetString("PlatformId"),
                Enum.Parse<Team>(reader.GetInt32("Winner").ToString()),
                []
            );

            PerkStats perkStats = new PerkStats
            {
                Defense = reader.GetInt32("defense"),
                Flex = reader.GetInt32("flex"),
                Offense = reader.GetInt32("offense")
            };

            PerkStyle primaryStyle = new PerkStyle
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

            PerkStyle secondaryStyle = new PerkStyle
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

            Perks perks = new Perks
            {
                StatPerks = perkStats,
                Styles = [primaryStyle, secondaryStyle]
            };

            GameParticipant participant = new GameParticipant(
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
        }

        return game;
    }

    public Player? GetPlayer(string playerName)
    {
        using var connection = _databaseConfig.GetConnection();
        connection.Open();

        var query = "CALL GetPlayer(@PlayerName)";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@PlayerName", playerName);

        using var reader = command.ExecuteReader();
        if (!reader.HasRows) return null;

        Player? player = null;
        while (reader.Read())
        {
            player ??= new Player(reader.GetString("PlayerName"), reader.GetString("TwitchChannel"));
            var champion = (Champion)Enum.Parse(typeof(Champion), reader.GetInt32("Champion").ToString());
            player.Champions.Add(champion, reader.GetDouble("PlayRate"));
        }

        return player;
    }
}