using System;
using System.Threading.Tasks;
using IdentityModel.OidcClient;
using IdentityModel.OidcClient.Browser;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SecureSettings;
using UniGetUI.Core.SettingsEngine; // For potentially storing username
using Windows.System;

// Octokit might be needed here later for user info fetching
// using Octokit;

namespace UniGetUI.Services
{
    public class GitHubAuthService
    {
        private const string GitHubClientId = "Iv23libnfvYqGI2ubBvI";
        private const string GitHubAuthority = "https://github.com/";
        // This must be registered in App.xaml.cs for protocol activation
        // And also in the GitHub OAuth App settings
        public const string RedirectUri = "unigetui-auth://callback";

        private readonly OidcClientOptions _options;
        private readonly OidcClient _client;

        // To store basic user info like login name
        private const Settings.K GitHubUserLoginSettingKey = Settings.K.GitHubUserLogin;

        public static TaskCompletionSource<string> CallbackCompletionSource { get; private set; }

        public GitHubAuthService()
        {
            _options = new OidcClientOptions
            {
                Authority = GitHubAuthority,
                ClientId = GitHubClientId,
                RedirectUri = RedirectUri,
                Scope = "read:user gist", // For reading user profile and creating Gists
                Browser = new WinUIBrowser(),
                Policy = new Policy
                {
                    RequireIdentityTokenSignature = false, // GitHub doesn't always send id_token_hint
                }
            };
            _client = new OidcClient(_options);
        }

        public async Task<bool> SignInAsync()
        {
            try
            {
                Logger.Info("Initiating GitHub sign-in process...");
                LoginResult loginResult = await _client.LoginAsync(new LoginRequest());

                if (loginResult.IsError)
                {
                    Logger.Error($"GitHub login failed: {loginResult.ErrorDescription}");
                    await ClearAuthenticatedUserDataAsync(); // Clear any partial data
                    return false;
                }

                Logger.Info("GitHub login successful. Storing access token.");
                await SecureTokenManager.StoreTokenAsync(loginResult.AccessToken);

                // Optionally, fetch and store user's GitHub login name
                // For now, we'll assume the Name claim from UserInfo might be sufficient if available
                // Or use Octokit to fetch more details.
                // Attempt to get 'login' (username) claim first, then 'name'
                string? userName = loginResult.User?.FindFirst("login")?.Value ?? loginResult.User?.FindFirst("name")?.Value;
                if (!string.IsNullOrEmpty(userName))
                {
                    Settings.SetValue(GitHubUserLoginSettingKey, userName);
                    Logger.Info($"Logged in as GitHub user: {userName}");
                }
                else
                {
                    Logger.Warn("Could not retrieve GitHub username from login result.");
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Exception during GitHub sign-in process:");
                Logger.Error(ex);
                await ClearAuthenticatedUserDataAsync();
                return false;
            }
        }

        public async Task SignOutAsync()
        {
            Logger.Info("Signing out from GitHub...");
            await ClearAuthenticatedUserDataAsync();
            // Optionally: Add token revocation logic here if GitHub supports it for this flow
            // await _client.RevokeTokenAsync( ... ); // This would need the token and potentially client secret for confidential clients
            Logger.Info("GitHub sign-out complete.");
        }

        private async Task ClearAuthenticatedUserDataAsync()
        {
            await SecureTokenManager.DeleteTokenAsync();
            Settings.SetValue(GitHubUserLoginSettingKey, ""); // Clear stored username
        }

        public async Task<string?> GetAccessTokenAsync()
        {
            return await SecureTokenManager.GetTokenAsync();
        }

        public async Task<string?> GetAuthenticatedUserLoginAsync()
        {
            string? storedLogin = Settings.GetValue(GitHubUserLoginSettingKey);
            // Ensure we await the task if GetValue becomes async in the future, for now it's synchronous
            await Task.CompletedTask;
            return string.IsNullOrEmpty(storedLogin) ? null : storedLogin;
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            // This is a quick check based on token presence.
            var token = await GetAccessTokenAsync();
            return !string.IsNullOrEmpty(token);
        }


        private class WinUIBrowser : IBrowser
        {
            public async Task<BrowserResult> InvokeAsync(BrowserOptions options, System.Threading.CancellationToken cancellationToken = default)
            {
                try
                {
                    Logger.Debug($"Launching browser for GitHub authentication at URL: {options.StartUrl}");
                    CallbackCompletionSource = new TaskCompletionSource<string>();

                    await Launcher.LaunchUriAsync(new Uri(options.StartUrl));

                    // Wait for the protocol activation to complete and provide the callback URL
                    string callbackUrl = await CallbackCompletionSource.Task;

                    if (string.IsNullOrEmpty(callbackUrl))
                    {
                        return new BrowserResult { ResultType = BrowserResultType.UserCancel };
                    }

                    return new BrowserResult
                    {
                        ResultType = BrowserResultType.Success,
                        Response = callbackUrl
                    };
                }
                catch (Exception ex)
                {
                    Logger.Error($"Browser invocation error: {ex.Message}");
                    Logger.Error(ex);
                    return new BrowserResult { ResultType = BrowserResultType.UnknownError, Error = ex.ToString() };
                }
            }
        }
    }
}
