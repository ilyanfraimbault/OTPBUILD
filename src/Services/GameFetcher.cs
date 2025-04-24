using System.Collections.Concurrent;
using System.Diagnostics;
using Camille.Enums;
using Camille.RiotGames;
using Camille.RiotGames.MatchV5;
using OTPBUILD.Models;
using ShellProgressBar;

namespace OTPBUILD.Services;

public class GameFetcher(
    RiotGamesApi riotApi,
    DatabaseService databaseService,
    ProgressBarBase progressBar,
    IDictionary<PlatformRoute, IList<(string, long)>> players,
    ConcurrentDictionary<PlatformRoute, ConcurrentBag<string>> matchIdsAlreadyInserted)
{
    private readonly ProgressBarOptions _progressBarOptions = new()
    {
        ProgressCharacter = '─',
        ForegroundColor = ConsoleColor.Magenta,
        DisplayTimeInRealTime = false,
        ProgressBarOnBottom = true,
        CollapseWhenFinished = true
    };

    public readonly ProgressBarBase ProgressBar = progressBar;

    public async Task RunAsync()
    {
        ConcurrentBag<Task<List<Game>>> tasks;

        var totalPlayers = players.Sum(p => p.Value.Count);

        using (var fetchPbar = ProgressBar.Spawn(totalPlayers, "Fetching players", _progressBarOptions))
        {
            tasks = FetchGamesForPlayers(players, fetchPbar);
        }

        var remainingTasks = new HashSet<Task<List<Game>>>(tasks);

        ProgressBar.Message = "Waiting for tasks to complete";
        using (var taskPbar = ProgressBar.Spawn(remainingTasks.Count, "Completing tasks", _progressBarOptions))
        {
            await CompleteTasks(remainingTasks, taskPbar);
        }
    }

    private ConcurrentBag<Task<List<Game>>> FetchGamesForPlayers(
        IDictionary<PlatformRoute, IList<(string, long)>> playerData, ProgressBarBase pbar
        )
    {
        ConcurrentBag<Task<List<Game>>> tasksBag = [];
        Parallel.ForEach(playerData, kvp =>
        {
            var platformRoute = kvp.Key;
            var playerList = kvp.Value;
            foreach (var (puuid, lastGameStartTimestamp) in playerList)
            {
                tasksBag.Add(FetchGames(puuid, platformRoute, lastGameStartTimestamp / 1000));
                pbar.Tick(message: $"{pbar.CurrentTick + 1}/{pbar.MaxTicks} players fetched");
            }
        });

        return tasksBag;
    }

    private async Task CompleteTasks(HashSet<Task<List<Game>>> remainingTasks, ProgressBarBase taskPbar)
    {
        var progressBarOptions = new ProgressBarOptions
        {
            ProgressCharacter = '─',
            ForegroundColor = ConsoleColor.Green,
            DisplayTimeInRealTime = true,
            CollapseWhenFinished = true,
            ProgressBarOnBottom = true
        };
        while (remainingTasks.Count > 0)
        {
            var finishedTask = await Task.WhenAny(remainingTasks);
            var resultCount = finishedTask.Result.Count;
            if (resultCount != 0)
                await InsertGames(finishedTask.Result,
                    taskPbar.Spawn(resultCount, $"{resultCount} games to insert", options: progressBarOptions));

            remainingTasks.Remove(finishedTask);

            taskPbar.WriteLine($"{resultCount} game tasks completed");

            ProgressBar.Tick();
            taskPbar.Tick(message: $"Progress: {taskPbar.CurrentTick + 1}/{taskPbar.MaxTicks} games tasks completed");
        }
    }

    private async Task<List<Game>> FetchGames(
        string puuid, PlatformRoute route, long lastPlayedTimestamp
        )
    {
        string[] matchIds;

        try
        {
            matchIds = await riotApi.MatchV5().GetMatchIdsByPUUIDAsync(route.ToRegional(), puuid,
                startTime: lastPlayedTimestamp, queue: Queue.SUMMONERS_RIFT_5V5_RANKED_SOLO);

            ProgressBar.WriteLine(
                $"{matchIds.Length} matches found for {puuid} and {route} since {lastPlayedTimestamp}");
        }
        catch (Exception)
        {
            ProgressBar.WriteErrorLine($"Error for {puuid} and {route} since {lastPlayedTimestamp}");
            return [];
        }

        return await ConvertGames(route, matchIds);
    }

    private async Task<List<Game>> ConvertGames(PlatformRoute route, string[] matchIds)
    {
        var matchTasks = AddMatchTaskIfNotExists(route, matchIds);

        ProgressBar.WriteLine($"{matchTasks.Count} match tasks to process");

        return await ProcessGameConversion(matchTasks);
    }

    private List<Task<Match>> AddMatchTaskIfNotExists(PlatformRoute route, string[] matchIds)
    {
        List<Task<Match>> matchTasks = [];
        foreach (var matchId in matchIds)
            try
            {
               if (!matchIdsAlreadyInserted.ContainsKey(route))
                   matchIdsAlreadyInserted.TryAdd(route, []);

               if (matchIdsAlreadyInserted[route].Contains(matchId)) continue;

               matchIdsAlreadyInserted[route].Add(matchId);

                var matchTask = riotApi.MatchV5().GetMatchAsync(route.ToRegional(), matchId);
                matchIdsAlreadyInserted[route].Add(matchId);

                matchTasks.Add(matchTask!);
            }
            catch (Exception e)
            {
                ProgressBar.WriteErrorLine(e.ToString());
            }

        return matchTasks;
    }

    private async Task<List<Game>> ProcessGameConversion(List<Task<Match>> matchTasks)
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
                ProgressBar.WriteErrorLine(e.ToString());
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
            var result = await databaseService.InsertGameAsync(game);
            ProgressBar.WriteLine((result > 0 ? "Inserted" : "Failed to insert") + $" game {game.GameId}");
            pbar.Tick(
                message: $"{pbar.CurrentTick + 1}/{pbar.MaxTicks} games inserted",
                estimatedDuration: pbar.EstimatedDuration);
        }
    }
}