
using COSA_XeroInt.Coastal.Helpers;
//using Xero.Api.Core.Model;
//using Xero.Api.Core.Model.Status;
//using Xero.Api.Infrastructure.Exceptions;
using Xero.NetStandard.OAuth2.Api;
using Xero.NetStandard.OAuth2.Client;
using Xero.NetStandard.OAuth2.Config;
using Xero.NetStandard.OAuth2.Model.Accounting;
using Xero.NetStandard.OAuth2.Token;

namespace COSA_XeroInt.Coastal.Services
{
    public class XeroService
    {
        private XeroClient _api;
        private string accessToken;
        private string refreshToken;
        private string idToken;
        private string tenantId;
        private AccountingApi accountingApi;

        public XeroService()
        {
            XeroConfiguration xeroConfig = new XeroConfiguration();
            xeroConfig.ClientId = ConfigHelper.Setting("ClientId"); // ConfigHelper.ClientId();
            xeroConfig.ClientSecret = ConfigHelper.Setting("ClientSecret");
            string callbackUrl = ConfigHelper.Setting("CallbackUri").ToString();
            xeroConfig.CallbackUri = new Uri(callbackUrl);
            xeroConfig.Scope = ConfigHelper.Setting("Scope");

            _api = new XeroClient(xeroConfig);

            //when to refresh???
            //just auto refresh once per week??

            //XeroOAuth2Token xeroToken = (XeroOAuth2Token)_api.RefreshAccessTokenAsync(xeroToken).Result;

            // or static read from file? after initial
            accessToken = ReadFromFileBase("AccessToken");
            tenantId = ReadFromFileBase("TenantId");

            accountingApi = new AccountingApi();

        }

        public void RefreshTokens()
        {
            Console.WriteLine("RefreshTokens");

            Console.WriteLine("Read existing Tokens...");

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

            Console.WriteLine("Refreshing Tokens...");
            XeroOAuth2Token newToken = (XeroOAuth2Token)_api.RefreshAccessTokenAsync(xeroToken).Result;

            accessToken = newToken.AccessToken;
            idToken = newToken.IdToken;
            refreshToken = newToken.RefreshToken;

            Console.WriteLine("WriteAccessToken...");
            WriteBase("AccessToken", accessToken);

            Console.WriteLine("WriteRefreshToken...");
            WriteBase("RefreshToken", refreshToken);

            Console.WriteLine("WriteIdToken...");
            WriteBase("IdToken", idToken);


        }

        public IEnumerable<Contact> GetContacts()
        {
            List<Contact> items = new List<Contact>();

            try
            {
                List<Contact> item = new List<Contact>();
                var items1 = accountingApi.GetContactsAsync(accessToken, tenantId).Result;
                items = items1._Contacts;
            }
            catch (Exception e)
            {
                string msg = e.Message;
                string innerMsg = e.InnerException == null ? null : e.InnerException.Message;
                LogHelper.LogDetail("Error in XeroService.GetContacts: " + msg);
                LogHelper.LogDetail("Error InnerException: " + innerMsg);
            }

            return items;

        }

        public bool CreateContact(Contact contact)
        {

            bool result = false;

            try
            {
                Contacts contacts = new Contacts();
                contacts._Contacts = new List<Contact> { contact };

                Contacts newContacts = accountingApi.CreateContactsAsync(accessToken, tenantId, contacts).Result;
                result = true;
            }
            catch (ApiException e)
            {
                string msg = e.Message;

                LogHelper.LogDetail(string.Format("Validation Error in XeroService.CreateContact: {0} ", msg));
            }
            catch (Exception e)
            {
                string msg = e.Message;
                LogHelper.LogDetail("Error in XeroService.CreateContact: " + msg);
            }

            return result;
        }

        public List<Invoice> GetInvoices()
        {
            DateTime firstInvoiceCheckDate = DateTime.Today.AddDays(-40);
            List<string> statusList = new List<string> { "DRAFT" };

            List<Invoice> items = new List<Invoice>();

            try
            {
                List<Invoice> item = new List<Invoice>();              
                var items1 = accountingApi.GetInvoicesAsync(accessToken, tenantId, firstInvoiceCheckDate, null,null,null,null,null,statusList).Result;
                items = items1._Invoices.ToList();
            }
            catch (Exception e)
            {
                string msg = e.Message;
                LogHelper.LogDetail("Error in XeroService.GetInvoices: " + msg);
            }

            return items;
        }

        //public string GetSalesAccount()
        //{
        //    string code = null;
        //    Account xeroSalesAccount = _api.Accounts
        //                        .Find()
        //                        .Where(w => w.Name == "Sales")
        //                        .FirstOrDefault();
        //    if (xeroSalesAccount != null)
        //    {
        //        code = xeroSalesAccount.Code;
        //    }
        //    return code;
        //}

