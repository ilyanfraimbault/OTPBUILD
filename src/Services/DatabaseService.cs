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

        var query =
            "INSERT INTO Games (GameId, GameDuration, GameStartTimestamp, GameVersion, GameType, PlatformId, Winner, MatchId) " +
            "VALUES (@GameId, @GameDuration, @GameStartTimestamp, @GameVersion, @GameType, @PlatformId, @Winner, @MatchId)";

        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@GameId", game.GameId);
        command.Parameters.AddWithValue("@GameDuration", game.GameDuration);
        command.Parameters.AddWithValue("@GameStartTimestamp", game.GameStartTimestamp);
        command.Parameters.AddWithValue("@GameVersion", game.GameVersion);
        command.Parameters.AddWithValue("@GameType", game.GameType);
        command.Parameters.AddWithValue("@PlatformId", game.PlatformId);
        command.Parameters.AddWithValue("@Winner", game.Winner);
        command.Parameters.AddWithValue("@MatchId", game.MatchId);
        int result;
        try
        {
            result = command.ExecuteNonQuery();
        }
        catch (MySqlException e)
        {
            return -1;
        }

        if (result <= 0) return result;

        foreach (var participant in game.Participants)
        {
            InsertParticipant(participant, game);
        }

        return result;
    }

    public int InsertAccount(Account account)
    {
        using var connection = _databaseConfig.GetConnection();
        connection.Open();

        var query = "INSERT INTO Accounts (Puuid, GameName, TagLine) VALUES (@Puuid, @GameName, @TagLine)";
        using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Puuid", account.Puuid);
        command.Parameters.AddWithValue("@GameName", account.GameName);
        command.Parameters.AddWithValue("@TagLine", account.TagLine);

        return command.ExecuteNonQuery();
    }

    private int InsertParticipant(GameParticipant participant, Game game)
    {
        var perksId = InsertPerks(participant.Perks);

        using var connection = _databaseConfig.GetConnection();
        connection.Open();

        var participantQuery =
            "CALL insertParticipant(@GameId, @SummonerPuuid, @SummonerId, @GameName, @TagLine, @Champion, @TeamId, @Kills, @Deaths, @Assists, " +
            "@Item0, @Item1, @Item2, @Item3, @Item4, @Item5, @Item6, " +
            "@SpellCast1, @SpellCast2, @SpellCast3, @SpellCast4, @SummonerSpell1, @SummonerSpell2, @Perks, @TeamPosition)";
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

        return participantCommand.ExecuteNonQuery();
    }

    private int InsertStatPerks(PerkStats statPerks)
    {
        using var connection = _databaseConfig.GetConnection();
        connection.Open();

        var statPerksQuery = "INSERT INTO StatPerks (defense, flex, offense) VALUES (@defense, @flex, @offense)";
        using var statPerksCommand = new MySqlCommand(statPerksQuery, connection);
        statPerksCommand.Parameters.AddWithValue("@defense", statPerks.Defense);
        statPerksCommand.Parameters.AddWithValue("@flex", statPerks.Flex);
        statPerksCommand.Parameters.AddWithValue("@offense", statPerks.Offense);

        statPerksCommand.ExecuteNonQuery();

        return (int)statPerksCommand.LastInsertedId;
    }

    private int InsertStyleSelection(PerkStyleSelection styleSelection)
    {
        using var connection = _databaseConfig.GetConnection();
        connection.Open();

        var styleSelectionQuery = "INSERT INTO StyleSelection (perk, var1, var2, var3) " +
                                  "VALUES (@perk, @var1, @var2, @var3)";
        using var styleSelectionCommand = new MySqlCommand(styleSelectionQuery, connection);
        styleSelectionCommand.Parameters.AddWithValue("@perk", styleSelection.Perk);
        styleSelectionCommand.Parameters.AddWithValue("@var1", styleSelection.Var1);
        styleSelectionCommand.Parameters.AddWithValue("@var2", styleSelection.Var2);
        styleSelectionCommand.Parameters.AddWithValue("@var3", styleSelection.Var3);

        styleSelectionCommand.ExecuteNonQuery();
        return (int)styleSelectionCommand.LastInsertedId;
    }

    private int InsertPerksStyle(PerkStyle style)
    {
        List<int> styleSelectionIds = [];

        foreach (var perkStyleSelection in style.Selections)
        {
            styleSelectionIds.Add(InsertStyleSelection(perkStyleSelection));
        }

        using var connection = _databaseConfig.GetConnection();
        connection.Open();

        var styleQuery =
            "INSERT INTO PerksStyle (description, style, styleSelection1, styleSelection2, styleSelection3, styleSelection4) " +
            "VALUES (@description, @style, @styleSelection1, @styleSelection2, @styleSelection3, @styleSelection4)";

        using var styleCommand = new MySqlCommand(styleQuery, connection);
        styleCommand.Parameters.AddWithValue("@style", style.Style);
        styleCommand.Parameters.AddWithValue("@description", style.Description);
        for (var i = 0; i < 4; i++)
        {
            styleCommand.Parameters.AddWithValue($"@styleSelection{i + 1}",
                styleSelectionIds.Count > i ? styleSelectionIds[i] : null);
        }

        styleCommand.ExecuteNonQuery();
        return (int)styleCommand.LastInsertedId;
    }

    private int InsertPerks(Perks perks)
    {
        var statPerksId = InsertStatPerks(perks.StatPerks);
        var primaryStyleId = InsertPerksStyle(perks.Styles[0]);
        var secondaryStyleId = InsertPerksStyle(perks.Styles[1]);

        using var connection = _databaseConfig.GetConnection();
        connection.Open();

        var perksQuery =
            "INSERT INTO Perks (statPerks, primaryStyle, secondaryStyle) VALUES (@statPerks, @primaryStyle, @secondaryStyle)";
        using var perksCommand = new MySqlCommand(perksQuery, connection);
        perksCommand.Parameters.AddWithValue("@statPerks", statPerksId);
        perksCommand.Parameters.AddWithValue("@primaryStyle", primaryStyleId);
        perksCommand.Parameters.AddWithValue("@secondaryStyle", secondaryStyleId);

        perksCommand.ExecuteNonQuery();
        return (int)perksCommand.LastInsertedId;
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
}