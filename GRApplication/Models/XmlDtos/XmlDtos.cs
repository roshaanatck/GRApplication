using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace GRApplication.Models.XmlDtos
{
    [XmlRoot("ArrayOfPurchaseNote")]
    public class PurchaseNoteXmlArray
    {
        [XmlElement("PurchaseNote")]
        public List<PurchaseNoteXml> PurchaseNotes { get; set; } = new List<PurchaseNoteXml>();
    }

    public class PurchaseNoteXml
    {
        public string ReceiptId { get; set; } = string.Empty; // Keep as string for robust parsing
        public string OrderId { get; set; } = string.Empty;
        public string CardCode { get; set; } = string.Empty;
        public string CardName { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public DateTime PostDate { get; set; }
        public DateTime ReceiptDate { get; set; }
        public string DeliveryAddress { get; set; } = string.Empty;
        public decimal ReceiptTotal { get; set; }
        public decimal ReceiptVAT { get; set; }
        public string ReceiptRefNo { get; set; } = string.Empty;
        public int Status { get; set; }

        [XmlArray("Lines")]
        [XmlArrayItem("PurchaseNoteLine")]
        public List<PurchaseNoteLineXml> Lines { get; set; } = new List<PurchaseNoteLineXml>();
    }

    public class PurchaseNoteLineXml
    {
        public string ProductCode { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string BPCatNO { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal VATPaid { get; set; }
        public decimal AmountPaid { get; set; }
        public bool IsActive { get; set; }
    }
}