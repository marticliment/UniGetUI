using System.Net;
using System.Text;
using Octokit;
using UniGetUI.Core.Data;
using UniGetUI.Core.Logging;
using UniGetUI.Core.SecureSettings;
using UniGetUI.Core.SettingsEngine;
using Windows.System;

namespace UniGetUI.Services
{
    public class GitHubAuthService
    {
        private readonly string GitHubClientId = Secrets.GetGitHubClientId();
        private readonly string GitHubClientSecret = Secrets.GetGitHubClientSecret();

        private const string DataReceivedWebsite = """
           <html>
               <style>
                   div {
                       display: flex;
                       flex-direction: column;
                       align-items: center;
                       justify-content: center;
                       height: 100vh;
                       font-family: sans-serif;
                       text-align: center;
                   }
               </style>
               <script>
                   window.close();
               </script>
               <div>
                   <title>UniGetUI authentication</title>
                   <h1>Authentication successful</h1>
                   <p>You can now close this window and return to UniGetUI</p>
               </div>
           </html>
           """;

        private const string RedirectUri = "http://127.0.0.1:58642/";

        private readonly GitHubClient _client;

        public static event EventHandler<EventArgs>? AuthStatusChanged;
        public GitHubAuthService()
        {
            _client = new GitHubClient(new ProductHeaderValue("UniGetUI", CoreData.VersionName));
        }

        public GitHubClient? CreateGitHubClient()
        {
            var token = GetAccessToken();

            if (string.IsNullOrEmpty(token))
            {
                Logger.Error("GitHub access token is not available. Cannot perform Gist operation.");
                return null;
            }

            return new GitHubClient(new ProductHeaderValue("UniGetUI", CoreData.VersionName))
            {
                Credentials = new Credentials(token)
            };
        }

        public async Task<bool> SignInAsync()
        {
            HttpListener? httpListener = null;
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
                var buffer = Encoding.UTF8.GetBytes(DataReceivedWebsite);
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
                    AuthStatusChanged?.Invoke(this, EventArgs.Empty);
                    return false;
                }

                return await _completeSignInAsync(code);
            }
            catch (HttpListenerException ex) when (ex.ErrorCode == 5) // Access Denied
            {
                Logger.Error("Access denied to the http listener. Please run the following command in an administrator terminal:");
                Logger.Error($"netsh http add urlacl url={RedirectUri} user=Everyone");
                // Optionally, you could try to run this command for the user.
                // For now, just logging the instruction.
                AuthStatusChanged?.Invoke(this, EventArgs.Empty);
                return false;
            }
            catch (Exception ex)
            {
                Logger.Error("Exception during GitHub sign-in process:");
                Logger.Error(ex);
                ClearAuthenticatedUserData();
                AuthStatusChanged?.Invoke(this, EventArgs.Empty);
                return false;
            }
            finally
            {
                httpListener?.Stop();
            }
        }

        private async Task<bool> _completeSignInAsync(string code)
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
                    AuthStatusChanged?.Invoke(this, EventArgs.Empty);
                    return false;
                }

                Logger.Info("GitHub login successful. Storing access token.");
                SecureGHTokenManager.StoreToken(token.AccessToken);

                var userClient = new GitHubClient(new ProductHeaderValue("UniGetUI"))
                {
                    Credentials = new Credentials(token.AccessToken)
                };

                var user = await userClient.User.Current();
                if (user != null)
                {
                    Settings.SetValue(Settings.K.GitHubUserLogin, user.Login);
                    Logger.Info($"Logged in as GitHub user: {user.Login}");
                }
                else
                {
                    Logger.Warn("Could not retrieve GitHub user information after login.");
                }

                AuthStatusChanged?.Invoke(this, EventArgs.Empty);
                return true;
            }
            catch (Exception ex)
            {
                Logger.Error("Exception during GitHub token exchange:");
                Logger.Error(ex);
                ClearAuthenticatedUserData();
                AuthStatusChanged?.Invoke(this, EventArgs.Empty);
                return false;
            }
        }

        public void SignOut()
        {
            Logger.Info("Signing out from GitHub...");
            try
            {
                ClearAuthenticatedUserData();
            }
            catch (Exception ex)
            {
                Logger.Error("Failed to log out:");
                Logger.Error(ex);
            }

            AuthStatusChanged?.Invoke(this, EventArgs.Empty);
            Logger.Info("GitHub sign-out complete.");
        }

        private static void ClearAuthenticatedUserData()
        {
            SecureGHTokenManager.DeleteToken();
            Settings.SetValue(Settings.K.GitHubUserLogin, ""); // Clear stored username
        }

        public string? GetAccessToken()
        {
            return SecureGHTokenManager.GetToken();
        }

        public async Task<string?> GetAuthenticatedUserLoginAsync()
        {
            string? storedLogin = Settings.GetValue(Settings.K.GitHubUserLogin);
            await Task.CompletedTask;
            return string.IsNullOrEmpty(storedLogin) ? null : storedLogin;
        }

        public bool IsAuthenticated()
        {
            var token = GetAccessToken();
            return !string.IsNullOrEmpty(token);
        }
    }
}
