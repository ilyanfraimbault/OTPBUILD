using System.Collections.Concurrent;
using System.Diagnostics;
using Camille.Enums;
using Camille.RiotGames;
using Camille.RiotGames.ChampionMasteryV4;
using Camille.RiotGames.LeagueV4;
using Camille.RiotGames.SummonerV4;
using OTPBUILD.Models;
using ShellProgressBar;

namespace OTPBUILD.Services;

public class FetchOtps(RiotGamesApi riotApi)
{
    public List<Champion> Champions { get; } = [];
    public List<PlatformRoute> PlatformRoutes { get; } = [];
    public Dictionary<Summoner, ChampionMastery[]> ChampionMasteries { get; } = new();

    public FetchOtps(List<Champion> champions, List<PlatformRoute> platformRoutes, RiotGamesApi riotApi)
        : this(riotApi)
    {
        Champions = champions;
        PlatformRoutes = platformRoutes;
    }

    public Dictionary<PlatformRoute, List<Player>> FindPlayers()
    {
        var players = new ConcurrentDictionary<PlatformRoute, List<Player>>();

        Parallel.ForEach(PlatformRoutes, platform =>
        {
            var list = new ConcurrentBag<Player>();

            Parallel.ForEach(Champions, champion =>
            {
                foreach (var summoner in ChampionMasteries.Keys.Where(summoner => IsMain(summoner, champion)))
                {
                    var player = new Player(summoner, champion);
                    list.Add(player);
                }
            });

            players[platform] = list.ToList();
        });

        return players.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
    }

    public async Task SetSummonersAsync(int? limit = null, List<string>? summonerIds = null)
    {
        var entries = new Dictionary<PlatformRoute, List<LeagueItem>>();

        var pbarPlatform = new ProgressBar(PlatformRoutes.Count, "Fetching entries", new ProgressBarOptions()
        {
            ProgressCharacter = '─',
            ForegroundColor = ConsoleColor.Yellow,
            DisplayTimeInRealTime = true
        });

        foreach (var platform in PlatformRoutes)
        {
            List<LeagueItem> leagueEntries = [];

            try
            {
                leagueEntries = await GetEntriesAsync(platform);
            }
            catch (Exception)
            {
                Console.WriteLine("Error while fetching entries");
            }

            if (summonerIds is not null)
                leagueEntries = leagueEntries
                    .Where(entry => !summonerIds.Contains(entry.SummonerId))
                    .OrderByDescending(entry => entry.LeaguePoints)
                    .ToList();
            else
                leagueEntries = leagueEntries
                    .OrderByDescending(entry => entry.LeaguePoints)
                    .ToList();

            if (limit is not null) leagueEntries = leagueEntries.Take(limit.Value).ToList();

            entries.Add(platform, leagueEntries);

            pbarPlatform.Tick($"Platform: {platform} {pbarPlatform.CurrentTick + 1}/{pbarPlatform.MaxTicks}");
        }

        using var pbar = new ProgressBar(entries.Count, "", new ProgressBarOptions
        {
            ProgressCharacter = '─',
            ForegroundColor = ConsoleColor.Blue,
            DisplayTimeInRealTime = true
        });

        var countPlatform = 0;
        var tasks = new List<Task>();
        var semaphore = new SemaphoreSlim(100);

        foreach (var (platform, platformEntries) in entries)
        {
            countPlatform++;
            var countLeagueItem = 0;

            using var childPbar = pbar.Spawn(platformEntries.Count, "",
                new ProgressBarOptions
                {
                    ForegroundColor = ConsoleColor.DarkCyan, ProgressCharacter = '─', ShowEstimatedDuration = true,
                    DisplayTimeInRealTime = true
                });

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            foreach (var leagueItem in platformEntries)
            {
                countLeagueItem++;

                await semaphore.WaitAsync();
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var summoner = await riotApi.SummonerV4().GetBySummonerIdAsync(platform, leagueItem.SummonerId);
                        var championMasteries = await riotApi.ChampionMasteryV4()
                            .GetAllChampionMasteriesByPUUIDAsync(platform, summoner.Puuid);
                        lock (ChampionMasteries)
                        {
                            ChampionMasteries.Add(summoner, championMasteries);
                        }
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                    finally
                    {
                        semaphore.Release();

                        try
                        {
                            var estimatedTime = stopwatch.Elapsed.TotalSeconds / countLeagueItem *
                                                platformEntries.Count;
                            childPbar.Tick(message: $"{childPbar.CurrentTick + 1}/{childPbar.MaxTicks} summoners fetched",
                                estimatedDuration: TimeSpan.FromSeconds(estimatedTime));

                        }
                        catch (Exception e)
                        {
                            childPbar.Tick();
                        }
                    }
                }));
            }
            pbar.Tick($"Platform: {platform} {pbar.CurrentTick + 1}/{pbar.MaxTicks}");
        }

        await Task.WhenAll(tasks);
    }

    private async Task<List<LeagueItem>> GetEntriesAsync(PlatformRoute platform)
    {
        var challengerLeague = await riotApi.LeagueV4().GetChallengerLeagueAsync(platform, QueueType.RANKED_SOLO_5x5);
        var grandmasterLeague = await riotApi.LeagueV4().GetGrandmasterLeagueAsync(platform, QueueType.RANKED_SOLO_5x5);
        var masterLeague = await riotApi.LeagueV4().GetMasterLeagueAsync(platform, QueueType.RANKED_SOLO_5x5);
        var leagueEntriesList = challengerLeague.Entries
            .Concat(grandmasterLeague.Entries).ToList()
            .Concat(masterLeague.Entries).ToList();

        return leagueEntriesList;
    }

    private bool IsMain(Summoner summoner, Champion champion)
    {
        var championMasteries = ChampionMasteries[summoner].FirstOrDefault(mastery => mastery.ChampionId == champion);
        if (championMasteries is null) return false;
        var lastPlayTime = DateTimeOffset.FromUnixTimeMilliseconds(championMasteries.LastPlayTime);
        return championMasteries.ChampionPoints > 300000 && lastPlayTime > DateTimeOffset.Now.AddDays(-7);
    }
}