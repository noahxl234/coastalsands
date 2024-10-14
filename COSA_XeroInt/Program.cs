// See https://aka.ms/new-console-template for more information

using COSA_XeroInt.Coastal.Helpers;
using COSA_XeroInt.Coastal.Services;
using Microsoft.Extensions.Configuration;
using Xero.NetStandard.OAuth2.Model.Accounting;

//----------------------------------------------------------
//config
var builder = new ConfigurationBuilder();

builder.SetBasePath(Directory.GetCurrentDirectory());
builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
builder.AddCommandLine(args);
IConfiguration config = builder.Build();

ConfigHelper.Initialise(config);

//----------------------------------------------------------
//services
XeroService xeroService = new XeroService();
SqlService sqlService = new SqlService();
ImportfileService importfileService = new ImportfileService();

//first we need to refresh tokens as they only last 30 mins
//but will need to reauthorise every 60 days

LogHelper.LogSummary("======================================================================================");
LogHelper.LogSummary("RefreshTokens");

xeroService.RefreshTokens();

//----------------------------------------------------------
//TESTING ONLY

//var xeroContactsAllTest = xeroService.GetContacts();
//var xeroItemsAllTest = xeroService.GetItems();
//var xeroInvoicesAllTest = xeroService.GetInvoices();

//return 1;

//Contact contact = new Contact(); 
//xeroService.CreateContact(contact);

//Item item = new Item();
//xeroService.CreateItem(item);

//Invoice invoice = new Invoice();
//xeroService.AddOrUpdateInvoice(invoice,true);
//----------------------------------------------------------

LogHelper.LogDetail("======================================================================================");
LogHelper.LogSummary("======================================================================================");

//----------------------------------------------------------------
//initialisation
//var _api = new Xero.Core(false);

int countSqlAccounts;
int countImportRows;
int countImportContacts;
int countImportContactInvoiceLines;
decimal sumContactImportAmount = 0;
decimal sumImportAmount = 0;

int countDraftXeroInvoicesWithNoImportsApproved = 0;
decimal sumDraftXeroInvoicesWithNoImportsApprovedAmount = 0;


int countXeroContacts;
int countXeroInvoices;

int ctrClient = 0;
string? invoicePeriod = null;

string? manualImportFileName = null;


//----------------------------------------------------------------
//xero call find all contacts
LogHelper.LogDetail("Retrieve Xero Contacts...");
var xeroContactsAll = xeroService.GetContacts();
countXeroContacts = xeroContactsAll.Count();
LogHelper.LogDetail("Xero contacts = " + countXeroContacts.ToString());

var xeroContactNames = xeroContactsAll.Select(s => s.Name.ToLower()).ToList();

//----------------------------------------------------------------
//xero call find all items
LogHelper.LogDetail("Retrieve Xero Items...");
var xeroItemsAll = xeroService.GetItems();
int countXeroItemss = xeroItemsAll.Count();
LogHelper.LogDetail("Xero items = " + countXeroItemss.ToString());
var xeroItemNames = xeroItemsAll.Select(s => s.Name?.ToLower()).ToList();

//----------------------------------------------------------------
//arguments

if (args.Length > 0)
{
    LogHelper.LogDetail("args[0]= " + args[0]);
    if (args[0].ToLower() == "import")
    {
        xeroService.CreateContacts(xeroContactNames);
        xeroService.CreateItems(xeroItemNames);
        //xeroService.CreateTrackingCategories();
    }
    else if (args[0].ToLower() == "yesterday")
    {
        DateTime yesterday = DateTime.Today.AddDays(-1);
        manualImportFileName = yesterday.ToString("yyyyMd") + ".csv";
    }
    else
    {
        manualImportFileName = args[0];
    }

}


//TESTING ONLY
//xeroService.CreateContacts(xeroContactNames);
//xeroService.CreateItems(xeroItemNames);
//manualImportFileName = "2024320.csv";

LogHelper.LogDetail("manualImportFileName = " + manualImportFileName);

//if no parameters passed then importCsvFilename will be current date
string? importCsvFilename = importfileService.GetImportFileName(manualImportFileName);
LogHelper.LogDetail("importCsvFilename = " + importCsvFilename);


