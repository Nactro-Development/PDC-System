namespace PDC_System.HomeUi
{
    public partial class BirthdayInfo
    {
        public string Name { get; set; }
        public DateTime? BirthDate { get; set; }
        public int DaysLeft { get; set; } // Added to store the number of days remaining until the birthday
        public bool IsToday { get; set; }

        public string Designation { get; set; } // New
    }
}
