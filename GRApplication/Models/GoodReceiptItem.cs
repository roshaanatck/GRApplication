// File: Models/GoodReceiptItem.cs
using System;

namespace GRApplication.Models
{
    public class GoodReceiptItem
    {
        // Add an ID for uniquely identifying the item for updates/deletes
        public int Id { get; set; }
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string UnitSize { get; set; } = string.Empty;
        public string CatalogueNumber { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal AmountPaid { get; set; }

        // Foreign key back to the parent GoodReceipt (optional but good practice)
        // public int GoodReceiptId { get; set; }
        // public GoodReceipt? GoodReceipt { get; set; } // Navigation property

        // Constructor (optional, properties have defaults or are value types)
        public GoodReceiptItem() { }
    }
}