//----------------------------------------------------------------
//have to get accounts and invoice terms from sql database because not stored in Xero????
LogHelper.LogDetail("Import account data from sql db...");

var sqlAccounts = sqlService.GetSqlAccounts();
countSqlAccounts = sqlAccounts.Count();
LogHelper.LogDetail("Sql accounts = " + countSqlAccounts);
if (countSqlAccounts == 0)
{
    LogHelper.LogDetail("Zero SQL Accounts found so end ");
    LogHelper.LogSummary("Zero SQL Accounts found so end ");
    return -1;
}


//----------------------------------------------------------------
//get all invoices with draft statuses
LogHelper.LogDetail("Retrieve Xero Invoices...");
var xeroInvoicesAll = xeroService.GetInvoices();
var xeroInvoicesIweigh = xeroInvoicesAll
                        //.Where(w => w.Reference.Contains("iWeigh")) //iWeigh
                        .ToList();
countXeroInvoices = xeroInvoicesIweigh.Count();
LogHelper.LogDetail("Xero Draft Invoices = " + countXeroInvoices.ToString());

//----------------------------------------------------------------
//Open csv in specified folder
//read in all lines and convert to List<ImportVM>

LogHelper.LogDetail("Import data from csv file...");

if (!System.IO.File.Exists(importCsvFilename))
{
    LogHelper.LogDetail("Import file not found " + importCsvFilename);
    LogHelper.LogSummary("Import file not found" + importCsvFilename);
    return -1;
}
var importRows = importfileService.ImportFile(importCsvFilename);
countImportRows = importRows.Count();
LogHelper.LogDetail("Import rows = " + countImportRows);
if (countImportRows == 0)
{
    LogHelper.LogDetail("Zero rows imported so end ");
    LogHelper.LogSummary("Zero rows imported so end ");
    return -1;
}

//----------------------------------------------------------------
//get distinct list of clients from importvm
var importContacts = importRows
                    .Select(s => new
                    {
                        ContactName = s.ContactName.Replace("\r\n","").Trim(),
                        OneInvoicePerPO=s.OneInvoicePerPO,
                        Reference = s.OneInvoicePerPO.ToLower() == "false" ? "" : s.Reference
                    })
                    .Distinct();
countImportContacts = importContacts.Count();
LogHelper.LogDetail("Import contacts = " + countImportContacts);

//----------------------------------------------------------------
//get distinct list of items from importvm
var importItems = importRows
                    .Where(s => s.ItemCode != null && s.ItemCode != "") //need to allow for nulls
                    .Select(s => s.ItemCode)
                    .Distinct();
int countImportItems = importItems.Count();
LogHelper.LogDetail("Import items = " + countImportItems);

//----------------------------------------------------------------
//add contacts not in Xero
var contactsNotInXero = importContacts
                        .Where(w => !xeroContactNames.Contains(w.ContactName.ToLower()))
                        .Select(s => s.ContactName)
                        .Distinct()
                        .ToList();
if (contactsNotInXero.Count() > 0)
{
    LogHelper.LogDetail("Add Contacts not in Xero");

    foreach (var contactName in contactsNotInXero)
    {

        LogHelper.LogDetail("Try to add contact to xero:" + contactName);

        Contact contact = new Contact()
        {
            Name = contactName
        };

        bool isAddContact = xeroService.CreateContact(contact);

        if (!isAddContact)
        {
            LogHelper.LogDetail("Contact could not be created: " + contactName);
            LogHelper.LogSummary("Contact could not be created: " + contactName);
            return -1;
        }


        LogHelper.LogDetail("Contact added " + contactName);
    }

    //need to get again as have added some
    xeroContactsAll = xeroService.GetContacts();
    countXeroContacts = xeroContactsAll.Count();
    LogHelper.LogDetail("Xero contacts after adding = " + countXeroContacts.ToString());

}

//----------------------------------------------------------------
//add items not in Xero
var xeroItemCodes = xeroItemsAll.Select(s => s.Code).ToList();
var itemsNotInXero = importItems.Except(xeroItemCodes);

