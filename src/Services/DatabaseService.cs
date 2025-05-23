using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using Camille.Enums;
using Camille.RiotGames.AccountV1;
using Camille.RiotGames.MatchV5;
using Camille.RiotGames.SummonerV4;
using MySql.Data.MySqlClient;
using OTPBUILD.Models;
using Team = Camille.RiotGames.Enums.Team;

namespace OTPBUILD.Services;

public class DatabaseService(DatabaseConnection databaseConnection)
{
    public async Task<int> InsertGameAsync(Game game)
    {
        await using var connection = databaseConnection.GetConnection();
        await connection.OpenAsync();

        var query =
            "CALL insertGame(@GameId, @GameDuration, @GameStartTimestamp, @GameVersion, @GameType, @PlatformId, @Winner, @MatchId)";

        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@GameId", game.GameId);
        command.Parameters.AddWithValue("@GameDuration", game.GameDuration);
        command.Parameters.AddWithValue("@GameStartTimestamp", game.GameStartTimestamp);
        command.Parameters.AddWithValue("@GameVersion", game.GameVersion);
        command.Parameters.AddWithValue("@GameType", game.GameType);
        command.Parameters.AddWithValue("@PlatformId", game.PlatformRoute.ToString());
        command.Parameters.AddWithValue("@Winner", game.Winner);
        command.Parameters.AddWithValue("@MatchId", game.MatchId);
        var result = await command.ExecuteNonQueryAsync();

        if (result <= 0) return result;

        var participantsTasks =
            game.Participants.Select(participant => InsertParticipantAsync(participant, game)).ToList();

        var results = await Task.WhenAll(participantsTasks);

        return results.Sum(participant => participant) + result;
    }

