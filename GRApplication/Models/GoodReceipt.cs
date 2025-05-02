// File: Models/GoodReceipt.cs
using System;
using System.Collections.Generic;

namespace GRApplication.Models
{
    public enum GRStatus { Unsynced, Synced, Failed, Archived }

    public class GoodReceipt
    {
        public int Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public int VectorGRId { get; set; }
        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierName { get; set; } = string.Empty;
        public DateTime DocumentDate { get; set; }
        public DateTime DocumentDueDate { get; set; }

        // *** CHANGE THIS LINE ***
        public List<GoodReceiptItem> Items { get; set; } // Changed from List<string>

        public GRStatus UploadStatus { get; set; }
        public string ErrorDetails { get; set; } = string.Empty;

        public GoodReceipt()
        {
            // *** CHANGE THIS LINE ***
            Items = new List<GoodReceiptItem>(); // Initialize the new type
            UploadStatus = GRStatus.Unsynced;
        }
    }
}