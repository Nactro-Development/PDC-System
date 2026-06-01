using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PDC_System.Services
{
    public static class EmployeeStorage
    {
        private static string FilePath =>
            Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Savers", "employees.dat");

        // LOAD
        public static List<Employee> Load()
        {
            if (!File.Exists(FilePath))
                return new List<Employee>();

            byte[] encrypted = File.ReadAllBytes(FilePath);

            byte[] decrypted = ProtectedData.Unprotect(
                encrypted,
                null,
                DataProtectionScope.CurrentUser);

            string json = Encoding.UTF8.GetString(decrypted);

            return JsonConvert.DeserializeObject<List<Employee>>(json)
                   ?? new List<Employee>();
        }

        // SAVE
        public static void Save(List<Employee> employees)
        {
            string json = JsonConvert.SerializeObject(employees, Formatting.Indented);

            byte[] data = Encoding.UTF8.GetBytes(json);

            byte[] encrypted = ProtectedData.Protect(
                data,
                null,
                DataProtectionScope.CurrentUser);

            File.WriteAllBytes(FilePath, encrypted);
        }
    }
}