    public async Task<int> InsertAccountAsync(Account account)
    {
        await using var connection = databaseConnection.GetConnection();
        await connection.OpenAsync();

        var query = "UPDATE Summoners SET GameName = @GameName, TagLine = @TagLine WHERE Puuid = @Puuid";
        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Puuid", account.Puuid);
        command.Parameters.AddWithValue("@GameName", account.GameName);
        command.Parameters.AddWithValue("@TagLine", account.TagLine);

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<int> InsertSummonerAsync(Summoner summoner, PlatformRoute platformRoute)
    {
        await using var connection = databaseConnection.GetConnection();
        await connection.OpenAsync();

        var query =
            "CALL insertSummoner(@SummonerId, @Puuid, @Name, @AccountId, @ProfileIconId, @RevisionDate, @SummonerLevel, @PlatformId, @GameName, @TagLine)";
        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@SummonerId", summoner.Id);
        command.Parameters.AddWithValue("@Puuid", summoner.Puuid);
        command.Parameters.AddWithValue("@Name", DBNull.Value);
        command.Parameters.AddWithValue("@AccountId", summoner.AccountId);
        command.Parameters.AddWithValue("@ProfileIconId", summoner.ProfileIconId);
        command.Parameters.AddWithValue("@RevisionDate", summoner.RevisionDate);
        command.Parameters.AddWithValue("@SummonerLevel", summoner.SummonerLevel);
        command.Parameters.AddWithValue("@PlatformId", platformRoute.ToString());
        command.Parameters.AddWithValue("@GameName", DBNull.Value);
        command.Parameters.AddWithValue("@TagLine", DBNull.Value);

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<int> InsertParticipantAsync(GameParticipant participant, Game game)
    {
        await using var connection = databaseConnection.GetConnection();
        await connection.OpenAsync();

        var perksId = await InsertPerksAsync(participant.Perks);

        var participantQuery =
            "CALL insertParticipant(@GameId, @SummonerPuuid, @SummonerId, @SummonerLevel, @SummonerName, @GameName, @TagLine, @Champion, @TeamId, @Kills, @Deaths, @Assists, " +
            "@Item0, @Item1, @Item2, @Item3, @Item4, @Item5, @Item6, " +
            "@SpellCast1, @SpellCast2, @SpellCast3, @SpellCast4, @SummonerSpell1, @SummonerSpell2, @Perks, @TeamPosition, @PlatformId)";
        await using var participantCommand = new MySqlCommand(participantQuery, connection);
        participantCommand.Parameters.AddWithValue("@GameId", game.GameId);
        participantCommand.Parameters.AddWithValue("@SummonerPuuid", participant.Puuid);
        participantCommand.Parameters.AddWithValue("@SummonerId", participant.SummonerId);
        participantCommand.Parameters.AddWithValue("@SummonerLevel", participant.SummonerLevel);
        participantCommand.Parameters.AddWithValue("@SummonerName", participant.SummonerName);
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
        participantCommand.Parameters.AddWithValue("@PlatformId", game.PlatformRoute.ToString());

        return await participantCommand.ExecuteNonQueryAsync();
    }

    private async Task<int> InsertPerksAsync(Perks perks)
    {
        var statPerksId = await InsertStatPerksAsync(perks.StatPerks);
        var primaryStyleId = await InsertPerksStyleAsync(perks.Styles[0]);
        var secondaryStyleId = await InsertPerksStyleAsync(perks.Styles[1]);

        await using var connection = databaseConnection.GetConnection();
        await connection.OpenAsync();

        var query = "CALL insertPerks(@StatPerks, @PrimaryStyle, @SecondaryStyle, @Id)";
        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@StatPerks", statPerksId);
        command.Parameters.AddWithValue("@PrimaryStyle", primaryStyleId);
        command.Parameters.AddWithValue("@SecondaryStyle", secondaryStyleId);

        var idParam = new MySqlParameter("@Id", MySqlDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(idParam);

        await command.ExecuteNonQueryAsync();

        return (int)idParam.Value;
    }

    private async Task<int> InsertPerksStyleAsync(PerkStyle style)
    {
        await using var connection = databaseConnection.GetConnection();
        await connection.OpenAsync();

        var query =
            "CALL insertPerksStyle(@Description, @Style, @StyleSelection1, @StyleSelection2, @StyleSelection3, @StyleSelection4, @Id)";
        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Description", style.Description);
        command.Parameters.AddWithValue("@Style", style.Style);
        for (var i = 0; i < 4; i++)
            command.Parameters.AddWithValue($"@StyleSelection{i + 1}",
                style.Selections.Length > i ? style.Selections[i].Perk : DBNull.Value);

        var idParam = new MySqlParameter("@Id", MySqlDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(idParam);

        await command.ExecuteNonQueryAsync();

        return (int)idParam.Value;
    }

    private async Task<int> InsertStatPerksAsync(PerkStats stats)
    {
        await using var connection = databaseConnection.GetConnection();
        await connection.OpenAsync();

        var query = "CALL insertStatPerks(@Defense, @Flex, @Offense, @Id)";
        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Defense", stats.Defense);
        command.Parameters.AddWithValue("@Flex", stats.Flex);
        command.Parameters.AddWithValue("@Offense", stats.Offense);

        var idParam = new MySqlParameter("@Id", MySqlDbType.Int32)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(idParam);

        await command.ExecuteNonQueryAsync();

        return (int)idParam.Value;
    }

    public async Task<int> InsertPlayerAsync(Player player, PlatformRoute platformRoute)
    {
        await InsertSummonerAsync(player.Summoner, platformRoute);

        await using var connection = databaseConnection.GetConnection();
        await connection.OpenAsync();

        var query = "CALL insertPlayer(@SummonerPuuid, @Champion)";
        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@SummonerPuuid", player.Summoner.Puuid);
        command.Parameters.AddWithValue("@Champion", (int)player.Champion);

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<ConcurrentDictionary<PlatformRoute, ConcurrentBag<Account>>> GetAccountsAsync()
    {
        var accounts = new ConcurrentDictionary<PlatformRoute, ConcurrentBag<Account>>();

        await using var connection = databaseConnection.GetConnection();
        await connection.OpenAsync();

        var query = "SELECT PlatformId, Puuid, GameName, TagLine FROM Summoners";
        await using var command = new MySqlCommand(query, connection);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var platformId = Enum.Parse<PlatformRoute>(reader.GetString("PlatformId"));

            var account = new Account
            {
                Puuid = reader.GetString("Puuid"),
                GameName = reader.IsDBNull(reader.GetOrdinal("GameName")) ? null : reader.GetString("GameName"),
                TagLine = reader.IsDBNull(reader.GetOrdinal("TagLine")) ? null : reader.GetString("TagLine")
            };

            if (!accounts.TryGetValue(platformId, out ConcurrentBag<Account>? value))
            {
                value = [];
                accounts[platformId] = value;
            }

            value.Add(account);
        }

        return accounts;
    }

    private Task<GameParticipant> ReadParticipantFromReaderAsync(DbDataReader reader)
    {
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
                    Var1 = 0,
                    Var2 = 0,
                    Var3 = 0
                },
                new PerkStyleSelection
                {
                    Perk = reader.GetInt32("primStyleSelection2"),
                    Var1 = 0,
                    Var2 = 0,
                    Var3 = 0
                },
                new PerkStyleSelection
                {
                    Perk = reader.GetInt32("primStyleSelection3"),
                    Var1 = 0,
                    Var2 = 0,
                    Var3 = 0
                },
                new PerkStyleSelection
                {
                    Perk = reader.GetInt32("primStyleSelection4"),
                    Var1 = 0,
                    Var2 = 0,
                    Var3 = 0
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
                    Var1 = 0,
                    Var2 = 0,
                    Var3 = 0
                },
                new PerkStyleSelection
                {
                    Perk = reader.GetInt32("secStyleSelection2"),
                    Var1 = 0,
                    Var2 = 0,
                    Var3 = 0
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
                reader.GetInt32("Item3"), reader.GetInt32("Item4"), reader.GetInt32("Item5"),
                reader.GetInt32("Item6")
            ],
            [
                reader.GetInt32("SpellCast1"), reader.GetInt32("SpellCast2"), reader.GetInt32("SpellCast3"),
                reader.GetInt32("SpellCast4")
            ],
            (reader.GetInt32("SummonerSpell1"), reader.GetInt32("SummonerSpell2")),
            perks,
            reader.GetString("GameName"),
            reader.GetString("Tagline")
        );

        return Task.FromResult(participant);
    }

    public async Task<Game?> GetGameAsync(long gameId)
    {
        await using var connection = databaseConnection.GetConnection();
        await connection.OpenAsync();

        var query = "SELECT * FROM gamesview G WHERE G.GameId = @GameId";

        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@GameId", gameId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!reader.HasRows) return null;

        await reader.ReadAsync();

        var gameDuration = reader.GetInt32("GameDuration");
        var gameStartTimestamp = reader.GetInt64("GameStartTimestamp");
        var gameVersion = reader.GetString("GameVersion");
        var gameType = Enum.Parse<GameType>(reader.GetString("GameType"));
        var matchId = reader.GetString("MatchId");
        var platformId = Enum.Parse<PlatformRoute>(reader.GetString("PlatformId"));
        var winner = Enum.Parse<Team>(reader.GetInt32("Winner").ToString());

        var game = new Game(gameDuration, gameStartTimestamp, gameId, gameVersion, gameType, matchId, platformId,
            winner, []);

        do
        {
            game.Participants.Add(await ReadParticipantFromReaderAsync(reader));
        } while (await reader.ReadAsync());

        return game;
    }

    public async Task<List<Game>> GetGamesAsync()
    {
        await using var connection = databaseConnection.GetConnection();
        await connection.OpenAsync();

        var query = "SELECT * FROM gamesview";
        await using var command = new MySqlCommand(query, connection);

        await using var reader = await command.ExecuteReaderAsync();
        if (!reader.HasRows) return [];

        var games = new Dictionary<long, Game>();

        while (await reader.ReadAsync())
        {
            var gameId = reader.GetInt64("GameId");
            if (!games.ContainsKey(gameId))
            {
                var gameDuration = reader.GetInt32("GameDuration");
                var gameStartTimestamp = reader.GetInt64("GameStartTimestamp");
                var gameVersion = reader.GetString("GameVersion");
                var gameType = Enum.Parse<GameType>(reader.GetString("GameType"));
                var matchId = reader.GetString("MatchId");
                var platformId = Enum.Parse<PlatformRoute>(reader.GetString("PlatformId"));
                var winner = Enum.Parse<Team>(reader.GetInt32("Winner").ToString());

                games[gameId] = new Game(gameDuration, gameStartTimestamp, gameId, gameVersion, gameType, matchId,
                    platformId,
                    winner, []);
            }

            games[gameId].Participants.Add(await ReadParticipantFromReaderAsync(reader));
        }

        return games.Values.ToList();
    }

    public async Task<ConcurrentDictionary<PlatformRoute, ConcurrentBag<string>>> GetMatchIdsAsync()
    {
        await using var connection = databaseConnection.GetConnection();
        await connection.OpenAsync();

        var query = "SELECT MatchId, PlatformId FROM Games";
        await using var command = new MySqlCommand(query, connection);

        await using var reader = await command.ExecuteReaderAsync();
        ConcurrentDictionary<PlatformRoute, ConcurrentBag<string>> matchIds = new();

        while (await reader.ReadAsync())
        {
            var id = reader.GetString("MatchId");
            var platform = Enum.Parse<PlatformRoute>(reader.GetString("PlatformId"));
            if (!matchIds.ContainsKey(platform)) matchIds[platform] = [];
            matchIds[platform].Add(id);
        }

        return matchIds;
    }

    public async Task<Player?> GetPlayerAsync(string summonerPuuid)
    {
        await using var connection = databaseConnection.GetConnection();
        await connection.OpenAsync();

        var query =
            "SELECT S.*, Champion FROM Players P JOIN Summoners S on P.SummonerPuuid = S.Puuid WHERE SummonerPuuid = @SummonerPuuid";
        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@SummonerPuuid", summonerPuuid);

        await using var reader = await command.ExecuteReaderAsync();
        if (!reader.HasRows) return null;

        await reader.ReadAsync();
        var champion = Enum.Parse<Champion>(reader.GetInt32("Champion").ToString());
        var summoner = CreateSummonerFromReader(reader);

        return new Player(summoner, champion);
    }

    public async Task<Summoner?> GetSummonerAsync(string summonerPuuid)
    {
        await using var connection = databaseConnection.GetConnection();

        connection.Open();

        var query = "SELECT * FROM Summoners WHERE Puuid = @Puuid";
        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Puuid", summonerPuuid);
        await using var reader = command.ExecuteReader();

        if (!reader.HasRows) return null;

        await reader.ReadAsync();

        return CreateSummonerFromReader(reader);
    }

    public async Task<IDictionary<PlatformRoute, IList<(string, long)>>> GetPlayerPuuidsLastGameStartTimestampAsync()
    {
        await using var connection = databaseConnection.GetConnection();
        await connection.OpenAsync();

        var query =
            "SELECT * FROM lastgamestarttimestampbyplayerPuuids LGSTP WHERE PlatformId = 'KR' LIMIT 150";
        await using var command = new MySqlCommand(query, connection);

        await using var reader = await command.ExecuteReaderAsync();
        if (!reader.HasRows) return new Dictionary<PlatformRoute, IList<(string, long)>>();

        var players = new Dictionary<PlatformRoute, IList<(string, long)>>();

        while (await reader.ReadAsync())
        {
            var puuid = reader.GetString("SummonerPuuid");
            var lastGameStartTimestamp = reader.IsDBNull(reader.GetOrdinal("LastGameStartTimestamp"))
                ? 0
                : reader.GetInt64("LastGameStartTimestamp");
            var platform = Enum.Parse<PlatformRoute>(reader.GetString("PlatformId"));

            if (!players.TryGetValue(platform, out var value))
            {
                value = [];
                players[platform] = value;
            }

            value.Add((puuid, lastGameStartTimestamp));
        }

        return players;
    }

    public async Task<ConcurrentDictionary<PlatformRoute, ConcurrentBag<Player>>> GetPlayersAsync()
    {
        await using var connection = databaseConnection.GetConnection();
        await connection.OpenAsync();

        var query =
            "SELECT * FROM Players P JOIN Summoners S on P.SummonerPuuid = S.Puuid";
        await using var command = new MySqlCommand(query, connection);

        await using var reader = await command.ExecuteReaderAsync();
        if (!reader.HasRows) return [];

        var players = new ConcurrentDictionary<PlatformRoute, ConcurrentBag<Player>>();

        while (await reader.ReadAsync())
        {
            var champion = Enum.Parse<Champion>(reader.GetInt32("Champion").ToString());
            var summoner = CreateSummonerFromReader(reader);

            var platform = Enum.Parse<PlatformRoute>(reader.GetString("PlatformId"));

            players[platform].Add(new Player(summoner, champion));
        }

        return players;
    }

    public async Task<List<(Summoner, PlatformRoute)>> GetSummonersAsync()
    {
        await using var connection = databaseConnection.GetConnection();
        await connection.OpenAsync();

        var query = "SELECT * FROM Summoners";
        await using var command = new MySqlCommand(query, connection);

        await using var reader = await command.ExecuteReaderAsync();

        List<(Summoner, PlatformRoute)> summoners = [];
        if (!reader.HasRows) return summoners;

        while (await reader.ReadAsync())
        {
            var summoner = CreateSummonerFromReader(reader);
            var platform = Enum.Parse<PlatformRoute>(reader.GetString("PlatformId"));

            summoners.Add((summoner, platform));
        }

        return summoners;
    }

    public async Task<List<(string, PlatformRoute)>> GetIncompleteMatchesAsync()
    {
        var matchIds = new List<(string, PlatformRoute)>();

        await using var connection = databaseConnection.GetConnection();
        await connection.OpenAsync();

        var query = @"
        SELECT MatchId, PlatformId
        FROM Games G
        LEFT JOIN Participants P ON P.GameId = G.GameId
        GROUP BY MatchId, PlatformId
        HAVING COUNT(DISTINCT P.SummonerPuuid) != 10";

        await using var command = new MySqlCommand(query, connection);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            matchIds.Add((reader.GetString("MatchId"), Enum.Parse<PlatformRoute>(reader.GetString("PlatformId"))));
        }

        return matchIds;
    }

    private Summoner CreateSummonerFromReader(DbDataReader reader)
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

    public async Task<int> UpdateAccountAsync(Account oldAccount, Account newAccount)
    {
        await using var connection = databaseConnection.GetConnection();
        await connection.OpenAsync();

        var query =
            "UPDATE Summoners SET GameName = @NewGameName, TagLine = @NewTagLine, Puuid = @NewPuuid WHERE Puuid = @OldPuuid";

        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@NewGameName", newAccount.GameName);
        command.Parameters.AddWithValue("@NewTagLine", newAccount.TagLine);
        command.Parameters.AddWithValue("@NewPuuid", newAccount.Puuid);
        command.Parameters.AddWithValue("@OldPuuid", oldAccount.Puuid);

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<int> DeleteSummonerAsync(string summonerPuuid)
    {
        await using var connection = databaseConnection.GetConnection();
        await connection.OpenAsync();

        var query = "DELETE FROM Summoners WHERE Puuid = @Puuid";
        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@Puuid", summonerPuuid);

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<int> DeleteGameAsync(string matchId)
    {
        await using var connection = databaseConnection.GetConnection();
        await connection.OpenAsync();

        var query = "DELETE FROM Games WHERE MatchId = @MatchId";
        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@MatchId", matchId);

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<ConcurrentBag<(PlatformRoute, Summoner)>> GetSummonersByLastUpdateTimeASync(DateTime lastUpdateTime)
    {
        await using var connection = databaseConnection.GetConnection();
        await connection.OpenAsync();

        var query = "SELECT * FROM Summoners WHERE lastUpdate IS NULL OR lastUpdate < @LastUpdateTime ORDER BY lastUpdate DESC";
        await using var command = new MySqlCommand(query, connection);
        command.Parameters.AddWithValue("@LastUpdateTime", lastUpdateTime);

        await using var reader = await command.ExecuteReaderAsync();
        if (!reader.HasRows) return [];

        var summoners = new ConcurrentBag<(PlatformRoute, Summoner)>();

        while (await reader.ReadAsync())
        {
            var summoner = CreateSummonerFromReader(reader);
            var platform = Enum.Parse<PlatformRoute>(reader.GetString("PlatformId"));
            summoners.Add((platform, summoner));
        }

        return summoners;
    }
}