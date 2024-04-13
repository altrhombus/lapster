using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Data;
using System.Text;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Tokens;

namespace Lapster
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //Set the scope for API call to user.read
        string[] scopes = ["user.read", "device.read.all", "devicelocalcredential.read.all"];
        AuthenticationResult authResult = null;
        ObservableCollection<string> logItems = new ObservableCollection<string>();
        
        public MainWindow()
        {
            InitializeComponent();
            txtTokenInfo.Text = "Not currently signed in to Microsoft Graph";
            lbLog.ItemsSource = logItems;
            dgWindowsLapsHistory.Visibility = Visibility.Hidden;
            logItems.CollectionChanged += (s, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add)
                {
                    object newItem = e.NewItems[e.NewItems.Count - 1];
                    lbLog.SelectedItem = newItem;
                    lbLog.ScrollIntoView(newItem);
                }
            };
        }

        private void addLogEntry(string item)
        {
            string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            logItems.Add($"[{timestamp}] {item}");
        }

        /// <summary>
        /// Perform an HTTP GET request to a URL using an HTTP Authorization header
        /// </summary>
        /// <param name="url">The URL</param>
        /// <param name="token">The token</param>
        /// <returns>String containing the results of the GET operation</returns>
        public async Task<string> GetHttpContentWithToken(string url, string token)
        {
            var httpClient = new System.Net.Http.HttpClient();
            System.Net.Http.HttpResponseMessage response;
            try
            {
                var request = new System.Net.Http.HttpRequestMessage(System.Net.Http.HttpMethod.Get, url);
                //Add the token in Authorization header
                request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
                request.Headers.UserAgent.ParseAdd("Lapster/1.0");
                response = await httpClient.SendAsync(request);
                var content = await response.Content.ReadAsStringAsync();

                return content;
            }
            catch (Exception ex)
            {
                return ex.ToString();
            }
        }

        /// <summary>
        /// Sign out the current user
        /// </summary>
        private async void btnGraphSignOut_Click(object sender, RoutedEventArgs e)
        {
            var accounts = await App.PublicClientApp.GetAccountsAsync();

            if (accounts.Any())
            {
                try
                {
                    await App.PublicClientApp.RemoveAsync(accounts.FirstOrDefault());
                    addLogEntry("User has signed-out");
                    //btnGraphSignIn.Visibility = Visibility.Visible;
                    //btnGraphSignOut.Visibility = Visibility.Collapsed;
                }
                catch (MsalException ex)
                {
                    addLogEntry($"Error signing-out user: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Display basic information contained in the token
        /// </summary>
        private void DisplayBasicTokenInfo(AuthenticationResult authResult)
        {
            txtTokenInfo.Text = string.Empty;
            if (authResult != null)
            {
                txtTokenInfo.Text += $"Connected to Microsoft Graph as: {authResult.Account.Username}" + Environment.NewLine;
                txtTokenInfo.Text += $"Token Expires: {authResult.ExpiresOn.ToLocalTime()}" + Environment.NewLine;
            }
            else
            {
                txtTokenInfo.Text = "Not currently signed in to Microsoft Graph";
            }
        }

        private void txtSearchBox_GotFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (tb.Text == "Search...")
            {
                tb.Text = "";
                tb.Foreground = Brushes.Black;
                tb.FontStyle = FontStyles.Normal;
            }

        }

        private void txtSearchBox_LostFocus(object sender, RoutedEventArgs e)
        {
            TextBox tb = (TextBox)sender;
            if (string.IsNullOrWhiteSpace(tb.Text))
            {
                tb.Text = "Search...";
                tb.Foreground = Brushes.Gray;
                tb.FontStyle = FontStyles.Italic;
            }
        }

        private async void txtSearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Clear everything to defaults
                grpLegacyLaps.IsEnabled = true;
                grpWindowsLapsCloud.IsEnabled = true;
                grpWindowsLapsOnPrem.IsEnabled = true;
                txtPasswordDisplay.Text = string.Empty;
                txtExpirationDisplay.Text = string.Empty;
                txtLegacyLapsPassword.Text = string.Empty;
                txtLegacyLapsPasswordExpiration.Text = string.Empty;
                txtWindowsLapsGraphAccount.Text = string.Empty;
                txtWindowsLapsGraphSid.Text = string.Empty;
                txtWindowsLapsGraphPassword.Text = string.Empty;
                txtWindowsLapsGraphExpiration.Text = string.Empty;
                txtWindowsLapsAccount.Text = string.Empty;
                txtWindowsLapsPassword.Text = string.Empty;
                txtWindowsLapsLastRefresh.Text = string.Empty;
                txtWindowsLapsExpiration.Text = string.Empty;
                chkIsEncrypted.IsChecked = false;
                chkIsDsrm.IsChecked = false;
                dgWindowsLapsHistory.Visibility = Visibility.Hidden;
                dgWindowsLapsHistory.ItemsSource = null;
                lblError.Content = string.Empty;
                lblError.Visibility = Visibility.Collapsed;
                lblSourceDisplay.Content = string.Empty;

                string refreshFormattedDateTime = string.Empty;
                byte[] pwdBase64 = null;

                // Control key was not also pressed, so we'll search Microsoft Graph
                if (!(Keyboard.Modifiers == ModifierKeys.Control))
                {
                    authResult = null;
                    var app = App.PublicClientApp;
                    txtTokenInfo.Text = string.Empty;

                    var accounts = await app.GetAccountsAsync();
                    var firstAccount = accounts.FirstOrDefault();

                    try
                    {
                        authResult = await app.AcquireTokenSilent(scopes, firstAccount)
                            .ExecuteAsync();
                    }
                    catch (MsalUiRequiredException ex)
                    {
                        // A MsalUiRequiredException happened on AcquireTokenSilent.
                        // This indicates you need to call AcquireTokenInteractive to acquire a token
                        System.Diagnostics.Debug.WriteLine($"MsalUiRequiredException: {ex.Message}");

                        try
                        {
                            authResult = await app.AcquireTokenInteractive(scopes)
                                .WithAccount(accounts.FirstOrDefault())
                                .WithPrompt(Microsoft.Identity.Client.Prompt.SelectAccount)
                                .ExecuteAsync();
                        }
                        catch (MsalException msalex)
                        {
                            addLogEntry($"Error Acquiring Token:{System.Environment.NewLine}{msalex}");
                        }
                    }
                    catch (Exception ex)
                    {
                        addLogEntry($"Error Acquiring Token Silently:{System.Environment.NewLine}{ex}");
                        return;
                    }

                    if (authResult != null)
                    {
                        string graphAPIEndpoint = $"https://graph.microsoft.com/beta/devices?$filter=displayName eq '{txtSearchBox.Text}'&$select=deviceId&$top=1";
                        string graphResponse = await GetHttpContentWithToken(graphAPIEndpoint, authResult.AccessToken);
                        string deviceId = string.Empty;
                        try
                        {
                            JsonDocument data = JsonDocument.Parse(graphResponse);
                            JsonElement root = data.RootElement;
                            if (root.TryGetProperty("error", out JsonElement errorElement))
                            {
                                string code = errorElement.GetProperty("code").GetString();
                                string message = errorElement.GetProperty("message").GetString();
                                addLogEntry($"Code: {code}, Message: {message}");
                            }
                        }
                        catch (Exception ex)
                        {
                            addLogEntry(ex.Message);
                        }

                        try
                        {
                            JsonDocument data = JsonDocument.Parse(graphResponse);
                            JsonElement valuesArray = data.RootElement.GetProperty("value");
                            deviceId = valuesArray[0].GetProperty("deviceId").ToString();
                        }
                        catch (Exception)
                        {
                            txtWindowsLapsGraphPassword.Text = string.Empty;
                        }
                        if (!string.IsNullOrEmpty(deviceId))
                        {
                            graphAPIEndpoint = $"https://graph.microsoft.com/v1.0/directory/deviceLocalCredentials/{deviceId}?$select=credentials";
                            graphResponse = await GetHttpContentWithToken(graphAPIEndpoint, authResult.AccessToken);
                            try
                            {
                                try
                                {
                                    JsonDocument dlcData = JsonDocument.Parse(graphResponse);
                                    JsonElement root = dlcData.RootElement;
                                    if (root.TryGetProperty("error", out JsonElement errorElement))
                                    {
                                        string code = errorElement.GetProperty("code").GetString();
                                        string message = errorElement.GetProperty("message").GetString();
                                        addLogEntry($"Code: {code}, Message: {message}");
                                        if (code == "authorization_error" && message == "Caller does not have access")
                                        {
                                            lblError.Visibility = Visibility.Visible;
                                            lblError.Content = "Authorization error. Did you PIM up?";
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    addLogEntry(ex.Message);
                                    grpWindowsLapsCloud.IsEnabled = false;
                                }
                                JsonDocument data = JsonDocument.Parse(graphResponse);
                                JsonElement credentialsArray = data.RootElement.GetProperty("credentials");

                                pwdBase64 = Convert.FromBase64String(credentialsArray[0].GetProperty("passwordBase64").ToString());
                                txtWindowsLapsGraphPassword.Text = Encoding.UTF8.GetString(pwdBase64);

                                string refreshDateProperty = data.RootElement.GetProperty("refreshDateTime").ToString();
                                DateTimeOffset dateTimeOffset = DateTimeOffset.Parse(refreshDateProperty);
                                DateTime refreshDateTime = dateTimeOffset.DateTime;
                                refreshFormattedDateTime = refreshDateTime.ToString("yyyy-MM-dd HH:mm:ss");
                                txtWindowsLapsGraphExpiration.Text = refreshFormattedDateTime;

                                txtWindowsLapsGraphAccount.Text = credentialsArray[0].GetProperty("accountName").ToString();
                                txtWindowsLapsGraphSid.Text = credentialsArray[0].GetProperty("accountSid").ToString();
                            }
                            catch (Exception ex)
                            {
                                addLogEntry(ex.Message);
                            }
                        }
                    }
                }
                if (Keyboard.Modifiers == ModifierKeys.Control)
                {
                    addLogEntry("Ctrl+Enter detected, not searching Microsoft Graph.");
                    grpWindowsLapsCloud.IsEnabled = false;
                }

                ActiveDirectoryLaps activeDirectoryLaps = new();
                var msMcsAdmPwdResult = activeDirectoryLaps.GetMsMcsAdmPwdDataset(txtSearchBox.Text);
                (string msMcsAdmPwdPassword, string msMcsAdmPwdExpiration) = msMcsAdmPwdResult;
                if (!string.IsNullOrEmpty(msMcsAdmPwdPassword))
                {
                    txtLegacyLapsPassword.Text = msMcsAdmPwdPassword;
                    txtLegacyLapsPasswordExpiration.Text = msMcsAdmPwdExpiration;
                }
                else
                {
                    grpLegacyLaps.IsEnabled = false;
                }

                var msLapsResult = activeDirectoryLaps.GetMsLapsPasswordDataset(txtSearchBox.Text);
                (string msLapsAccount, string msLapsLastRefresh, string msLapsPassword, bool isEncrypted) = msLapsResult;
                if (!string.IsNullOrEmpty(msLapsPassword))
                {
                    txtWindowsLapsAccount.Text = msLapsAccount;
                    txtWindowsLapsPassword.Text = msLapsPassword;
                    txtWindowsLapsLastRefresh.Text = msLapsLastRefresh;
                    chkIsEncrypted.IsChecked = isEncrypted;
                }
                else
                {
                    grpWindowsLapsOnPrem.IsEnabled = false;
                }

                string msLapsPasswordExpiration = activeDirectoryLaps.GetMsLapsPasswordExpirationDataset(txtSearchBox.Text);
                if (msLapsPasswordExpiration != "1601-01-01 00:00:00")
                {
                    txtWindowsLapsExpiration.Text = msLapsPasswordExpiration;
                }


                if (isEncrypted)
                {
                    var msLapsHistoryResult = activeDirectoryLaps.GetMsLapsPasswordHistoryDataset(txtSearchBox.Text);
                    (string[] msLapsAccountHistory, string[] msLapsRefreshHistory, string[] msLapsPasswordHistory) = msLapsHistoryResult;

                    DataTable passwordHistoryDataTable = new DataTable();
                    passwordHistoryDataTable.Columns.Add("Password", typeof(string));
                    passwordHistoryDataTable.Columns.Add("RefreshTime", typeof(string));
                    for (int i = 0; i < msLapsPasswordHistory.Length; i++)
                    {
                        passwordHistoryDataTable.Rows.Add(msLapsPasswordHistory[i], msLapsRefreshHistory[i]);
                    }

                    dgWindowsLapsHistory.ItemsSource = passwordHistoryDataTable.DefaultView;
                    dgWindowsLapsHistory.Visibility = Visibility.Visible;
                }


                Dictionary<int, DateTime> dateTimes = new Dictionary<int, DateTime>()
                {
                    { 0, string.IsNullOrEmpty(msMcsAdmPwdExpiration) ? DateTime.MinValue : DateTime.Parse(msMcsAdmPwdExpiration) },
                    { 1, string.IsNullOrEmpty(msLapsPasswordExpiration) ? DateTime.MinValue : DateTime.Parse(msLapsPasswordExpiration) },
                    { 2, string.IsNullOrEmpty(refreshFormattedDateTime) ? DateTime.MinValue : DateTime.Parse(refreshFormattedDateTime) }
                };

                DateTime today = DateTime.Today;
                KeyValuePair<int, DateTime> selectedDateTime;

                if (dateTimes.All(date => date.Value < today))
                {
                    // If all dates are in the past, select the one closest to today
                    selectedDateTime = dateTimes.Aggregate((current, next) => Math.Abs((current.Value - today).Ticks) < Math.Abs((next.Value - today).Ticks) ? current : next);
                }
                else
                {
                    // Otherwise, select the date that is furthest into the future
                    selectedDateTime = dateTimes.Aggregate((current, next) => current.Value > next.Value ? current : next);
                }

                if (selectedDateTime.Key == 0)
                {
                    txtPasswordDisplay.Text = msMcsAdmPwdPassword;
                    txtExpirationDisplay.Text = msMcsAdmPwdExpiration;
                    lblSourceDisplay.Content = "Legacy LAPS";
                }
                else if (selectedDateTime.Key == 1)
                {
                    txtPasswordDisplay.Text = msLapsPassword;
                    txtExpirationDisplay.Text = msLapsPasswordExpiration;
                    lblSourceDisplay.Content = "Windows LAPS (Active Directory)";
                }
                else if (selectedDateTime.Key == 2)
                {
                    txtPasswordDisplay.Text = Encoding.UTF8.GetString(pwdBase64);
                    txtExpirationDisplay.Text = refreshFormattedDateTime;
                    lblSourceDisplay.Content = "Windows LAPS (Cloud)";
                }
            }
        }

        private async void TextBox_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is TextBox)
            {
                TextBox textBox = sender as TextBox;
                if (!string.IsNullOrEmpty(textBox.Text))
                {
                    Clipboard.SetText(textBox.Text);
                    string previousText = textBox.Text;
                    textBox.Foreground = Brushes.Gray;
                    textBox.Text = "Copied!";
                    await Task.Delay(500);
                    //DispatcherTimer timer = new DispatcherTimer();
                    //timer.Interval = TimeSpan.FromMilliseconds(500);
                    //timer.Tick += (s, args) =>
                    //{
                        textBox.Text = previousText;
                        textBox.Foreground = Brushes.Black;
                        //timer.Stop();
                    //};
                    //timer.Start();
                }

            }
        }

        private void CheckBox_PreviewMouseLeftButtonDown(object sender, MouseEventArgs e)
        {
            e.Handled = true;
        }

        private void CheckBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            e.Handled = true;
        }

    }
}