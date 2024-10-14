using COSA_XeroInt.Coastal.Helpers;
using COSA_XeroInt.Coastal.Viewmodels;
using Microsoft.Extensions.Configuration;

namespace COSA_XeroInt.Coastal.Services
{
    public class ImportfileService
    {
        private string importFolder;
        private string archiveFolder;
        public ImportfileService()
        {
            importFolder = ConfigHelper.Setting("ImportFolder"); 
            archiveFolder = ConfigHelper.Setting("ArchiveFolder");
        }

        public string? GetImportFileName(string? manualImportFileName = null)
        {
            string? importFilename = null;
            if (manualImportFileName != null)
            {
                importFilename = manualImportFileName;
            }
            else
            {
                DateTime currentDate = DateTime.Today;
                importFilename = currentDate.ToString("yyyyMd") + ".csv";
            }
            string importFullFilename = Path.Combine(importFolder, importFilename);
            return importFullFilename;
        }

        public List<ImportVM> ImportFile(string importFullFilename)
        {
            List<ImportVM> rows = new List<ImportVM>();

            //-------------------------------------------------------------------
            //read in all rows
            StreamReader reader = new StreamReader(importFullFilename);
            int rowCtr = 0;

            try
            {
                while (!reader.EndOfStream)
                {
                    rowCtr++;
                    string? line = reader.ReadLine();
                    if (!String.IsNullOrWhiteSpace(line) && rowCtr > 1)
                    {

                        string[] values = line.Split(',');
                        int numberOfColumns = values.Length;
                        if (numberOfColumns >= 29)

                        {
                            //put names and column numbers in config???
                            string contactName = values[0].Trim();
                            string docketNumber = values[10].Trim();
                            string reference = values[11].Trim();
                            string invoiceDate = values[12].Trim();
                            string dueDate = values[13].Trim();
                            string itemCode = values[14].Trim();
                            string description = values[15].Trim();
                            string quantity = values[16].Trim();
                            string unit = values[17].Trim();
                            string discount = values[18].Trim();
                            string account = values[19].Trim();
                            string taxType = values[20].Trim();
                            string trackingName = values[21].Trim();
                            string trackingOption = values[22].Trim();

                            string invoicePeriod = values[27].Trim();
                            string purchaseOrder = values[28].Trim();
                            string oneInvoicePerPO = values[29].Trim();
                            string? fixedCartageRate = null;
                            if (numberOfColumns == 31)
                            {
                                fixedCartageRate = line.Substring(line.LastIndexOf(",") + 1).Trim();
                            }

                            ImportVM row = new ImportVM
                            {
                                ContactName = contactName,
                                DocketNumber = String.IsNullOrWhiteSpace(docketNumber) ? (int?)null : int.Parse(docketNumber),
                                Reference = reference,
                                DocketDate = String.IsNullOrWhiteSpace(invoiceDate) ? (DateTime?)null : DateTime.Parse(invoiceDate),
                                DueDate = String.IsNullOrWhiteSpace(dueDate) ? (DateTime?)null : DateTime.Parse(dueDate),
                                ItemCode = itemCode,
                                Description = description,
                                Quantity = String.IsNullOrWhiteSpace(quantity) ? (decimal?)null : decimal.Parse(quantity),
                                Unit = String.IsNullOrWhiteSpace(unit) ? (decimal?)null : decimal.Parse(unit),
                                Discount = String.IsNullOrWhiteSpace(discount) ? (decimal?)null : decimal.Parse(discount),
                                TaxType = taxType,
                                TrackingName = trackingName,
                                TrackingOption = trackingOption,
                                InvoicePeriod = invoicePeriod,
                                OneInvoicePerPO = oneInvoicePerPO,
                                FixedCartageRate = fixedCartageRate
                            };

                            rows.Add(row);

                        }
                    }
                }
            }
            catch (Exception e)
            {
                string msg = e.Message;
                LogHelper.LogDetail(string.Format("Error importing csv file {0} Error: {1}", importFullFilename,  msg));
            }

            reader.Close();

            return rows;

        }

        //archive file
        //move to archive folder
        public bool ArchiveImportFile(string importFullFilename)
        {
            bool result = false;
            string filenameOnly = Path.GetFileName(importFullFilename);
            string archiveFullFilename = Path.Combine(archiveFolder, filenameOnly);
            try
            {
                File.Move(importFullFilename, archiveFullFilename);
                result = true;
            }
            catch(Exception e)
            {
                string msg = e.Message;
                LogHelper.LogDetail(string.Format("Error archiving file {0} to {1} Error: {2}", importFullFilename, archiveFullFilename,msg));
            }
            return result;
        }

    }

}
