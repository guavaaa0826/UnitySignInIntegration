using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Services.CloudCode;
using Unity.Services.CloudCode.GeneratedBindings;
using System;
using System.Linq;
using Unity.VisualScripting;

public class PlayerDataManager : MonoBehaviour
{
    // Variables
    private PlayerDataServiceBindings m_bindings;
    public LoginManager loginManager;
    private string PlayerName;
    private int CoinCounter;

    // UI elements
    public Button SetPlayerNameButton, AddCoinButton, ResetCoinButton, SaveCoinButton;
    public TextMeshProUGUI PlayerNameText, CoinText;
    public TMP_InputField PlayerNameInputField;

    // Initialization functions
    void Start()
    {
        m_bindings = new PlayerDataServiceBindings(CloudCodeService.Instance);
        loginManager.PlayerSignedIn += InitializePlayer;

        SetPlayerNameButton.onClick.AddListener(SavePlayerName);
        AddCoinButton.onClick.AddListener(AddCoin);
        ResetCoinButton.onClick.AddListener(ResetCoin);
        SaveCoinButton.onClick.AddListener(SaveCoin);
    }

    private async void InitializePlayer()
    {
        try
        {
            PlayerName = await m_bindings.GetPlayerName();
            UpdatePlayerNameUI();
            Debug.Log($"PlayerDataManager: Say hello to {PlayerName}.");

            CoinCounter = await m_bindings.GetCoin();
            UpdateCoinUI();
            Debug.Log($"PlayerDataManager: {PlayerName} has {CoinCounter} coins.");
        }
        catch (CloudCodeException ex)
        {
            Debug.Log($"PlayerDataManager: Player initialization failed with {ex.Message}");
        }
    }

    // Utility functions
    // PlayerName
    public async void SavePlayerName()
    {
        string NewName = PlayerNameInputField.text; // Retrieve the PlayerName from the input field.
        string message = IsPlayerNameValid(NewName);
        if (message != null)
        {
            Debug.LogWarning($"PlayerDataManager: Saving new player name failed: {message}");
            return;
        }
        
        try
        {
            PlayerName = await m_bindings.SavePlayerName(NewName);
            UpdatePlayerNameUI();
            Debug.Log($"PlayerDataManager: Saved new player name: {PlayerName}");
        }
        catch (Exception ex)
        {
            Debug.Log($"PlayerDataManager: Error when saving new player name: {ex.Message}");
        }
    }

    private static string IsPlayerNameValid(string NewName)
    {
        if (NewName.Length < 4)
        {
            return "Name too short.";
        }
        else if (NewName.Length > 16)
        {
            return "Name too long.";
        }
        else if (!NewName.All(c => char.IsLetterOrDigit(c)))
        {
            return "Name contains non-alphanumeric characters.";
        }
        return null; // The NewName is valid
    }

    // Coin
    private void AddCoin()
    {
        CoinCounter++;
        UpdateCoinUI();
    }

    public void ResetCoin()
    {
        CoinCounter = 0;
        UpdateCoinUI();
    }

    public async void SaveCoin()
    {        
        try
        {
            string CoinStr = await m_bindings.SaveCoin(CoinCounter);
            if (CoinStr == null || CoinStr != CoinCounter.ToString())
            {
                throw new ArgumentException("Coin counter does not match");
            }
            Debug.Log($"PlayerDataManager: Saved coin counter: {CoinCounter}");
        }
        catch (Exception ex)
        {
            Debug.Log($"PlayerDataManager: Error when saving coin counter: {ex.Message}");
        }
    }

    // UI functions
    public void UpdatePlayerNameUI()
    {
        if (!string.IsNullOrEmpty(PlayerName))
        {
            PlayerNameText.text = $"PlayerName: {PlayerName}";
        }
        else
        {
            PlayerNameText.text = "PlayerName not set.";
        }
    }

    public void UpdateCoinUI()
    {
        if (CoinCounter <= 1)
        {
            CoinText.text = $"You have: {CoinCounter} coin.";
        }
        else
        {
            CoinText.text = $"You have: {CoinCounter} coins.";
        }
    }

    // OnDisable
    private void OnDisable()
    {
        loginManager.PlayerSignedIn -= InitializePlayer;
    }
}