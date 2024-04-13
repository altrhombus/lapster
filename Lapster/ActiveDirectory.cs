using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Text.Json;
using System.Windows.Forms.VisualStyles;

namespace Lapster
{
    public class ActiveDirectoryLaps
    {
        public ComputerPrincipalEx? GetComputerPrincipalEx(string computerName)
        {
            try
            {
                // Search the specified domain if FQDN is used
                if (computerName.Contains('.'))
                {
                    int periodIndex = computerName.IndexOf('.');
                    string specifiedDomainName = computerName.Substring(periodIndex + 1);
                    computerName = computerName.Substring(0, periodIndex);
                    PrincipalContext ActiveDirectoryComputer =
                        new PrincipalContext(ContextType.Domain, specifiedDomainName);
                    ComputerPrincipalEx computerPrincipalEx = ComputerPrincipalEx.FindByIdentity(ActiveDirectoryComputer, computerName);
                    return computerPrincipalEx;
                }
                // Search the local domain
                else
                {
                    PrincipalContext ActiveDirectoryComputer = 
                        new PrincipalContext(ContextType.Domain);
                    ComputerPrincipalEx computerPrincipalEx = ComputerPrincipalEx.FindByIdentity(ActiveDirectoryComputer, computerName);
                    return computerPrincipalEx;
                }
            }
            catch 
            {
                return null;
            }
        }
        public (string, string) GetMsMcsAdmPwdDataset(string computerName)
        {
            try
            {
                ComputerPrincipalEx? computerPrincipalEx = GetComputerPrincipalEx(computerName);

                if (computerPrincipalEx != null)
                {
                    string password = computerPrincipalEx.MsMcsAdmPwd;
                    string expiration = string.Empty;
                    try
                    {
                        long msMcsAdmPwdExpirationFileTime = computerPrincipalEx.MsMcsAdmPwdExpirationTime;
                        DateTime dateTime = DateTime.FromFileTimeUtc(msMcsAdmPwdExpirationFileTime);
                        expiration = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
                    }
                    catch (Exception)
                    {
                        expiration = "Error retrieving expiration";
                    }

                    return (password, expiration);
                }
                else
                {
                    return (string.Empty, string.Empty);
                }
			}
			catch (Exception ex)
			{
                return (string.Empty, string.Empty);
            }
        }

        public (string, string, string, bool) GetMsLapsPasswordDataset(string computerName)
        {
            try
            {
                ComputerPrincipalEx? computerPrincipalEx = GetComputerPrincipalEx(computerName);

                if (computerPrincipalEx != null)
                {
                    if (!string.IsNullOrEmpty(computerPrincipalEx.MsLapsEncryptedPassword))
                    {

                        string dataset = computerPrincipalEx.MsLapsEncryptedPassword.ToString();
                        if (!dataset.EndsWith('}'))
                        {
                            int index = dataset.LastIndexOf('}');
                            dataset = dataset.Substring(0, index + 1);
                        }
                        JsonDocument doc = JsonDocument.Parse(dataset);
                        JsonElement root = doc.RootElement;
                        string n = root.GetProperty("n").GetString();
                        string t = root.GetProperty("t").GetString();
                        string p = root.GetProperty("p").GetString();

                        long refreshFileTime = Convert.ToInt64(t, 16);
                        DateTime refreshDateTime = DateTime.FromFileTime(refreshFileTime);
                        string ft = refreshDateTime.ToString("yyyy-MM-dd HH:mm:ss");

                        return (n, ft, p, true);
                    }
                    else
                    {
                        string dataset = computerPrincipalEx.MsLapsPassword.ToString();
                        if (!dataset.EndsWith('}'))
                        {
                            int index = dataset.LastIndexOf('}');
                            dataset = dataset.Substring(0, index + 1);
                        }
                        JsonDocument doc = JsonDocument.Parse(dataset);
                        JsonElement root = doc.RootElement;
                        string n = root.GetProperty("n").GetString();
                        string t = root.GetProperty("t").GetString();
                        string p = root.GetProperty("p").GetString();

                        long refreshFileTime = Convert.ToInt64(t, 16);
                        DateTime refreshDateTime = DateTime.FromFileTime(refreshFileTime);
                        string ft = refreshDateTime.ToString("yyyy-MM-dd HH:mm:ss");

                        return (n, ft, p, false);
                    }
                }
                else
                {
                    return (string.Empty, string.Empty, string.Empty, false);
                }
            }
            catch (Exception ex)
            {
                return (string.Empty, string.Empty, string.Empty, false);
            }
        }

        public string GetMsLapsPasswordExpirationDataset(string computerName)
        {
            try
            {
                ComputerPrincipalEx? computerPrincipalEx = GetComputerPrincipalEx(computerName);

                string expiration = string.Empty;
                try
                {
                        long msLapsPasswordExpirationFileTime = computerPrincipalEx.MsLapsPasswordExpirationTime;
                        DateTime dateTime = DateTime.FromFileTimeUtc(msLapsPasswordExpirationFileTime);
                        expiration = dateTime.ToString("yyyy-MM-dd HH:mm:ss");
                }
                catch (Exception)
                {
                    expiration = "";
                }

                return expiration;
            }
            catch (Exception e)
            {
                return string.Empty;
            }
        }

        public (string, string, string) ParseJsonCredentialEntry(string dataset)
        {
            if (!dataset.EndsWith('}'))
            {
                int index = dataset.LastIndexOf('}');
                dataset = dataset.Substring(0, index + 1);
            }
            try
            {
                JsonDocument doc = JsonDocument.Parse(dataset);
                JsonElement root = doc.RootElement;
                string n = root.GetProperty("n").GetString();
                string t = root.GetProperty("t").GetString();
                string p = root.GetProperty("p").GetString();

                long refreshFileTime = Convert.ToInt64(t, 16);
                DateTime refreshDateTime = DateTime.FromFileTime(refreshFileTime);
                string ft = refreshDateTime.ToString("yyyy-MM-dd HH:mm:ss");

                return (n, ft, p);
            }
            catch (Exception)
            {
                throw;
            }

        }

        public (string[], string[], string[]) GetMsLapsPasswordHistoryDataset(string computerName)
        {
            try
            {
                ComputerPrincipalEx? computerPrincipalEx = GetComputerPrincipalEx(computerName);

                if (computerPrincipalEx != null)
                {
                    string[] passwordHistory = computerPrincipalEx.MsLapsEncryptedPasswordHistory;
                    string[] name = new string[passwordHistory.Length];
                    string[] refreshtime = new string[passwordHistory.Length];
                    string[] password = new string[passwordHistory.Length];

                    for (int i = 0; i < passwordHistory.Length; i++)
                    {
                        var passwordHistoryDataset = ParseJsonCredentialEntry(passwordHistory[i]);
                        (string n, string t, string p) = passwordHistoryDataset;
                        name[i] = n;
                        refreshtime[i] = t;
                        password[i] = p;
                    }

                    return (name, refreshtime, password);
                }
                else
                {
                    return (new string[1], new string[1], new string[1]);
                }
            }
            catch (Exception ex)
            {
                return (new string[1], new string[1], new string[1]);
            }
        }
    }

}
