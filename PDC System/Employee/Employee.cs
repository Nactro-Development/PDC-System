namespace PDC_System
{
    public class Employee
    {
        // The name of the employee (required field)
        public  string Name { get; set; }

        // The employee's unique ID (required field)
        public  string ID { get; set; }

        public string FullName => $"{Name} - {ID}";

        // The employee's basic salary (required field)
        public decimal BSalary { get; set; }


        // The employee's basic salary (required field)
        public decimal Salary { get; set; }

        // Overtime hours worked
        public decimal OT { get; set; }

        // Double overtime hours worked
        public decimal DOT { get; set; }

        // The department the employee belongs to
        public string? Department { get; set; }

        // The job role or designation of the employee
        public string? jobrole { get; set; }

        // First line of the employee's address
        public string? Address1 { get; set; }

        // Second line of the employee's address
        public string? Address2 { get; set; }

        // The city where the employee resides
        public string? City { get; set; }

        // The province or state where the employee resides
        public string? Province { get; set; }

        // Primary contact number of the employee
        public string? Contactn1 { get; set; }

        // Secondary contact number of the employee
        public string? Contactn2 { get; set; }

        // National ID number of the employee
        public string? NID { get; set; }

        // Number of days the employee was absent
        public decimal ABSENT { get; set; }

        // The original path where employee-related files are stored
        public string? OriginalPath { get; set; }

        // The location where employee files are saved
        public string? SavedLocation { get; set; }

        // The birthdate of the employee
        public DateTime? Birthday { get; set; }



        public string   EmployeeCheckin { get; set; }

        public string   EmployeeCheckout { get; set; }




        public string EmployeeId { get; set; }
        public string EmployeeEmail { get; set; }
        
        public decimal OvertimeAmount { get; set; }
        public decimal DoubleOvertimeAmount { get; set; }
       
        public decimal Nopay { get; set; }
        public decimal AbesentAmount { get; set; }


        public TimeSpan CheckIn { get; set; }
        public TimeSpan CheckOut { get; set; }
        public TimeSpan SaturdayCheckIn { get; set; }
        public TimeSpan SaturdayCheckOut { get; set; }

        public bool CountOffDays { get; set; }
        public bool Monday { get; set; }
        public bool Tuesday { get; set; }
        public bool Wednesday { get; set; }
        public bool Thursday { get; set; }
        public bool Friday { get; set; }
        public bool Saturday { get; set; }
        public bool Sunday { get; set; }


        public bool fingerprint1 { get; set; } 
        public bool fingerprint2 { get; set; } 


        public DateTime? begintimee { get; set; }
        public DateTime? endintimee { get; set; }


        // Removed ValidFrom and ValidTo properties
    }
}
