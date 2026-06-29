namespace PDC_System.Models
{
    public class User
    {
        public string Username { get; set; }

        public string FName { get; set; } // New property for the user's first name
        public string LName { get; set; } // New property for the user's last name

        public string PasswordHash { get; set; }

        public bool Dashbord { get; set; }
        public bool OderCheck { get; set; }
        public bool Jobcard { get; set; }
        public bool Customer { get; set; }
        public bool Outsourcing { get; set; }
        public bool Quotation { get; set; }
        public bool Employee { get; set; }
        public bool Attendance { get; set; }
        public bool Payroll { get; set; }
        public bool Paysheet { get; set; }
        public bool UserManager { get; set; }
        public bool Isadmin { get; set; }

    }
}
