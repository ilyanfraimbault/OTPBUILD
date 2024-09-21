using Camille.RiotGames.MatchV5;
using MySql.Data.MySqlClient;
using OTPBUILD.Configurations;
using OTPBUILD.Models;

namespace OTPBUILD.Services;

public class DatabaseService
{
    private readonly DatabaseConfig _databaseConfig;

    public DatabaseService(DatabaseConfig databaseConfig)
    {
        _databaseConfig = databaseConfig;
    }

    public void InsertGame(Game game)
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

        foreach (var participant in game.Participants)
        {
            InsertParticipant(participant, game);
        }

        command.ExecuteNonQuery();
    }

    private void InsertParticipant(GameParticipant participant, Game game)
    {
        using var connection = _databaseConfig.GetConnection();
        connection.Open();

        var participantQuery =
            "INSERT INTO Participants " +
            "VALUES (@GameId, @SummonerPuuid, @Champion, @TeamId, @Kills, @Deaths, @Assists, " +
            "@Item0, @Item1, @Item2, @Item3, @Item4, @Item5, @Item6, " +
            "@SpellCast1, @SpellCast2, @SpellCast3, @SpellCast4, @SummonerSpell1, @SummonerSpell2, @Perks, @TeamPosition)";
        using var participantCommand = new MySqlCommand(participantQuery, connection);
        participantCommand.Parameters.AddWithValue("@GameId", game.GameId);
        participantCommand.Parameters.AddWithValue("@SummonerPuuid", participant.Puuid);
        participantCommand.Parameters.AddWithValue("@Champion", participant.Champion);
        participantCommand.Parameters.AddWithValue("@TeamId", participant.TeamId);
        participantCommand.Parameters.AddWithValue("@Kills", participant.Kills);
        participantCommand.Parameters.AddWithValue("@Deaths", participant.Deaths);
        participantCommand.Parameters.AddWithValue("@Assists", participant.Assists);
        participantCommand.Parameters.AddWithValue("@Item0", participant.Items[0]);
        participantCommand.Parameters.AddWithValue("@Item1", participant.Items[1]);
        participantCommand.Parameters.AddWithValue("@Item2", participant.Items[2]);
        participantCommand.Parameters.AddWithValue("@Item3", participant.Items[3]);
        participantCommand.Parameters.AddWithValue("@Item4", participant.Items[4]);
        participantCommand.Parameters.AddWithValue("@Item5", participant.Items[5]);
        participantCommand.Parameters.AddWithValue("@Item6", participant.Items[6]);
        participantCommand.Parameters.AddWithValue("@SpellCast1", participant.SpellsCasts[0]);
        participantCommand.Parameters.AddWithValue("@SpellCast2", participant.SpellsCasts[1]);
        participantCommand.Parameters.AddWithValue("@SpellCast3", participant.SpellsCasts[2]);
        participantCommand.Parameters.AddWithValue("@SpellCast4", participant.SpellsCasts[3]);
        participantCommand.Parameters.AddWithValue("@SummonerSpell1", participant.SummonerSpells.Item1);
        participantCommand.Parameters.AddWithValue("@SummonerSpell2", participant.SummonerSpells.Item2);
        participantCommand.Parameters.AddWithValue("@Perks", InsertPerks(participant.Perks, connection));
        participantCommand.Parameters.AddWithValue("@TeamPosition", participant.TeamPosition);

        participantCommand.ExecuteNonQuery();
    }

    private int InsertStatPerks(PerkStats statPerks, MySqlConnection? connection = null)
    {
        using (connection ??= _databaseConfig.GetConnection())
        {
            connection.Open();
            var statPerksQuery = "INSERT INTO StatPerks (defense, flex, offense) VALUES (@defense, @flex, @offense)";
            using var statPerksCommand = new MySqlCommand(statPerksQuery, connection);
            statPerksCommand.Parameters.AddWithValue("@defense", statPerks.Defense);
            statPerksCommand.Parameters.AddWithValue("@flex", statPerks.Flex);
            statPerksCommand.Parameters.AddWithValue("@offense", statPerks.Offense);
            statPerksCommand.ExecuteNonQuery();
            return (int)statPerksCommand.LastInsertedId;
        }
    }

    private int InsertStyleSelection(PerkStyleSelection styleSelection, MySqlConnection? connection = null)
    {
        using (connection ??= _databaseConfig.GetConnection())
        {
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
    }

    private int InsertPerksStyle(PerkStyle style, MySqlConnection? connection = null)
    {
        using (connection ??= _databaseConfig.GetConnection())
        {
            connection.Open();
            var styleQuery =
                "INSERT INTO PerksStyle (description, style, styleSelection1, styleSelection2, styleSelection3, styleSelection4) " +
                "VALUES (@description, @style, @styleSelection1, @styleSelection2, @styleSelection3, @styleSelection4)";
            using var styleCommand = new MySqlCommand(styleQuery, connection);
            styleCommand.Parameters.AddWithValue("@style", style.Description);
            styleCommand.Parameters.AddWithValue("@selections", style.Selections);
            for (var i = 0; i < 4; i++)
            {
                styleCommand.Parameters.AddWithValue($"@styleSelection{i + 1}",
                    style.Selections.Length >= i ? InsertStyleSelection(style.Selections[i], connection) : null);
            }

            styleCommand.ExecuteNonQuery();
            return (int)styleCommand.LastInsertedId;
        }
    }

    private int InsertPerks(Perks perks, MySqlConnection? connection = null)
    {
        using (connection ??= _databaseConfig.GetConnection())
        {
            connection.Open();
            var perksQuery =
                "INSERT INTO Perks (statPerks, primaryStyle, secondaryStyle) VALUES (@statPerks, @primaryStyle, @secondaryStyle)";
            using var perksCommand = new MySqlCommand(perksQuery, connection);
            perksCommand.Parameters.AddWithValue("@statPerks", InsertStatPerks(perks.StatPerks, connection));
            perksCommand.Parameters.AddWithValue("@primaryStyle", InsertPerksStyle(perks.Styles[0], connection));
            perksCommand.Parameters.AddWithValue("@secondaryStyle", InsertPerksStyle(perks.Styles[1], connection));

            perksCommand.ExecuteNonQuery();
            return (int)perksCommand.LastInsertedId;
        }
    }
}