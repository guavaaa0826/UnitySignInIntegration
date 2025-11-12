using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Unity.Services.CloudCode.Apis;
using Unity.Services.CloudCode.Core;
using Unity.Services.CloudCode.Shared;
using Unity.Services.CloudSave.Model;
using System.Reflection;

namespace UnitySignInIntegrationCloud;

public class PlayerDataService
{
    // Constants and read-only variables
    public const string k_PlayerNameKey = "PLAYER_NAME", k_CoinKey = "COIN";
    private readonly ILogger<PlayerDataService> _logger;
    private readonly IGameApiClient _gameApiClient;

    public PlayerDataService(ILogger<PlayerDataService> logger, IGameApiClient gameApiClient)
    {
        _logger = logger;
        _gameApiClient = gameApiClient;
    }

    // Fundamental Save/Get functions
    private async Task SavePlayerData(IExecutionContext context, string key, string value)
    {
        try
        {
            await _gameApiClient.CloudSaveData.SetItemAsync(
                context, 
                context.AccessToken!, 
                context.ProjectId!,
                context.PlayerId!, 
                new SetItemBody(key, value));
                
            _logger.LogInformation("Successfully saved data for key: {Key}", key);
        }
        catch (ApiException ex)
        {
            _logger.LogError("Failed to save data for key {Key}. Error: {Error}", key, ex.Message);
            throw new Exception($"Unable to save player data: {ex.Message}");
        }
    }

    private async Task<string> GetPlayerData(IExecutionContext context, string key)
    {
        try
        {
            var result = await _gameApiClient.CloudSaveData.GetItemsAsync(
                context,
                context.AccessToken!,
                context.ProjectId!,
                context.PlayerId!,
                new List<string> { key });

            var data = result.Data.Results.FirstOrDefault()?.Value?.ToString() ?? string.Empty;
            _logger.LogInformation("Successfully retrieved data for key: {Key}", key);
            return data;
        }
        catch (ApiException ex)
        {
            _logger.LogError("Failed to retrieve data for key {Key}. Error: {Error}", key, ex.Message);
            throw new Exception($"Unable to retrieve player data: {ex.Message}");
        }
    }

    // Save/Get functions for client-side
    // PlayerName
    [CloudCodeFunction("SavePlayerName")]
    public async Task<string> SavePlayerName(IExecutionContext context, string NewName)
    {
        await SavePlayerData(context, k_PlayerNameKey, NewName);
        return NewName;
    }

    [CloudCodeFunction("GetPlayerName")]
    public async Task<string> GetPlayerName(IExecutionContext context)
    {
        string PlayerName = await GetPlayerData(context, k_PlayerNameKey);
        return PlayerName;
    }

    // Coin
    [CloudCodeFunction("SaveCoin")]
    public async Task<string> SaveCoin(IExecutionContext context, int Coin)
    {
        string CoinStr = Coin.ToString();
        await SavePlayerData(context, k_CoinKey, CoinStr);
        return CoinStr;
    }

    [CloudCodeFunction("GetCoin")]
    public async Task<int> GetCoin(IExecutionContext context)
    {
        string CoinStr = await GetPlayerData(context, k_CoinKey);
        if (string.IsNullOrEmpty(CoinStr))
        {
            return 0;
        }
        int Coin = int.Parse(CoinStr);
        return Coin;
    }
}

public class ModuleConfig : ICloudCodeSetup
{
    public void Setup(ICloudCodeConfig config)
    {
        config.Dependencies.AddSingleton<IGameApiClient>(GameApiClient.Create());
    }
}