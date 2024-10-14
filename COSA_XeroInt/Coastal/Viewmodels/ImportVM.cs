using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COSA_XeroInt.Coastal.Viewmodels
{
    public class ImportVM
    {
        public Guid ContactId { get; set; } //needed?
        public string ContactName { get; set; }
        public int? DocketNumber { get; set; }
        public string Reference { get; set; }
        public DateTime? DocketDate { get; set; } //date or string to start??
        public DateTime? DueDate { get; set; }
        public string ItemCode { get; set; }
        public string Description { get; set; }
        public decimal? Quantity { get; set; }
        public decimal? Unit { get; set; }
        public decimal? Discount { get; set; }
        public string TaxType { get; set; } //GST On Income
        public string TrackingName { get; set; }
        public string TrackingOption { get; set; }
        public string InvoicePeriod { get; set; } //daily, weekly, monthly, transaction
        public string OneInvoicePerPO { get; set; } //true, false
        public string? FixedCartageRate { get; set; } //true, false

    }

    public class ContactInvoicePeriodVM
    {
        public Guid ContactId { get; set; }
        public string ContactName { get; set; }
        public string InvoicePeriod { get; set; } //weekly, monthly, transaction
    }


}
