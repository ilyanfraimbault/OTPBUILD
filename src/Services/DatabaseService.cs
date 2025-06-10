using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using Camille.Enums;
using Camille.RiotGames.AccountV1;
using Camille.RiotGames.MatchV5;
using Camille.RiotGames.SummonerV4;
using Npgsql;
using OTPBUILD.Models;
using Team = Camille.RiotGames.Enums.Team;

namespace OTPBUILD.Services;

public class DatabaseService(DatabaseConnection databaseConnection)
{
    public async Task<int> InsertGameAsync(Game game)
    {
        var command = databaseConnection.CreateStoredProcedure("insert_game");
        command.Parameters.AddWithValue("p_game_id", game.GameId);
        command.Parameters.AddWithValue("p_game_duration", game.GameDuration);
        command.Parameters.AddWithValue("p_game_start_timestamp", game.GameStartTimestamp);
        command.Parameters.AddWithValue("p_game_version", game.GameVersion);
        command.Parameters.AddWithValue("p_game_type", game.GameType.ToString());
        command.Parameters.AddWithValue("p_platform_id", game.PlatformRoute.ToString());
        command.Parameters.AddWithValue("p_winner", (int)game.Winner);
        command.Parameters.AddWithValue("p_match_id", game.MatchId);
        var result = await command.ExecuteNonQueryAsync();

        if (result <= 0) return result;

        var participantsTasks =
            game.Participants.Select(participant => InsertParticipantAsync(participant, game)).ToList();

        var results = await Task.WhenAll(participantsTasks);

        return results.Sum(participant => participant) + result;
    }

