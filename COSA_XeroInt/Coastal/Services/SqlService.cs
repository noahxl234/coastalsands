using COSA_XeroInt.Coastal.Helpers;
using COSA_XeroInt.Coastal.Viewmodels;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xero.NetStandard.OAuth2.Model.Accounting;

namespace COSA_XeroInt.Coastal.Services
{


    public class SqlService
    {

        private string connString;
        public SqlService()
        {
            connString = ConfigHelper.Setting("SqlConnectionString");
        }

        public List<SqlAccountVM> GetSqlAccounts()
        {
            LogHelper.LogDetail("GetSqlAccounts...");
            List<SqlAccountVM> _list= new List<SqlAccountVM>();
            SqlConnection conn = new SqlConnection(connString);
            try
            {
                conn.Open();
            }
            catch (Exception e)
            {
                string msg = e.Message;
                LogHelper.LogDetail("Error in SqlConnection: " + msg);
            }

            SqlCommand cmd = new SqlCommand();
            string commandText = "SELECT [Account_Name], [Invoice_Terms] FROM dbo.Account";
            cmd.CommandText = commandText;
            cmd.Connection = conn;

            SqlDataReader dr = null;
            try
            {
                dr = cmd.ExecuteReader();
            }
            catch (Exception e)
            {
                string msg = e.Message;
                LogHelper.LogDetail("Error in SqlDataReader: " + msg);
            }

            while (dr.Read())
            {
                string accountName = dr["Account_Name"].ToString();
                string invoiceTerms = dr["Invoice_Terms"].ToString();

                //LogHelper.LogDetail("accountName:" + accountName);

                SqlAccountVM vm = new SqlAccountVM
                {
                    AccountName = accountName,
                    InvoiceTerms = invoiceTerms
                };
                _list.Add(vm);
            }

            //cleanup
            dr.Close();
            cmd.Dispose();
            conn.Close();

            return _list;
        }

        public List<SqlAccountVM> GetSqlAccounts_TEST()
        {
            List<SqlAccountVM> _list = new List<SqlAccountVM>();

            _list.Add(new SqlAccountVM() { AccountName = "Adbri Masonry", InvoiceTerms= "Monthly" });
            _list.Add(new SqlAccountVM() { AccountName = "Centenary Landscaping Supplies", InvoiceTerms="Monthly" });
            _list.Add(new SqlAccountVM() { AccountName = "Crown Bricklaying", InvoiceTerms = "Monthly" });
            _list.Add(new SqlAccountVM() { AccountName = "EMW Electrical Pty Ltd", InvoiceTerms = "Monthly" });
            _list.Add(new SqlAccountVM() { AccountName = "Hy-tec Swanbank", InvoiceTerms = "Monthly" });
            _list.Add(new SqlAccountVM() { AccountName = "Hy-tec Yatala", InvoiceTerms = "Monthly" });
            _list.Add(new SqlAccountVM() { AccountName = "John McLaughlin Pimpama Landscape Supplies", InvoiceTerms = "Monthly" });
            _list.Add(new SqlAccountVM() { AccountName = "MTC Gas Australia Pty Ltd", InvoiceTerms = "Monthly" });
            _list.Add(new SqlAccountVM() { AccountName = "National Masonry", InvoiceTerms = "Monthly" });
            _list.Add(new SqlAccountVM() { AccountName = "NuCon Concrete Logan", InvoiceTerms = "Monthly" });
            _list.Add(new SqlAccountVM() { AccountName = "NuCon Concrete Oxenford", InvoiceTerms = "Monthly" });
            _list.Add(new SqlAccountVM() { AccountName = "NuCon Concrete Yatala", InvoiceTerms = "Monthly" });
            _list.Add(new SqlAccountVM() { AccountName = "Pronto Concrete Ipswich", InvoiceTerms = "Monthly" });
            _list.Add(new SqlAccountVM() { AccountName = "Rock Around the Block.", InvoiceTerms = "Monthly" });
            _list.Add(new SqlAccountVM() { AccountName = "Rocky Point Production", InvoiceTerms = "Monthly" });
            _list.Add(new SqlAccountVM() { AccountName = "Sapar Landscaping Supplies Pty Ltd", InvoiceTerms = "Monthly" });
            _list.Add(new SqlAccountVM() { AccountName = "Sunmix Concrete", InvoiceTerms = "Monthly" });
            _list.Add(new SqlAccountVM() { AccountName = "Western Landscape Supplies" , InvoiceTerms="Monthly"});



            return _list;
        }


    }

}
