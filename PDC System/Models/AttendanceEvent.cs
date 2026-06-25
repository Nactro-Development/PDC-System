namespace PDC_System.Models
{
    public class AttendanceEvent
    {
        public int SerialNo { get; set; }

        public string EmployeeId { get; set; }

        public DateTime EventTime { get; set; }

        public int VerifyMode { get; set; }

        public int Major { get; set; }

        public int Minor { get; set; }
    }
}