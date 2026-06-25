namespace PDC_System.Models
{
    public class DeviceInfo
    {
        public string Name { get; set; }

        public string IP { get; set; }

        public int Port { get; set; } = 80;

        public string Username { get; set; }

        public string Password { get; set; }

        public bool Enabled { get; set; }
    }
}