if (itemsNotInXero.Count() > 0)
{
    LogHelper.LogDetail("Add Contacts not in Xero");

    foreach (var itemCode in itemsNotInXero)
    {
        LogHelper.LogDetail("Try to add item to xero:" + itemCode);
        string? itemName = xeroItemsAll.Where(w => w.Code == itemCode).Select(s => s.Name).FirstOrDefault();
        Item item = new Item
        {
            Code = itemCode,
            Name = itemName
        };

        bool isAddItem = xeroService.CreateItem(item);

        if (!isAddItem)
        {
            LogHelper.LogDetail("Item could not be created: " + itemName);
            LogHelper.LogSummary("Item could not be created: " + itemName);
            return -1;
        }


        LogHelper.LogDetail("Item added " + itemName);
    }

    //need to get again as have added some
    xeroItemsAll = xeroService.GetItems();
    countXeroItemss = xeroItemsAll.Count();

    LogHelper.LogDetail("Xero items after adding = " + countXeroItemss.ToString());

}


//----------------------------------------------------------------
// get list of item codes and item account codes

var itemAccounts = xeroItemsAll.Select(s => new
{
    Code = s.Code,
    AccountCode = s.SalesDetails == null ? null : s.SalesDetails.AccountCode
})
.ToList();

//----------------------------------------------------------------
//approve any invoices missed (should have already been approved 
//or approve today with none in list)
LogHelper.LogDetail("Approve invoices if required");

//get contacts in sql db not in import csv
var sqlAccountNames = sqlAccounts.Select(s => s.AccountName.ToLower()).ToList();
LogHelper.LogDetail("Count SQL account Names=" + sqlAccountNames.Count);

var contactsNotInImport = sqlAccountNames
                        .Where(w => !importContacts.Select(s => s.ContactName).Contains(w.ToLower()))
                        .ToList();
LogHelper.LogDetail("Count Contacts Not in Import=" + contactsNotInImport.Count);

//check if need to approve invoice
var existingDraftInvoicesWithNoImport = (from inv in xeroInvoicesIweigh.Where(w => w.Status == Invoice.StatusEnum.DRAFT)
                                         join cont in sqlAccounts.Where(w => contactsNotInImport.Contains(w.AccountName.ToLower()))
                                         on inv.Contact.Name equals cont.AccountName
                                         select new
                                         {
                                             Name = cont.AccountName,
                                             InvoicePeriod = cont.InvoiceTerms,
                                             Invoice = inv
                                         }
                                        ).ToList();

//approve if required
LogHelper.LogDetail("Count Existing Draft Invoices With No Imports Today=" + existingDraftInvoicesWithNoImport.Count);
foreach (var item in existingDraftInvoicesWithNoImport)
{
    invoicePeriod = item.InvoicePeriod;
    string contactName = item.Name;
    LogHelper.LogDetail("Check for " + contactName);

    bool shouldApprove = LogicService.ShouldApproveInvoice(invoicePeriod);

    if (shouldApprove)
    {
        LogHelper.LogDetail("Approve existing invoice for " + contactName);

        Invoice xeroInvoice = item.Invoice;
        List<LineItem> lineItems = xeroInvoice.LineItems;

        countDraftXeroInvoicesWithNoImportsApproved++;

        LogHelper.LogDetail("Get ApprovedAmount for " + contactName);
        var approvemountItems = lineItems.Select(s => (decimal)s.Quantity * (decimal)s.UnitAmount).ToList();
        decimal approvemount = approvemountItems==null ? 0 : approvemountItems.Sum() ;
        sumDraftXeroInvoicesWithNoImportsApprovedAmount = sumDraftXeroInvoicesWithNoImportsApprovedAmount + approvemount;

        LogHelper.LogDetail("AddOrUpdateInvoice existing invoice for " + contactName);
        xeroInvoice.Status = Invoice.StatusEnum.AUTHORISED;
        var retInvoice = xeroService.AddOrUpdateInvoice(xeroInvoice, true);
    }
    else
    {
        LogHelper.LogDetail("No Approval required for " + contactName);

    }
}

//----------------------------------------------------------------
//loop through for each client and 
LogHelper.LogDetail("Loop through csv distinct contacts...");

