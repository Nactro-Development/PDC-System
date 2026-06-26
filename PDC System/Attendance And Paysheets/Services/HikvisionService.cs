using Newtonsoft.Json;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using PDC_System.Models;
using System.Collections.Generic;
using System.Windows;


namespace PDC_System.Services
{
    public class HikvisionService
    {
        private readonly HttpClient _client;

        public HikvisionService(string ip, string username, string password, int port = 80)
        {
            var handler = new HttpClientHandler
            {
                Credentials = new NetworkCredential(username, password)
            };

            _client = new HttpClient(handler);

            _client.BaseAddress = new Uri($"http://{ip}:{port}");
            _client.Timeout = TimeSpan.FromSeconds(15);
        }

        public async Task<bool> TestConnection()
        {
            try
            {
                var response = await _client.GetAsync("/ISAPI/System/deviceInfo");

                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task<string> GetDeviceInfo()
        {
            var response = await _client.GetAsync("/ISAPI/System/deviceInfo");

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetAttendanceEvents(
            int startPosition = 0,
            int maxResults = 100)
        {
            var body = new
            {
                AcsEventCond = new
                {
                    searchID = "all",
                    searchResultPosition = startPosition,
                    maxResults = maxResults,
                    major = 5,
                    minor = 38
                }
            };

           
            string json = JsonConvert.SerializeObject(body);

            var content =
                new StringContent(json, Encoding.UTF8, "application/json");

            var response =
                await _client.PostAsync(
                    "/ISAPI/AccessControl/AcsEvent?format=json",
                    content);

            response.EnsureSuccessStatusCode();




            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> GetUsers()
        {
            string json =
@"{
    ""UserInfoSearchCond"":
    {
        ""searchID"":""001"",
        ""searchResultPosition"":0,
        ""maxResults"":100
    }
}";

            var content =
                new StringContent(json,
                    Encoding.UTF8,
                    "application/json");

            var response =
                await _client.PostAsync(
                    "/ISAPI/AccessControl/UserInfo/Search?format=json",
                    content);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadAsStringAsync();
        }

        public async Task<string> CaptureFingerprint(int fingerNo = 1)
        {
            string xml =
$@"<?xml version=""1.0"" encoding=""UTF-8""?>
<CaptureFingerPrintCond xmlns=""http://www.isapi.org/ver20/XMLSchema"">
    <fingerNo>{fingerNo}</fingerNo>
</CaptureFingerPrintCond>";

            var content =
                new StringContent(xml,
                    Encoding.UTF8,
                    "application/xml");

            var response =
                await _client.PostAsync(
                    "/ISAPI/AccessControl/CaptureFingerPrint",
                    content);

            response.EnsureSuccessStatusCode();


            return await response.Content.ReadAsStringAsync();
        }


        public async Task<List<AttendanceEvent>> DownloadAttendanceEvents(
        int startPosition,
        int maxResults = 100)

        {
            var events = new List<AttendanceEvent>();


            while (true)
            {
                string json = await GetAttendanceEvents(startPosition, maxResults);

                JObject root = JObject.Parse(json);

                var acs = root["AcsEvent"];

                if (acs == null)
                    break;

                string status = acs["responseStatusStrg"]?.ToString() ?? "";

                int count = acs["numOfMatches"]?.Value<int>() ?? 0;

                var infoList = acs["InfoList"];



                if (infoList == null || !infoList.Any())
                    break;

                foreach (var item in infoList)
                {
                    events.Add(new AttendanceEvent
                    {
                        SerialNo = item["serialNo"]?.Value<int>() ?? 0,
                        EmployeeId = item["employeeNoString"]?.ToString()?.Trim(),
                        EventTime = DateTime.Parse(item["time"]?.ToString() ?? DateTime.MinValue.ToString()),
                        VerifyMode = item["verifyNo"]?.Value<int>() ?? 0,
                        Major = item["major"]?.Value<int>() ?? 0,
                        Minor = item["minor"]?.Value<int>() ?? 0
                    });
                }

               

                // Next page
                startPosition += count;

                // No more records
                if (status != "MORE")
                    break;
            }

            return events;
        }



        public async Task<int> GetTotalMatches()
        {
            string json = await GetAttendanceEvents(0, 1);

            JObject root = JObject.Parse(json);

            return root["AcsEvent"]?["totalMatches"]?.Value<int>() ?? 0;
        }




    }
}