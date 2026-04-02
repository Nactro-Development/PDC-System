using System;

namespace PDC_System
{
    public class JobCard
    {
        public DateTime JobCardDate { get; set; }
        public string? Customer_Name { get; set; }
        public string? Description { get; set; }
        public string? Paper_Size { get; set; }
        public int GSM { get; set; }
        public string? Paper_Type { get; set; }
        public int Quantity { get; set; }
        public int Printed { get; set; }
        public string? Duplex { get; set; }
        public string? Laminate { get; set; }
        public string? Special_Note { get; set; }
        public bool? IsSeen { get; set; }
        public string? Type { get; set; }

        public string? ScreenshotPath { get; set; }
        public string? JobNo { get; set; }
        public string? PlateQuantitiy { get; set; }


        // New fields
        public bool ExtraCheckBoxState { get; set; }
        public string? PlateCompanyName { get; set; }
        public string? DigitalConpanyName { get; set; }
        public string? selectedPlateName { get; set; }
      
        public int? ExtraQuantity { get; set; }

    }


}