foreach (var importContact in importContacts)
{
    ctrClient++;

    string contactName = importContact.ContactName;
    string oneInvoicePerPO = importContact.OneInvoicePerPO;
    string contactRef = importContact.Reference;
    bool isOneInvoicePerPO = oneInvoicePerPO.ToLower() == "true";

    LogHelper.LogDetail(string.Format("----------- Contact {0} : {1} : {2}", ctrClient, contactName, contactRef));

    Contact contact = xeroContactsAll.Where(w => w.Name.ToLower() == contactName.ToLower()).FirstOrDefault();
    LogHelper.LogDetail("Contact guid = " + contact.ContactID);

    //create invoice vm
    Invoice xeroInvoice = new Invoice()
    {
        Type = Invoice.TypeEnum.ACCREC,
        Contact = contact,
        Status = Invoice.StatusEnum.DRAFT,
        LineAmountTypes = LineAmountTypes.Exclusive,
        LineItems = new List<LineItem>(),
        Reference = isOneInvoicePerPO ? contactRef : ""
    };

    //get rows from csv import for this contact
    var linesCSV = importRows
                        .Where(w => w.ContactName.ToLower() == contactName.ToLower())
                        .Where(w => (w.Reference.ToLower() == contactRef.ToLower() && isOneInvoicePerPO) || !isOneInvoicePerPO)
                        .Where(w => w.OneInvoicePerPO.ToLower() == oneInvoicePerPO.ToLower())
                        .ToList();

    invoicePeriod = linesCSV.Select(s => s.InvoicePeriod).FirstOrDefault()?.ToLower();
    LogHelper.LogDetail(string.Format("InvoicePeriod: {0}  ", invoicePeriod));
    DateTime? invoiceDate = linesCSV.Select(s => s.DocketDate).FirstOrDefault();
    DateTime? dueDate = linesCSV.Select(s => s.DueDate).FirstOrDefault();

    bool isExistingInvoice = false;

    //check for matching in xero
    
    var existingInvoice = xeroInvoicesIweigh
                        //should this just be draft??
                        .Where(w => w.Status == Invoice.StatusEnum.DRAFT)
                        .Where(w => w.Contact.Name.ToLower() == contactName.ToLower())     //always must match
                        .Where(w => (isOneInvoicePerPO && w.Reference.ToLower() == contactRef.ToLower()) || !isOneInvoicePerPO && w.Reference == "")  //must match if oneinvoiceperpo = true
                        .FirstOrDefault();

    countImportContactInvoiceLines = linesCSV.Count();
    LogHelper.LogDetail("Contact line items = " + countImportContactInvoiceLines);

    List<LineItem> lineItemsCSV = (from s in linesCSV
                                   join i in itemAccounts on s.ItemCode equals i.Code into tempItems
                                   from i in tempItems.DefaultIfEmpty()

                                   select new LineItem
                                   {
                                       Description = s.Description + " " + " - " + s.DocketNumber + ((!isOneInvoicePerPO && s.Reference != "") ? " [" + s.Reference + "]" : ""),
                                       Quantity = s.FixedCartageRate == "TRUE" ? 1 : s.Quantity,
                                       UnitAmount = s.Unit,
                                       AccountCode = i == null ? null : i.AccountCode, //will fail if item has no account code in xero
                                       ItemCode = s.ItemCode,
                                       TaxType = "OUTPUT", // XeroApi.Core.Model.Types.ReportTaxType.Output.ToString(),
                                       LineAmount = (decimal?)((s.FixedCartageRate == "TRUE" ? 1 : s.Quantity) * s.Unit),
                                       DiscountRate = s.Discount,
                                       Tracking = new List<LineItemTracking> { new LineItemTracking { Name = s.TrackingName, Option = s.TrackingOption } }
                                   })
                                .ToList();

    sumContactImportAmount = lineItemsCSV.Select(s => (decimal)s.Quantity * (decimal)s.UnitAmount).Sum();

    //add new lineitems
    List<LineItem> newInvoiceLineItems = new List<LineItem>();

    //if existing invoice exists, check for existing matching ine items
    //and get lineitemid so updates instead of creates
    if (existingInvoice != null && invoicePeriod != "daily")
    {
        LogHelper.LogDetail(string.Format("Existing Invoice No = {0} {1}" , existingInvoice.InvoiceNumber, existingInvoice.InvoiceID));

        List<LineItem> existingInvoiceLineItems = existingInvoice.LineItems;

        if (existingInvoiceLineItems != null)
        {
            //add line items existing in xero but not in new csv import file
            // DO NOT INCLUDE LINEITEMID OTHERWISE WILL GO OUT OF ORDER!!!!
            List<LineItem> existingNotNew = (from e in existingInvoiceLineItems
                                             join n in lineItemsCSV on e.Description.ToLower() equals n.Description.ToLower() into tempNew
                                             from n in tempNew.DefaultIfEmpty()
                                             where n == null
                                             select new LineItem
                                             {
                                                 Description = e.Description,
                                                 Quantity = e.Quantity,
                                                 UnitAmount = e.UnitAmount,
                                                 AccountCode = e.AccountCode,
                                                 ItemCode = e.ItemCode,
                                                 TaxType = e.TaxType,
                                                 LineAmount = e.LineAmount,
                                                 DiscountRate = e.DiscountRate,
                                                 Tracking = e.Tracking
                                             })
                                            .ToList();
            xeroInvoice.LineItems.AddRange(existingNotNew);
        }

        xeroInvoice.InvoiceID = existingInvoice.InvoiceID;
        xeroInvoice.InvoiceNumber = existingInvoice.InvoiceNumber;
        xeroInvoice.Date = existingInvoice.Date;
        xeroInvoice.DueDate = existingInvoice.DueDate;
        isExistingInvoice = true;

    }
    else //new invoice so need dates
    {
        invoiceDate = LogicService.GetNewInvoiceDate(invoicePeriod);
        LogHelper.LogDetail(string.Format("New Invoice Date = {0}", invoiceDate));
        xeroInvoice.Date = invoiceDate;
        xeroInvoice.DueDate = dueDate;
    }

    //set / change status if required
    if (LogicService.ShouldApproveInvoice(invoicePeriod))
    {
        LogHelper.LogDetail("Authorise Invoice.......");
        xeroInvoice.Status = Invoice.StatusEnum.AUTHORISED;
    }

    //add new lineItems
    xeroInvoice.LineItems.AddRange(lineItemsCSV);

    LogHelper.LogDetail("Invoice vm created");

    //best to add one at a time for errors, logging, in case one fails
    LogHelper.LogDetail("Write invoice to Xero.......");
    try
    {
        LogHelper.LogDetail(isExistingInvoice ? "Try to update invoice" : "Try to add Invoice");
        var retInvoice = xeroService.AddOrUpdateInvoice(xeroInvoice, isExistingInvoice);

        string invoiceNumber = retInvoice.InvoiceNumber;

        sumImportAmount = sumImportAmount + sumContactImportAmount;

        //var warnings = itemsInvoice.Warnings;
        //foreach(var item in warnings)
        //{
        //    LogHelper.LogDetail("  Warning on write invoice" + item.Message);
        //}
        //var errors = itemsInvoice.Errors;
        //foreach (var item in errors)
        //{
        //    LogHelper.LogDetail("  Error on write invoice" + item.Message );
        //}

        LogHelper.LogDetail(isExistingInvoice ? "Invoice updated" : "Invoice added");
        LogHelper.LogSummary(string.Format("Contact:{0} Period:{1} Invoice No:{2} Line Items:{3} Amount:{4}", contactName, invoicePeriod, invoiceNumber, countImportContactInvoiceLines, sumContactImportAmount.ToString("C")));
    }
    catch (Exception e)
    {
        string msg = e.Message;
        string? msgInner = e.InnerException?.Message;
        string? trace = e.StackTrace;
        LogHelper.LogDetail(string.Format("Error adding invoice {0} {1} {2}", msg, msgInner, trace));
    }

} //contactName in invoiceContactNames

LogHelper.LogDetail("Archive csv file");
importfileService.ArchiveImportFile(importCsvFilename);

//----------------------------------------------------------------
//done
LogHelper.LogDetail("Run completed");
LogHelper.LogSummary(string.Format("Count Invoices Approved with Contact not in Import:{0} Sum Amount:{1}", countDraftXeroInvoicesWithNoImportsApproved, sumDraftXeroInvoicesWithNoImportsApprovedAmount.ToString("C")));
LogHelper.LogSummary(string.Format("Count Contacts:{0} Sum Amount:{1}", ctrClient, sumImportAmount.ToString("C")));

return 1;