    public async Task<int> InsertAccountAsync(Account account)
    {
        const string query = "UPDATE Summoners SET GameName = @GameName, TagLine = @TagLine WHERE Puuid = @Puuid";
        var command = databaseConnection.CreateCommand(query);
        command.Parameters.AddWithValue("@Puuid", account.Puuid);
        command.Parameters.AddWithValue("@GameName", account.GameName);
        command.Parameters.AddWithValue("@TagLine", account.TagLine);

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<int> InsertSummonerAsync(Summoner summoner, PlatformRoute platformRoute)
    {
        var command = databaseConnection.CreateStoredProcedure("insert_summoner");
        command.Parameters.AddWithValue("p_summoner_id", summoner.Id);
        command.Parameters.AddWithValue("p_puuid", summoner.Puuid);
        command.Parameters.AddWithValue("p_name", DBNull.Value);
        command.Parameters.AddWithValue("p_account_id", summoner.AccountId);
        command.Parameters.AddWithValue("p_profile_icon_id", summoner.ProfileIconId);
        command.Parameters.AddWithValue("p_revision_date", summoner.RevisionDate);
        command.Parameters.AddWithValue("p_summoner_level", summoner.SummonerLevel);
        command.Parameters.AddWithValue("p_platform_id", platformRoute.ToString());
        command.Parameters.AddWithValue("p_game_name", DBNull.Value);
        command.Parameters.AddWithValue("p_tag_line", DBNull.Value);

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<int> InsertParticipantAsync(GameParticipant participant, Game game)
    {
        var perksId = await InsertPerksAsync(participant.Perks);

        var command = databaseConnection.CreateStoredProcedure("insert_participant");
        command.Parameters.AddWithValue("p_game_id", game.GameId);
        command.Parameters.AddWithValue("p_summoner_puuid", participant.Puuid);
        command.Parameters.AddWithValue("p_summoner_id", participant.SummonerId);
        command.Parameters.AddWithValue("p_summoner_level", participant.SummonerLevel);
        command.Parameters.AddWithValue("p_summoner_name", participant.SummonerName);
        command.Parameters.AddWithValue("p_game_name", participant.RiotIdGameName);
        command.Parameters.AddWithValue("p_tag_line", participant.RiotIdTagline);
        command.Parameters.AddWithValue("p_champion", (int)participant.Champion);
        command.Parameters.AddWithValue("p_team_id", (int)participant.TeamId);
        command.Parameters.AddWithValue("p_kills", participant.Kills);
        command.Parameters.AddWithValue("p_deaths", participant.Deaths);
        command.Parameters.AddWithValue("p_assists", participant.Assists);
        for (var i = 0; i < 7; i++)
            command.Parameters.AddWithValue($"p_item{i}", participant.Items[i]);
        for (var i = 0; i < 4; i++)
            command.Parameters.AddWithValue($"p_spell_cast{i + 1}", participant.SpellsCasts[i]);
        command.Parameters.AddWithValue("p_summoner_spell1", participant.SummonerSpells.Item1);
        command.Parameters.AddWithValue("p_summoner_spell2", participant.SummonerSpells.Item2);
        command.Parameters.AddWithValue("p_perks", perksId);
        command.Parameters.AddWithValue("p_team_position", participant.TeamPosition);
        command.Parameters.AddWithValue("p_platform_id", game.PlatformRoute.ToString());

        return await command.ExecuteNonQueryAsync();
    }

    private async Task<int?> InsertPerksAsync(Perks perks)
    {
        var statPerksId = await InsertStatPerksAsync(perks.StatPerks);
        var primaryStyleId = await InsertPerksStyleAsync(perks.Styles[0]);
        var secondaryStyleId = await InsertPerksStyleAsync(perks.Styles[1]);

        var command = databaseConnection.CreateStoredProcedure("insert_perks");
        command.Parameters.AddWithValue("p_stat_perks", statPerksId);
        command.Parameters.AddWithValue("p_primary_style", primaryStyleId);
        command.Parameters.AddWithValue("p_secondary_style", secondaryStyleId);

        var idParam = new NpgsqlParameter("p_id", NpgsqlTypes.NpgsqlDbType.Integer)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(idParam);

        await command.ExecuteNonQueryAsync();

        return (int?)idParam.Value;
    }

    private async Task<int?> InsertPerksStyleAsync(PerkStyle style)
    {
        var command = databaseConnection.CreateStoredProcedure("insert_perks_style");
        command.Parameters.AddWithValue("p_description", style.Description);
        command.Parameters.AddWithValue("p_style", style.Style);
        for (var i = 0; i < 4; i++)
            command.Parameters.AddWithValue($"p_style_selection{i + 1}",
                style.Selections.Length > i ? style.Selections[i].Perk : DBNull.Value);

        var idParam = new NpgsqlParameter("p_id", NpgsqlTypes.NpgsqlDbType.Integer)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(idParam);

        await command.ExecuteNonQueryAsync();

        return (int?)idParam.Value;
    }

    private async Task<int?> InsertStatPerksAsync(PerkStats stats)
    {
        var command = databaseConnection.CreateStoredProcedure("insert_stat_perks");
        command.Parameters.AddWithValue("p_defense", stats.Defense);
        command.Parameters.AddWithValue("p_flex", stats.Flex);
        command.Parameters.AddWithValue("p_offense", stats.Offense);

        var idParam = new NpgsqlParameter("p_id", NpgsqlTypes.NpgsqlDbType.Integer)
        {
            Direction = ParameterDirection.Output
        };
        command.Parameters.Add(idParam);

        await command.ExecuteNonQueryAsync();

        return (int?)idParam.Value;
    }

    public async Task<int> InsertPlayerAsync(Player player, PlatformRoute platformRoute)
    {
        await InsertSummonerAsync(player.Summoner, platformRoute);

        var command = databaseConnection.CreateStoredProcedure("insert_player");
        command.Parameters.AddWithValue("p_summoner_puuid", player.Summoner.Puuid);
        command.Parameters.AddWithValue("p_champion", (int)player.Champion);

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<ConcurrentDictionary<PlatformRoute, ConcurrentBag<Account>>> GetAccountsAsync()
    {
        var accounts = new ConcurrentDictionary<PlatformRoute, ConcurrentBag<Account>>();

        const string query = "SELECT PlatformId, Puuid, GameName, TagLine FROM Summoners";
        var command = databaseConnection.CreateCommand(query);

        await using var reader = await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            var platformId = Enum.Parse<PlatformRoute>(reader.GetString(reader.GetOrdinal("PlatformId")));

            var account = new Account
            {
                Puuid = reader.GetString(reader.GetOrdinal("Puuid")),
                GameName = reader.IsDBNull(reader.GetOrdinal("GameName"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("GameName")),
                TagLine = reader.IsDBNull(reader.GetOrdinal("TagLine"))
                    ? null
                    : reader.GetString(reader.GetOrdinal("TagLine"))
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
            Defense = reader.GetInt32(reader.GetOrdinal("defense")),
            Flex = reader.GetInt32(reader.GetOrdinal("flex")),
            Offense = reader.GetInt32(reader.GetOrdinal("offense"))
        };

        var primaryStyle = new PerkStyle
        {
            Style = reader.GetInt32(reader.GetOrdinal("primaryStyle")),
            Description = reader.GetString(reader.GetOrdinal("primaryStyleDescription")),
            Selections =
            [
                new PerkStyleSelection
                {
                    Perk = reader.GetInt32(reader.GetOrdinal("primStyleSelection1")),
                    Var1 = 0,
                    Var2 = 0,
                    Var3 = 0
                },
                new PerkStyleSelection
                {
                    Perk = reader.GetInt32(reader.GetOrdinal("primStyleSelection2")),
                    Var1 = 0,
                    Var2 = 0,
                    Var3 = 0
                },
                new PerkStyleSelection
                {
                    Perk = reader.GetInt32(reader.GetOrdinal("primStyleSelection3")),
                    Var1 = 0,
                    Var2 = 0,
                    Var3 = 0
                },
                new PerkStyleSelection
                {
                    Perk = reader.GetInt32(reader.GetOrdinal("primStyleSelection4")),
                    Var1 = 0,
                    Var2 = 0,
                    Var3 = 0
                }
            ]
        };

        var secondaryStyle = new PerkStyle
        {
            Style = reader.GetInt32(reader.GetOrdinal("secondaryStyle")),
            Description = reader.GetString(reader.GetOrdinal("secondaryStyleDescription")),
            Selections =
            [
                new PerkStyleSelection
                {
                    Perk = reader.GetInt32(reader.GetOrdinal("secStyleSelection1")),
                    Var1 = 0,
                    Var2 = 0,
                    Var3 = 0
                },
                new PerkStyleSelection
                {
                    Perk = reader.GetInt32(reader.GetOrdinal("secStyleSelection2")),
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
            reader.GetString(reader.GetOrdinal("SummonerName")),
            reader.GetString(reader.GetOrdinal("SummonerId")),
            reader.GetInt32(reader.GetOrdinal("SummonerLevel")),
            reader.GetString(reader.GetOrdinal("SummonerPuuid")),
            Enum.Parse<Champion>(reader.GetInt32(reader.GetOrdinal("Champion")).ToString()),
            Enum.Parse<Team>(reader.GetInt32(reader.GetOrdinal("TeamId")).ToString()),
            reader.GetString(reader.GetOrdinal("TeamPosition")),
            reader.GetInt32(reader.GetOrdinal("Kills")),
            reader.GetInt32(reader.GetOrdinal("Deaths")),
            reader.GetInt32(reader.GetOrdinal("Assists")),
            [
                reader.GetInt32(reader.GetOrdinal("Item0")),
                reader.GetInt32(reader.GetOrdinal("Item1")),
                reader.GetInt32(reader.GetOrdinal("Item2")),
                reader.GetInt32(reader.GetOrdinal("Item3")),
                reader.GetInt32(reader.GetOrdinal("Item4")),
                reader.GetInt32(reader.GetOrdinal("Item5")),
                reader.GetInt32(reader.GetOrdinal("Item6"))
            ],
            [
                reader.GetInt32(reader.GetOrdinal("SpellCast1")),
                reader.GetInt32(reader.GetOrdinal("SpellCast2")),
                reader.GetInt32(reader.GetOrdinal("SpellCast3")),
                reader.GetInt32(reader.GetOrdinal("SpellCast4"))
            ],
            (reader.GetInt32(reader.GetOrdinal("SummonerSpell1")),
                reader.GetInt32(reader.GetOrdinal("SummonerSpell2"))),
            perks,
            reader.GetString(reader.GetOrdinal("GameName")),
            reader.GetString(reader.GetOrdinal("TagLine"))
        );

        return Task.FromResult(participant);
    }

    public async Task<Game?> GetGameAsync(long gameId)
    {
        const string query = "SELECT * FROM gamesview G WHERE G.GameId = @GameId";
        var command = databaseConnection.CreateCommand(query);
        command.Parameters.AddWithValue("@GameId", gameId);

        await using var reader = await command.ExecuteReaderAsync();
        if (!reader.HasRows) return null;

        await reader.ReadAsync();

        var gameDuration = reader.GetInt32(reader.GetOrdinal("GameDuration"));
        var gameStartTimestamp = reader.GetInt64(reader.GetOrdinal("GameStartTimestamp"));
        var gameVersion = reader.GetString(reader.GetOrdinal("GameVersion"));
        var gameType = Enum.Parse<GameType>(reader.GetString(reader.GetOrdinal("GameType")));
        var matchId = reader.GetString(reader.GetOrdinal("MatchId"));
        var platformId = Enum.Parse<PlatformRoute>(reader.GetString(reader.GetOrdinal("PlatformId")));
        var winner = Enum.Parse<Team>(reader.GetInt32(reader.GetOrdinal("Winner")).ToString());

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
        const string query = "SELECT * FROM gamesview";
        var command = databaseConnection.CreateCommand(query);

        await using var reader = await command.ExecuteReaderAsync();
        if (!reader.HasRows) return [];

        var games = new Dictionary<long, Game>();

        while (await reader.ReadAsync())
        {
            var gameId = reader.GetInt64(reader.GetOrdinal("GameId"));
            if (!games.ContainsKey(gameId))
            {
                var gameDuration = reader.GetInt32(reader.GetOrdinal("GameDuration"));
                var gameStartTimestamp = reader.GetInt64(reader.GetOrdinal("GameStartTimestamp"));
                var gameVersion = reader.GetString(reader.GetOrdinal("GameVersion"));
                var gameType = Enum.Parse<GameType>(reader.GetString(reader.GetOrdinal("GameType")));
                var matchId = reader.GetString(reader.GetOrdinal("MatchId"));
                var platformId = Enum.Parse<PlatformRoute>(reader.GetString(reader.GetOrdinal("PlatformId")));
                var winner = Enum.Parse<Team>(reader.GetInt32(reader.GetOrdinal("Winner")).ToString());

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
        const string query = "SELECT MatchId, PlatformId FROM Games";
        var command = databaseConnection.CreateCommand(query);

        await using var reader = await command.ExecuteReaderAsync();
        ConcurrentDictionary<PlatformRoute, ConcurrentBag<string>> matchIds = new();

        while (await reader.ReadAsync())
        {
            var id = reader.GetString(reader.GetOrdinal("MatchId"));
            var platform = Enum.Parse<PlatformRoute>(reader.GetString(reader.GetOrdinal("PlatformId")));
            if (!matchIds.ContainsKey(platform)) matchIds[platform] = [];
            matchIds[platform].Add(id);
        }

        return matchIds;
    }

    public async Task<Player?> GetPlayerAsync(string summonerPuuid)
    {
        const string query = "SELECT S.*, Champion FROM Players P JOIN Summoners S on P.SummonerPuuid = S.Puuid WHERE SummonerPuuid = @SummonerPuuid";
        var command = databaseConnection.CreateCommand(query);
        command.Parameters.AddWithValue("@SummonerPuuid", summonerPuuid);

        await using var reader = await command.ExecuteReaderAsync();
        if (!reader.HasRows) return null;

        await reader.ReadAsync();
        var champion = Enum.Parse<Champion>(reader.GetInt32(reader.GetOrdinal("Champion")).ToString());
        var summoner = CreateSummonerFromReader(reader);

        return new Player(summoner, champion);
    }

    public async Task<Summoner?> GetSummonerAsync(string summonerPuuid)
    {
        const string query = "SELECT * FROM Summoners WHERE Puuid = @Puuid";
        var command = databaseConnection.CreateCommand(query);
        command.Parameters.AddWithValue("@Puuid", summonerPuuid);
        await using var reader = await command.ExecuteReaderAsync();

        if (!reader.HasRows) return null;

        await reader.ReadAsync();

        return CreateSummonerFromReader(reader);
    }

    public async Task<IDictionary<PlatformRoute, IList<(string, long)>>> GetPlayerPuuidsLastGameStartTimestampAsync()
    {
        const string query = "SELECT * FROM lastgamestarttimestampbyplayerPuuids LGSTP WHERE PlatformId = 'KR' LIMIT 150";
        var command = databaseConnection.CreateCommand(query);

        await using var reader = await command.ExecuteReaderAsync();
        if (!reader.HasRows) return new Dictionary<PlatformRoute, IList<(string, long)>>();

        var players = new Dictionary<PlatformRoute, IList<(string, long)>>();

        while (await reader.ReadAsync())
        {
            var puuid = reader.GetString(reader.GetOrdinal("SummonerPuuid"));
            var lastGameStartTimestamp = reader.IsDBNull(reader.GetOrdinal("LastGameStartTimestamp"))
                ? 0
                : reader.GetInt64(reader.GetOrdinal("LastGameStartTimestamp"));
            var platform = Enum.Parse<PlatformRoute>(reader.GetString(reader.GetOrdinal("PlatformId")));

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
        var connection = await databaseConnection.GetOpenConnectionAsync();

        const string query = "SELECT * FROM Players P JOIN Summoners S on P.SummonerPuuid = S.Puuid";
        var command = databaseConnection.CreateCommand(query);

        await using var reader = await command.ExecuteReaderAsync();
        if (!reader.HasRows) return [];

        var players = new ConcurrentDictionary<PlatformRoute, ConcurrentBag<Player>>();

        while (await reader.ReadAsync())
        {
            var champion = Enum.Parse<Champion>(reader.GetInt32(reader.GetOrdinal("Champion")).ToString());
            var summoner = CreateSummonerFromReader(reader);

            var platform = Enum.Parse<PlatformRoute>(reader.GetString(reader.GetOrdinal("PlatformId")));

            if (!players.ContainsKey(platform))
            {
                players[platform] = [];
            }

            players[platform].Add(new Player(summoner, champion));
        }

        return players;
    }

    public async Task<List<(Summoner, PlatformRoute)>> GetSummonersAsync()
    {
        const string query = "SELECT * FROM Summoners";
        var command = databaseConnection.CreateCommand(query);

        await using var reader = await command.ExecuteReaderAsync();

        List<(Summoner, PlatformRoute)> summoners = [];
        if (!reader.HasRows) return summoners;

        while (await reader.ReadAsync())
        {
            var summoner = CreateSummonerFromReader(reader);
            var platform = Enum.Parse<PlatformRoute>(reader.GetString(reader.GetOrdinal("PlatformId")));

            summoners.Add((summoner, platform));
        }

        return summoners;
    }

    public async Task<List<(string, PlatformRoute)>> GetIncompleteMatchesAsync()
    {
        var matchIds = new List<(string, PlatformRoute)>();

        const string query = """

                                     SELECT MatchId, PlatformId
                                     FROM Games G
                                     LEFT JOIN Participants P ON P.GameId = G.GameId
                                     GROUP BY MatchId, PlatformId
                                     HAVING COUNT(DISTINCT P.SummonerPuuid) != 10
                             """;

        var command = databaseConnection.CreateCommand(query);

        await using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            matchIds.Add((reader.GetString(reader.GetOrdinal("MatchId")),
                Enum.Parse<PlatformRoute>(reader.GetString(reader.GetOrdinal("PlatformId")))));
        }

        return matchIds;
    }

    private Summoner CreateSummonerFromReader(DbDataReader reader)
    {
        return new Summoner
        {
            SummonerLevel = reader.IsDBNull(reader.GetOrdinal("Level"))
                ? 0
                : reader.GetInt32(reader.GetOrdinal("Level")),
            Id = reader.IsDBNull(reader.GetOrdinal("Id")) ? string.Empty : reader.GetString(reader.GetOrdinal("Id")),
            RevisionDate = reader.IsDBNull(reader.GetOrdinal("RevisionDate"))
                ? 0
                : reader.GetInt64(reader.GetOrdinal("RevisionDate")),
            Puuid = reader.IsDBNull(reader.GetOrdinal("Puuid"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("Puuid")),
            ProfileIconId = reader.IsDBNull(reader.GetOrdinal("ProfileIconId"))
                ? 0
                : reader.GetInt32(reader.GetOrdinal("ProfileIconId")),
            AccountId = reader.IsDBNull(reader.GetOrdinal("AccountId"))
                ? string.Empty
                : reader.GetString(reader.GetOrdinal("AccountId"))
        };
    }

    public async Task<int> UpdateAccountAsync(Account oldAccount, Account newAccount)
    {
        var connection = await databaseConnection.GetOpenConnectionAsync();

        var query =
            "UPDATE Summoners SET GameName = @NewGameName, TagLine = @NewTagLine, Puuid = @NewPuuid WHERE Puuid = @OldPuuid";

        var command = databaseConnection.CreateCommand(query);
        command.Parameters.AddWithValue("@NewGameName", newAccount.GameName);
        command.Parameters.AddWithValue("@NewTagLine", newAccount.TagLine);
        command.Parameters.AddWithValue("@NewPuuid", newAccount.Puuid);
        command.Parameters.AddWithValue("@OldPuuid", oldAccount.Puuid);

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<int> DeleteSummonerAsync(string summonerPuuid)
    {
        var query = "DELETE FROM Summoners WHERE Puuid = @Puuid";
        var command = databaseConnection.CreateCommand(query);
        command.Parameters.AddWithValue("@Puuid", summonerPuuid);

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<int> DeleteGameAsync(string matchId)
    {
        var query = "DELETE FROM Games WHERE MatchId = @MatchId";
        var command = databaseConnection.CreateCommand(query);
        command.Parameters.AddWithValue("@MatchId", matchId);

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<ConcurrentBag<(PlatformRoute, Summoner)>> GetSummonersByLastUpdateTimeASync(
        DateTime lastUpdateTime
        )
    {
        var query =
            "SELECT * FROM Summoners WHERE lastUpdate IS NULL OR lastUpdate < @LastUpdateTime ORDER BY lastUpdate DESC";
        var command = databaseConnection.CreateCommand(query);
        command.Parameters.AddWithValue("@LastUpdateTime", lastUpdateTime);

        await using var reader = await command.ExecuteReaderAsync();
        if (!reader.HasRows) return [];

        var summoners = new ConcurrentBag<(PlatformRoute, Summoner)>();

        while (await reader.ReadAsync())
        {
            var summoner = CreateSummonerFromReader(reader);
            var platform = Enum.Parse<PlatformRoute>(reader.GetString(reader.GetOrdinal("PlatformId")));
            summoners.Add((platform, summoner));
        }

        return summoners;
    }
}