using COSA_XeroInt.Coastal.Viewmodels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COSA_XeroInt.Coastal.Services
{
    public static class LogicService
    {

        public static bool ShouldApproveInvoice(string invoicePeriod)
        {
            //monthly - if last day of month, approve all draft monthly
            //weekly - if Sat, approve all draft weekly
            //if transaction, approve all draft

            bool result = false;

            DateTime today = DateTime.Today;
            DayOfWeek dow = today.DayOfWeek;
            int dom = today.Day;
            int dim = DateTime.DaysInMonth(today.Year, today.Month);

            switch (invoicePeriod.ToLower())
            {
                case "daily":
                    result = true;
                    break;

                case "weekly":
                    if (dow == DayOfWeek.Saturday)
                    {
                        result = true;
                    }

                    break;

                case "monthly":
                    if (dom == dim)
                    {
                        result = true;
                    }

                    break;

                case "each transaction":
                    result = true;
                    break;

                default:
                    break;
            }

            return result;

        }

        public static DateTime GetNewInvoiceDate(string invoicePeriod)
        {

            DateTime today = DateTime.Today;
            DateTime invoiceDate;

            switch (invoicePeriod.ToLower())
            {
                case "monthly":
                    invoiceDate = new DateTime(today.Year, today.Month , 1).AddMonths(1).AddDays(-1);
                    break;

                default:
                    invoiceDate = today;
                    break;
            }

            return invoiceDate;

        }

    }

}
