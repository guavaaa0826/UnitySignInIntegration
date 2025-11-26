using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Unity.Services.Authentication;
using Unity.Services.Authentication.PlayerAccounts;
using Unity.Services.Core;
using TMPro;
using Facebook.Unity;

public class LoginManager : MonoBehaviour
{
    // Variables
    public enum LoginState
    {
        None,
        Unity,
        Facebook
    }
    private LoginState currentLogin;
    private string FacebookToken;
    public PlayerDataManager playerDataManager;

    // Player data management
    public Action PlayerSignedIn, PlayerSignedOut;

    // UI elements
    public TextMeshProUGUI LoginStateText, PlayerNameText, CoinText;
    public TMP_InputField PlayerNameInputField;
    public Button UnityLoginButton, FacebookLoginButton;
    public Button LogoutButton;

    // Initialization functions and callbacks
    private async void Awake()
    {
        InitializeFacebook();
        InitializeUnity();
    }

    private void InitializeFacebook()
    {
        Debug.Log("LoginManager: Facebook Service Initializing.");
        if (!FB.IsInitialized)
        {
            FB.Init(FacebookInitCallback, null); // Initialize the Facebook SDK
        }
        else
        {
            FB.ActivateApp(); // Already initialized, signal an app activation App Event
            Debug.Log("LoginManager: Facebook Service already initialized.");
        }
    }

    private void FacebookInitCallback()
    {
        if (FB.IsInitialized)
        {
            FB.ActivateApp(); // Signal an app activation App Event
            Debug.Log("LoginManager: Facebook Service Initialization completed.");
        }
        else
        {
            Debug.Log("LoginManager: Failed to Initialize the Facebook SDK");
        }
    }

    private async void InitializeUnity()
    {
        if (UnityServices.State == ServicesInitializationState.Uninitialized)
        {
            Debug.Log("LoginManager: Unity Service Initializing.");
            await UnityServices.InitializeAsync();
        }
    }

    private async void Start()
    {
        UnityLoginButton.onClick.AddListener(StartUnitySignInAsync);
        FacebookLoginButton.onClick.AddListener(FacebookSignIn);
        LogoutButton.onClick.AddListener(Logout);

        currentLogin = LoginState.None;
        PlayerSignedIn += UpdateLoginStateUI;
        UpdateLoginStateUI();
    }

    // Login/Logout function
    // Unity
    private async void StartUnitySignInAsync()
    {
        Debug.Log("LoginManager: Signing in with Unity.");

        try
        {
            if (PlayerAccountService.Instance.IsSignedIn)
            {
                OnUnitySignedIn();
                return;
            }

            await PlayerAccountService.Instance.StartSignInAsync();
            PlayerAccountService.Instance.SignedIn += OnUnitySignedIn;
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"LoginManager: Unity Sign-in failed with error: {ex.Message}");
        }
    }

    private async void OnUnitySignedIn()
    {
        PlayerAccountService.Instance.SignedIn -= OnUnitySignedIn;
        Debug.Log("LoginManager: Player Account signed in. Now signing into Authentication...");

        try
        {
            await AuthenticationService.Instance.SignInWithUnityAsync(PlayerAccountService.Instance.AccessToken);

            currentLogin = LoginState.Unity;
            PlayerSignedIn.Invoke();
            Debug.Log("LoginManager: Unity sign-in fully completed.");
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"LoginManager: Authentication sign-in failed: {ex.Message}");
        }
    }

    private void UnitySignOut()
    {
        Debug.Log("LoginManager: Signing out with Unity.");

        AuthenticationService.Instance.SignOut();
        PlayerAccountService.Instance.SignOut();
        currentLogin = LoginState.None;
        UpdateLoginStateUI();
        ResetPlayerNameUI();
        playerDataManager.ResetCoin();

        Debug.Log("LoginManager: Unity sign out succeeded.");
    }

    // Facebook
    public void FacebookSignIn()
    {
        Debug.Log("LoginManager: Signing in with Facebook.");
        var perms = new List<string>() { "public_profile", "email" }; // Define the permissions

        #if UNITY_ANDROID
        FB.LogInWithReadPermissions(perms, async result =>
        {
            if (FB.IsLoggedIn)
            {
                FacebookToken = AccessToken.CurrentAccessToken.TokenString;
                await SignInWithFacebookAsync(FacebookToken);
            }
            else
            {
                Debug.Log("LoginManager: User cancelled Facebook login.");
            }
        });
        #endif

        #if UNITY_IOS
        // FB.Mobile.LoginWithTrackingPreference();
        #endif
    }

    async Task SignInWithFacebookAsync(string token)
    {
        try
        {
            await AuthenticationService.Instance.SignInWithFacebookAsync(token);

            currentLogin = LoginState.Facebook;
            PlayerSignedIn.Invoke();
            Debug.Log("LoginManager: Facebook sign in succeeded.");
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"LoginManager: Facebook Sign-in failed with error: {ex.Message}");
        }
    }
    
    public void FacebookSignOut()
    {
        Debug.Log("LoginManager: Signing out from Facebook.");
        
        FB.LogOut();
        FacebookToken = null;
        
        try
        {
            AuthenticationService.Instance.SignOut();

            currentLogin = LoginState.None;
            UpdateLoginStateUI();
            ResetPlayerNameUI();
            playerDataManager.ResetCoin();

            Debug.Log("LoginManager: Facebook sign out succeeded.");
        }
        catch (RequestFailedException ex)
        {
            Debug.LogError($"LoginManager: Facebook Sign-out failed with error: {ex.Message}");
        }
    }

    // Overall logout function
    public void Logout()
    {
        switch (currentLogin)
        {
            case LoginState.Unity:
                UnitySignOut();
                break;
            case LoginState.Facebook:
                FacebookSignOut();
                break;
            default:
                break;
        }
    }

    // UI Update function
    private void UpdateLoginStateUI()
    {
        switch (currentLogin)
        {
            case LoginState.Unity:
                LoginStateText.text = "LoginState: Unity";
                break;
            case LoginState.Facebook:
                LoginStateText.text = "LoginState: Facebook";
                break;
            default:
                LoginStateText.text = "LoginState: None";
                break;
        }
    }

    private void ResetPlayerNameUI()
    {
        PlayerNameText.text = "PlayerName not set.";
        PlayerNameInputField.text = "";
    }

    // OnDestroy()
    private void OnDestroy()
    {
        PlayerAccountService.Instance.SignedIn -= OnUnitySignedIn;
        PlayerSignedIn -= UpdateLoginStateUI;
    }
}