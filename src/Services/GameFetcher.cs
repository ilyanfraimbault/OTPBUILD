using System.Collections.Concurrent;
using System.Diagnostics;
using Camille.Enums;
using Camille.RiotGames;
using Camille.RiotGames.MatchV5;
using OTPBUILD.Models;
using ShellProgressBar;

namespace OTPBUILD.Services;

public class GameFetcher(RiotGamesApi riotApi, DatabaseService databaseService, ProgressBarBase progressBar)
{
    private ConcurrentDictionary<PlatformRoute, ConcurrentBag<string>> _matchIdsAlreadyInserted = new();
    private readonly ConcurrentDictionary<PlatformRoute, ConcurrentBag<(string, long)>> _retryList = new();

    private readonly ProgressBarOptions _progressBarOptions = new()
    {
        ProgressCharacter = '─',
        ForegroundColor = ConsoleColor.DarkYellow,
        DisplayTimeInRealTime = false
    };

    public async Task RunAsync(IDictionary<PlatformRoute, IList<(string, long)>> players)
    {
        _matchIdsAlreadyInserted = await databaseService.GetMatchIdsAsync();

        var tasks = new List<Task<List<Game>>>();

        var totalPlayers = players.Sum(p => p.Value.Count);

        using (var fetchPbar = progressBar.Spawn(totalPlayers, "Fetching players", _progressBarOptions))
        {
            await FetchGamesForPlayers(players, tasks, fetchPbar);
        }

        var remainingTasks = new HashSet<Task<List<Game>>>(tasks);

        progressBar.Message = "Waiting for tasks to complete";
        using (var taskPbar = progressBar.Spawn(remainingTasks.Count, "Completing tasks", _progressBarOptions))
        {
            await CompleteTasks(remainingTasks, taskPbar);
        }

        var retryTasks = new List<Task<List<Game>>>();

        progressBar.Message = "Retrying failed tasks";
        foreach (var (platformRoute, playerList) in _retryList)
        {
            using var retryPbar = progressBar.Spawn(playerList.Count, "Retrying failed tasks", _progressBarOptions);
            foreach (var (puuid, lastGameStartTimestamp) in playerList)
            {
                retryPbar.WriteLine($"Retrying {puuid} and {platformRoute} since {lastGameStartTimestamp}");
                retryTasks.Add(FetchGames(puuid, platformRoute, lastGameStartTimestamp, retryPbar));
            }
        }

        remainingTasks.UnionWith(retryTasks);

        var gamesList = await Task.WhenAll(tasks);

        progressBar.Message = "Inserting games";
        await InsertGames(gamesList.SelectMany(g => g).ToList(),
            progressBar.Spawn(gamesList.SelectMany(g => g).Count(), "Inserting games", _progressBarOptions));
    }

    private Task FetchGamesForPlayers(
        IDictionary<PlatformRoute, IList<(string, long)>> players, List<Task<List<Game>>> tasks, ProgressBarBase pbar
        )
    {
        var stopwatch = Stopwatch.StartNew();
        Parallel.ForEach(players, kvp =>
        {
            var platformRoute = kvp.Key;
            var playerList = kvp.Value;
            foreach (var (puuid, lastGameStartTimestamp) in playerList)
            {
                if (pbar.Percentage > 0)
                    pbar.EstimatedDuration =
                        TimeSpan.FromSeconds(stopwatch.Elapsed.TotalSeconds / (pbar.Percentage / 100));

                tasks.Add(FetchGames(puuid, platformRoute, lastGameStartTimestamp / 1000, pbar));
                pbar.Tick(
                    message: $"{pbar.CurrentTick + 1}/{pbar.MaxTicks} players fetched",
                    estimatedDuration: pbar.EstimatedDuration);
            }
        });

        return Task.CompletedTask;
    }

