using COSA_GetAuth.Coastal.Helpers;
using Microsoft.Extensions.Configuration;
using System.Linq.Expressions;
using Xero.NetStandard.OAuth2.Client;
using Xero.NetStandard.OAuth2.Config;
using Xero.NetStandard.OAuth2.Models;
using Xero.NetStandard.OAuth2.Token;


namespace COSA_GetAuth.Services
{
    public class XeroAuthService
    {
        private XeroClient _api;
        private string? accessToken;
        private string? refreshToken;
        private string? idToken;
        private string? tenantId;
        private string? code;

        public XeroAuthService()
        {
            XeroConfiguration xeroConfig = new XeroConfiguration();
            xeroConfig.ClientId = ConfigHelper.Setting("ClientId");
            xeroConfig.ClientSecret = ConfigHelper.Setting("ClientSecret");
            string callbackUrl = ConfigHelper.Setting("CallbackUri").ToString();

            xeroConfig.CallbackUri = new Uri(callbackUrl);
            xeroConfig.Scope = ConfigHelper.Setting("Scope");
            _api = new XeroClient(xeroConfig);

        }

        public void GetCode()
        {
            LogHelper.LogDetail("GetCode");

            string loginUrl = _api.BuildLoginUri();
            Console.WriteLine(string.Format("Copy this link into a browser and click enter"));
            Console.WriteLine();
            Console.WriteLine(string.Format("{0}", loginUrl));
            Console.WriteLine();
            Console.WriteLine(string.Format("Enter code"));
            code = Console.ReadLine();

            WriteCode();

        }


        public void GetTokens()
        {
            LogHelper.LogDetail("GetTokens");

            XeroOAuth2Token xeroToken = new XeroOAuth2Token();

            try
            {
                xeroToken = (XeroOAuth2Token)_api.RequestAccessTokenAsync(code).Result;

                accessToken = xeroToken.AccessToken;
                idToken = xeroToken.IdToken;
                refreshToken = xeroToken.RefreshToken;

            }
            catch (Exception ex)
            {
                string? msg = ex.Message;
                LogHelper.LogDetail(string.Format("Error in _api.RequestAccessTokenAsync {0}",  msg));
            }

            Console.WriteLine();

            Console.WriteLine("WriteAccessToken...");
            LogHelper.LogDetail("WriteAccessToken");
            WriteAccessToken();

            Console.WriteLine("WriteRefreshToken...");
            LogHelper.LogDetail("WriteRefreshToken");
            WriteRefreshToken();

            Console.WriteLine("WriteIdToken...");
            LogHelper.LogDetail("WriteIdToken");
            WriteIdToken();

            Console.WriteLine();

            var tenants = _api.GetConnectionsAsync(xeroToken).Result;

            foreach (var tenant in tenants)
            {
                string tenId = tenant.TenantId.ToString();
                string tenName = tenant.TenantName;
                Console.WriteLine(string.Format("Tenant = {0} | TenantId = {1}", tenName, tenId));
                LogHelper.LogDetail(string.Format("Tenant = {0} | TenantId = {1}", tenName, tenId));

            }
            Console.WriteLine();
            Console.WriteLine(string.Format("Enter TenantId"));

            tenantId = Console.ReadLine();


            Console.WriteLine("WriteTenantId...");
            LogHelper.LogDetail("WriteTenantId");
            WriteTenantId();

            Console.WriteLine("Finished thanks, click enter to exit");
            Console.ReadLine();

            LogHelper.LogSummary("Tokens completed");
            LogHelper.LogDetail("Tokens completed");

        }


