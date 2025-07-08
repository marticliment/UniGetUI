using System;
using System.Threading.Tasks;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SecureSettings;
using UniGetUI.Core.SettingsEngine;
using Windows.System;
using Octokit;
using System.Net;
using System.Text;

namespace UniGetUI.Services
{
    public class GitHubAuthService
    {
        private const string GitHubClientId = Secrets.GitHubClientId;
        private const string GitHubClientSecret = Secrets.GitHubClientSecret;

        private const string RedirectUri = "http://127.0.0.1:58642/";

        private readonly GitHubClient _client;
        private const Settings.K GitHubUserLoginSettingKey = Settings.K.GitHubUserLogin;

        public GitHubAuthService()
        {
            _client = new GitHubClient(new ProductHeaderValue("UniGetUI"));
        }

        public async Task<bool> SignInAsync()
        {
            HttpListener httpListener = null;
            try
            {
                Logger.Info("Initiating GitHub sign-in process using loopback redirect...");

                httpListener = new HttpListener();
                httpListener.Prefixes.Add(RedirectUri);
                httpListener.Start();
                Logger.Info($"Listening for GitHub callback on {RedirectUri}");

                var request = new OauthLoginRequest(GitHubClientId)
                {
                    Scopes = { "read:user", "gist" },
                    RedirectUri = new Uri(RedirectUri)
                };

                var oauthLoginUrl = _client.Oauth.GetGitHubLoginUrl(request);

                await Launcher.LaunchUriAsync(oauthLoginUrl);

                var context = await httpListener.GetContextAsync();

                var response = context.Response;
                string responseString = "<html><head><title>Auth Success</title></head><body><h1>Authentication successful!</h1><p>You can now close this window and return to the UniGetUI application.</p></body></html>";
                var buffer = Encoding.UTF8.GetBytes(responseString);
                response.ContentLength64 = buffer.Length;
                var output = response.OutputStream;
                await output.WriteAsync(buffer, 0, buffer.Length);
                output.Close();

                httpListener.Stop();
                Logger.Info("GitHub callback received and processed.");

                var code = context.Request.QueryString["code"];
                if (string.IsNullOrEmpty(code))
                {
                    var error = context.Request.QueryString["error"];
                    var errorDescription = context.Request.QueryString["error_description"];
                    Logger.Error($"GitHub OAuth callback returned an error: {error} - {errorDescription}");
                    return false;
                }

                return await CompleteSignInAsync(code);
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 5) // Access Denied
            {
                Logger.Error("Access denied to the http listener. Please run the following command in an administrator terminal:");
                Logger.Error($"netsh http add urlacl url={RedirectUri} user=Everyone");
                // Optionally, you could try to run this command for the user.
                // For now, just logging the instruction.
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("Exception during GitHub sign-in process:");
                Logger.Error(ex);
                await ClearAuthenticatedUserDataAsync();
                return false;
            }
            finally
            {
                httpListener?.Stop();
            }
        }

        private async Task<bool> CompleteSignInAsync(string code)
        {
            try
            {
                var tokenRequest = new OauthTokenRequest(GitHubClientId, GitHubClientSecret, code)
                {
                    RedirectUri = new Uri(RedirectUri) // The same redirect_uri must be sent
                };
                var token = await _client.Oauth.CreateAccessToken(tokenRequest);

                if (string.IsNullOrEmpty(token.AccessToken))
                {
                    Logger.Error("Failed to obtain GitHub access token.");
                    return false;
                }

                Logger.Info("GitHub login successful. Storing access token.");
                await SecureTokenManager.StoreTokenAsync(token.AccessToken);

                var userClient = new GitHubClient(new ProductHeaderValue("UniGetUI"))
                {
                    Credentials = new Credentials(token.AccessToken)
                };

                var user = await userClient.User.Current();
                if (user != null)
                {
                    Settings.SetValue(GitHubUserLoginSettingKey, user.Login);
                    Logger.Info($"Logged in as GitHub user: {user.Login}");
                }
                else
                {
                    Logger.Warn("Could not retrieve GitHub user information after login.");
                }

                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Exception during GitHub token exchange:");
                Logger.Error(ex);
                await ClearAuthenticatedUserDataAsync();
                return false;
            }
        }

        public async Task SignOutAsync()
        {
            Logger.Info("Signing out from GitHub...");
            await ClearAuthenticatedUserDataAsync();
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
            await Task.CompletedTask;
            return string.IsNullOrEmpty(storedLogin) ? null : storedLogin;
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            var token = await GetAccessTokenAsync();
            return !string.IsNullOrEmpty(token);
        }
    }
}