    private async Task CompleteTasks(HashSet<Task<List<Game>>> remainingTasks, ProgressBarBase taskPbar)
    {
        var stopwatch = Stopwatch.StartNew();
        while (remainingTasks.Count > 0)
        {
            var finishedTask = await Task.WhenAny(remainingTasks);
            var resultCount = finishedTask.Result.Count;
            if (resultCount != 0)
                await InsertGames(finishedTask.Result,
                    taskPbar.Spawn(resultCount, $"{resultCount} games to insert", options: _progressBarOptions));

            remainingTasks.Remove(finishedTask);

            taskPbar.WriteLine($"{resultCount} game tasks completed");

            if (taskPbar.Percentage > 0)
                taskPbar.EstimatedDuration =
                    TimeSpan.FromSeconds(stopwatch.Elapsed.TotalSeconds / (taskPbar.Percentage / 100));
            taskPbar.Tick(
                message: $"Progress: {taskPbar.CurrentTick}/{taskPbar.MaxTicks} games tasks completed",
                estimatedDuration: taskPbar.EstimatedDuration);
            progressBar.Tick();
        }
    }

    private async Task<List<Game>> FetchGames(
        string puuid, PlatformRoute route, long lastPlayedTimestamp, ProgressBarBase pbar
        )
    {
        string[] matchIds;

        try
        {
            matchIds = await riotApi.MatchV5().GetMatchIdsByPUUIDAsync(route.ToRegional(), puuid,
                startTime: lastPlayedTimestamp, queue: Queue.SUMMONERS_RIFT_5V5_RANKED_SOLO);

            pbar.WriteLine($"{matchIds.Length} matches found for {puuid} and {route} since {lastPlayedTimestamp}");
        }
        catch (Exception)
        {
            _retryList.AddOrUpdate(route,
                _ => [(puuid, lastPlayedTimestamp)],
                (_, bag) =>
                {
                    bag.Add((puuid, lastPlayedTimestamp));
                    return bag;
                });

            pbar.WriteErrorLine($"Error for {puuid} and {route} since {lastPlayedTimestamp}");
            return [];
        }

        return await ConvertGames(route, pbar, matchIds);
    }

    private async Task<List<Game>> ConvertGames(PlatformRoute route, ProgressBarBase pbar, string[] matchIds)
    {
        var matchTasks = AddMatchTaskIfNotExists(route, pbar, matchIds);

        pbar.WriteLine($"{matchTasks.Count} match tasks to process");

        return await ProcessGameConversion(pbar, matchTasks);
    }

    private List<Task<Match>> AddMatchTaskIfNotExists(PlatformRoute route, ProgressBarBase pbar, string[] matchIds)
    {
        List<Task<Match>> matchTasks = [];
        foreach (var matchId in matchIds)
            try
            {
                if (!_matchIdsAlreadyInserted.ContainsKey(route))
                    _matchIdsAlreadyInserted.TryAdd(route, []);

                if (_matchIdsAlreadyInserted[route].Contains(matchId)) continue;

                var matchTask = riotApi.MatchV5().GetMatchAsync(route.ToRegional(), matchId);
                _matchIdsAlreadyInserted[route].Add(matchId);

                matchTasks.Add(matchTask!);
            }
            catch (Exception e)
            {
                pbar.WriteErrorLine(e.ToString());
            }

        return matchTasks;
    }

    private async Task<List<Game>> ProcessGameConversion(ProgressBarBase pbar, List<Task<Match>> matchTasks)
    {
        List<Game> gamesConverted = [];
        while (matchTasks.Count != 0)
        {
            var matchTask = await Task.WhenAny(matchTasks);
            matchTasks.Remove(matchTask);

            try
            {
                var game = new Game(matchTask.Result);
                gamesConverted.Add(game);
            }
            catch (Exception e)
            {
                pbar.WriteErrorLine(e.ToString());
            }
        }

        return gamesConverted;
    }

    private async Task InsertGames(List<Game> games, ProgressBarBase pbar)
    {
        var stopwatch = Stopwatch.StartNew();
        foreach (var game in games)
        {
            if (pbar.Percentage > 0)
                pbar.EstimatedDuration =
                    TimeSpan.FromSeconds(stopwatch.Elapsed.TotalSeconds / (pbar.Percentage / 100));
            await databaseService.InsertGameAsync(game);
            pbar.Tick(
                message: $"{pbar.CurrentTick + 1}/{pbar.MaxTicks} games inserted",
                estimatedDuration: pbar.EstimatedDuration);
        }
    }
}