        public Invoice AddOrUpdateInvoice(Invoice invoice, bool isExistingInvoice)
        {
            var retInvoice = isExistingInvoice ? UpdateInvoice(invoice) : CreateInvoice(invoice);
            return retInvoice;
        }

        public Invoice CreateInvoice(Invoice invoice)
        {
            LogHelper.LogDetail("CreateInvoice");

            var summarizeErrors = true;
            var unitdp = 4;
            var idempotencyKey = "KEY_VALUE";

            Invoice retInvoice = new Invoice();
            try
            {
                Invoices invoices = new Invoices();
                invoices._Invoices = new List<Invoice> { invoice };
                Invoices newInvoices = accountingApi.CreateInvoicesAsync(accessToken, tenantId, invoices, summarizeErrors).Result;
                //Invoices newInvoices = accountingApi.CreateInvoicesAsync(accessToken, tenantId, invoices, summarizeErrors, unitdp, idempotencyKey).Result;
                retInvoice = newInvoices._Invoices[0];
            }
            catch (ApiException e)
            {
                string msg = e.Message;
                LogHelper.LogDetail(string.Format("Validation Error in XeroService.CreateInvoice: {0}", msg));
            }
            catch (Exception e)
            {
                string msg = e.Message;
                LogHelper.LogDetail("Error in XeroService.CreateInvoice: " + msg);
            }
            return retInvoice;
        }

        public Invoice UpdateInvoice(Invoice invoice)
        {
            LogHelper.LogDetail("UpdateInvoice");

            Guid invoiceID = (Guid)invoice.InvoiceID;
            var unitdp = 4;
            var idempotencyKey = "KEY_VALUE";

            Invoice retInvoice = new Invoice();
            try
            {
                Invoices invoices = new Invoices();
                invoices._Invoices = new List<Invoice> { invoice };
                Invoices newInvoices = accountingApi.UpdateInvoiceAsync(accessToken, tenantId, invoiceID, invoices).Result;
              // Invoices newInvoices = accountingApi.UpdateInvoiceAsync(accessToken, tenantId, invoiceID, invoices, unitdp, idempotencyKey).Result;
                retInvoice = newInvoices._Invoices[0];
            }
            catch (ApiException e)
            {
                string msg = e.Message;
                LogHelper.LogDetail(string.Format("Validation Error in XeroService.UpdateInvoice: {0}", msg));
            }
            catch (Exception e)
            {
                string msg = e.Message;
                LogHelper.LogDetail("Error in XeroService.UpdateInvoice: " + msg);
            }
            return retInvoice;
        }


        public List<Item> GetItems()
        {
            List<Item> items = new List<Item>();

            try
            {
                List<Item> item = new List<Item>();
                var items1 = accountingApi.GetItemsAsync(accessToken, tenantId).Result;
                items = items1._Items.ToList();
            }
            catch (Exception e)
            {
                string msg = e.Message;
                string innerMsg = e.InnerException == null ? null : e.InnerException.Message;
                LogHelper.LogDetail("Error in XeroService.GetItems: " + msg);
                LogHelper.LogDetail("Error InnerException: " + innerMsg);
            }

            return items;

        }

        public bool CreateItem(Item item)
        {
            bool result = false;
            try
            {

                Items items = new Items();
                items._Items = new List<Item> { item };

                Items newItems = accountingApi.CreateItemsAsync(accessToken, tenantId, items).Result;

                //Item retItem = _api.Create(item);
                result = true;
            }
            catch (ApiException e)
            {
                string msg = e.Message;
                string innerMsg = e.InnerException == null ? null : e.InnerException.Message;
                LogHelper.LogDetail(string.Format("Validation Error in XeroService.CreateItem: {0}", msg));
                LogHelper.LogDetail("Error InnerException: " + innerMsg);
            }
            catch (Exception e)
            {
                string msg = e.Message;
                string innerMsg = e.InnerException == null ? null : e.InnerException.Message;
                LogHelper.LogDetail("Error in XeroService.CreateItem: " + msg);
                LogHelper.LogDetail("Error InnerException: " + innerMsg);
            }
            return result;
        }

