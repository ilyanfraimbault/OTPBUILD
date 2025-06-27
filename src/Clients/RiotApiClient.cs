using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Camille.Enums;
using Camille.RiotApi.MatchV4;
using Camille.RiotGames.AccountV1;
using Camille.RiotGames.MatchV5;
using Camille.RiotGames.SummonerV4;
using ShellProgressBar;
using Match = Camille.RiotGames.MatchV5.Match;

namespace OTPBUILD.Services;

public class RiotApiClient
{
    private const int MaxRetries = 3;
    private readonly HttpClient _httpClient;
    public IProgressBar? ProgressBar { get; set; }

    public RiotApiClient(string apiToken)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("Origin", "https://developer.riotgames.com");
        _httpClient.DefaultRequestHeaders.Add("X-Riot-Token", apiToken);
    }

    public async Task<Account?> GetAccountByPuuidAsync(string puuid, RegionalRoute regionalRoute)
    {
        var url = $"https://{regionalRoute}.api.riotgames.com/riot/account/v1/accounts/by-puuid/{puuid}";
        return await ExecuteApiRequest<Account>(url);
    }

    public async Task<Account?> GetAccountByRiotIdAsync(string gameName, string tagLine, RegionalRoute regionRoute)
    {
        var encodedGameName = Uri.EscapeDataString(gameName);
        var encodedTagLine = Uri.EscapeDataString(tagLine);
        var url =
            $"https://{regionRoute}.api.riotgames.com/riot/account/v1/accounts/by-riot-id/{encodedGameName}/{encodedTagLine}";
        return await ExecuteApiRequest<Account>(url);
    }

    public async Task<Match?> GetMatchByMatchIdAsync(string matchId, RegionalRoute regionalRoute)
    {
        var url = $"https://{regionalRoute}.api.riotgames.com/lol/match/v5/matches/{matchId}";

        return await ExecuteApiRequest<Match>(url, 0);
    }

    public async Task<Timeline?> GetMatchTimelineByMatchIdAsync(string matchId, RegionalRoute regionalRoute)
    {
        var url = $"https://{regionalRoute}.api.riotgames.com/lol/match/v5/matches/{matchId}/timeline";
        return await ExecuteApiRequest<Timeline>(url, 0);
    }

    private async Task<T?> ExecuteApiRequest<T>(string url, int retryCount = 0)
    {
        var response = await _httpClient.GetAsync(url);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            var retryAfter = GetRetryAfterSeconds(response.Headers);
            if (retryAfter > 0)
            {
                ProgressBar?.WriteErrorLine($"Rate limit atteint, tentative {retryCount + 1}/{MaxRetries} — Attente de {retryAfter}s...");
                await Task.Delay(retryAfter * 1000);
                return await ExecuteApiRequest<T>(url, retryCount); // Ne pas incrémenter retryCount
            }

            ProgressBar?.WriteErrorLine("Rate limit atteint, pas de Retry-After fourni.");
            return default;
        }

        if (!response.IsSuccessStatusCode)
        {
            ProgressBar?.WriteErrorLine($"Erreur API Riot : {response.StatusCode}");
            return default;
        }

        var content = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(content);
    }

    private static int GetRetryAfterSeconds(HttpResponseHeaders headers)
    {
        if (headers.TryGetValues("Retry-After", out var values) &&
            int.TryParse(values.FirstOrDefault(), out var seconds))
        {
            return seconds;
        }

        return 0;
    }

    public async Task<Summoner?> GetSummonerByPuuidAsync(string summonerPuuid, PlatformRoute platform)
    {
        var url = $"https://{platform}.api.riotgames.com/lol/summoner/v4/summoners/by-puuid/{summonerPuuid}";
        return await ExecuteApiRequest<Summoner>(url);
    }
}