        public void RefreshTokens()
        {
            LogHelper.LogSummary("======================================================================================");
            LogHelper.LogSummary("RefreshTokens");


            LogHelper.LogDetail("======================================================================================");
            LogHelper.LogDetail("RefreshTokens");
            Console.WriteLine("RefreshTokens...");

            //Console.WriteLine("Read existing Tokens...");

            accessToken = ReadFromFileBase("AccessToken");
            idToken = ReadFromFileBase("IdToken");
            refreshToken = ReadFromFileBase("RefreshToken");
            //tenantId = FileHelper.ReadFromFileBase("TenantId");

            XeroOAuth2Token xeroToken = new XeroOAuth2Token
            {
                AccessToken = accessToken,
                IdToken = idToken,
                RefreshToken = refreshToken
            };

            //Console.WriteLine("Refreshing Tokens...");
            XeroOAuth2Token newToken = (XeroOAuth2Token)_api.RefreshAccessTokenAsync(xeroToken).Result;

            accessToken = newToken.AccessToken;
            idToken = newToken.IdToken;
            refreshToken = newToken.RefreshToken;


            Console.WriteLine("...AccessToken...");
            WriteAccessToken();

            Console.WriteLine("...RefreshToken...");
            WriteRefreshToken();

            Console.WriteLine("...IdToken...");
            WriteIdToken();

            //Console.WriteLine();

            //Console.WriteLine("Finished thanks, click enter to exit");
            //Console.ReadLine();

        }

        public void DisconnectTenant()
        {

            accessToken = ReadFromFileBase("AccessToken");
            idToken = ReadFromFileBase("IdToken");
            refreshToken = ReadFromFileBase("RefreshToken");

            XeroOAuth2Token xeroToken = new XeroOAuth2Token
            {
                AccessToken = accessToken,
                IdToken = idToken,
                RefreshToken = refreshToken
            };


            List<Tenant> tenants = _api.GetConnectionsAsync(xeroToken).Result;

            foreach (var tenant in tenants)
            {
                string tenId = tenant.TenantId.ToString();
                string tenName = tenant.TenantName;
                Console.WriteLine(string.Format("Tenant = {0} | TenantId = {1}", tenName, tenId));

            }
            Console.WriteLine();
            Console.WriteLine(string.Format("Enter TenantId"));

            tenantId = Console.ReadLine();

            Tenant tenantToDisconnet = tenants.Where(w => w.TenantId.ToString() == tenantId).FirstOrDefault();
            var tenantsRemain = _api.DeleteConnectionAsync(xeroToken, tenantToDisconnet);

            Console.WriteLine("Finished thanks, click enter to exit");
            Console.ReadLine();

        }

        public void ShowPause()
        {
            Console.WriteLine(string.Format("Click enter in this dialog when done"));
        }

        public void WriteCode()
        {
            WriteBase("Code", code);
        }

        public void WriteAccessToken()
        {
            WriteBase("AccessToken", accessToken);
        }

        public void WriteRefreshToken()
        {
            WriteBase("RefreshToken", refreshToken);
        }

        public void WriteIdToken()
        {
            WriteBase("IdToken", idToken);
        }

        public void WriteTenantId()
        {
            WriteBase("TenantId", tenantId);
        }

        public void WriteBase(string type, string content)
        {
            try
            {

                string authFilesFolder = ConfigHelper.Setting("AuthFilesFolder");
                string authFileName = FileHelper.GetAuthFilename(type);
                string authFullFileName = Path.Combine(authFilesFolder, authFileName) + ".txt";

                if (!File.Exists(authFullFileName))
                {
                    File.Create(authFullFileName);
                }

                FileHelper.WriteToFile(authFullFileName, content);

            }
            catch(Exception ex)
            {
                string? msg = ex.Message;
                LogHelper.LogDetail(string.Format("Error in WriteBase {0} {1} {2}", type,content,msg));
            }

        }


        public static string ReadFromFileBase(string type)
        {
            string? result = null;
            try
            {
                string authFilesFolder = ConfigHelper.Setting("AuthFilesFolder");
                string authFileName = FileHelper.GetAuthFilename(type);
                string authFullFileName = Path.Combine(authFilesFolder, authFileName) + ".txt";

                if (File.Exists(authFullFileName))
                {
                    result = FileHelper.ReadFromFile(authFullFileName);
                }

            }
            catch (Exception ex)
            {
                string? msg = ex.Message;
                LogHelper.LogDetail(string.Format("Error in ReadFromFileBase {0} {1}", type,  msg));
            }

            return result;
        }

    }
}