        public void CreateItems(List<string> xeroItemNames)
        {
            try
            {
                Items items = new Items();

                List<Item> itms = new List<Item>();
                itms.Add(new Item() { Code = "DELIVERY", Name = "DELIVERY" });
                itms.Add(new Item() { Code = "SND-FINE", Name = "Fine Sand" });
                itms.Add(new Item() { Code = "SND-BIO", Name = "Bio-Retention" });
                itms.Add(new Item() { Code = "SND-BRCK", Name = "Brickies Sand" });
                itms.Add(new Item() { Code = "SND-PLST", Name = "Plasterers Sand" });
                itms.Add(new Item() { Code = "BAG-FINE", Name = "Bagged Fine Sand" });
                //var itemsWritten = _api.Create(items);
                items._Items = itms;


                items._Items = itms.Where(w => !xeroItemNames.Contains(w.Name.ToLower())).ToList();

                if (items._Items.Count > 0)
                {
                    Items newItems = accountingApi.CreateItemsAsync(accessToken, tenantId, items).Result;
                };



            }
            catch (Exception e)
            {
                string msg = e.Message;
                LogHelper.LogDetail("Error in CreateItems: " + msg);
            }
        }

        public void CreateContacts(List<string> existingXeroContactNames)
        {
            try
            {
                Contacts contacts = new Contacts();

                List<Contact> conts = new List<Contact>();
                conts.Add(new Contact() { Name = "Adbri Masonry"  });
                conts.Add(new Contact() { Name = "Centenary Landscaping Supplies" });
                conts.Add(new Contact() { Name = "Crown Bricklaying" });
                conts.Add(new Contact() { Name = "EMW Electrical Pty Ltd" });
                conts.Add(new Contact() { Name = "Hy-tec Swanbank" });
                conts.Add(new Contact() { Name = "Hy-tec Yatala" });
                conts.Add(new Contact() { Name = "John McLaughlin Pimpama Landscape Supplies" });
                conts.Add(new Contact() { Name = "MTC Gas Australia Pty Ltd" });
                conts.Add(new Contact() { Name = "National Masonry" });
                conts.Add(new Contact() { Name = "NuCon Concrete Logan" });
                conts.Add(new Contact() { Name = "NuCon Concrete Oxenford" });
                conts.Add(new Contact() { Name = "NuCon Concrete Yatala" });
                conts.Add(new Contact() { Name = "Pronto Concrete Ipswich" });
                conts.Add(new Contact() { Name = "Rock Around the Block." });
                conts.Add(new Contact() { Name = "Rocky Point Production" });
                conts.Add(new Contact() { Name = "Sapar Landscaping Supplies Pty Ltd" });
                conts.Add(new Contact() { Name = "Sunmix Concrete" });
                conts.Add(new Contact() { Name = "Western Landscape Supplies" });
                //var itemsContact = _api.Create(contacts);

                contacts._Contacts = conts.Where(w=> !existingXeroContactNames.Contains(w.Name.ToLower())).ToList();

                if (contacts._Contacts.Count>0)
                {
                    Contacts newContacts = accountingApi.CreateContactsAsync(accessToken, tenantId, contacts).Result;
                };


            }
            catch (Exception e)
            {
                string msg = e.Message;
                LogHelper.LogDetail("Error in CreateContacts: " + msg);
            }
        }

        //public void CreateTrackingCategories()
        //{
        //    try
        //    {
        //        List<TrackingCategory> items = new List<TrackingCategory>();
        //        items.Add(new TrackingCategory()
        //        {
        //            Name = "Product Type",
        //            Options = new List<Option> {
        //            new Option { Name = "Fine Sand" },
        //            new Option { Name = "Brickies Sand" },
        //            new Option { Name = "Bio-Retention" },
        //            new Option { Name = "Plasterers Sand" }
        //        }
        //        });
        //        //items.Add(new TrackingCategory() { Name = "Product Type", Option = "Fine Sand" });
        //        //items.Add(new TrackingCategory() { Name = "Product Type", Option = "Brickies Sand" });
        //        //items.Add(new TrackingCategory() { Name = "Product Type", Option = "Bio-Retention" });
        //        //items.Add(new TrackingCategory() { Name = "Product Type", Option = "Plasterers Sand" });
        //        var itemsWritten = _api.Create(items);
        //    }
        //    catch (Exception e)
        //    {
        //        string msg = e.Message;
        //        LogHelper.LogDetail("Error in CreateContacts: " + msg);
        //    }
        //}


        public static void WriteBase(string type, string content)
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

        public static string ReadFromFileBase(string type)
        {
            string result = null;

            string authFilesFolder = ConfigHelper.Setting("AuthFilesFolder");
            string authFileName = FileHelper.GetAuthFilename(type);
            string authFullFileName = Path.Combine(authFilesFolder, authFileName) + ".txt";

            if (File.Exists(authFullFileName))
            {
                result = FileHelper.ReadFromFile(authFullFileName);
            }

            return result;
        }

    }
}
