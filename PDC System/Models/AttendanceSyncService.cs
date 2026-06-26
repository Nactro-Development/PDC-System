using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PDC_System.Services
{
    public class AttendanceSyncService
    {
        private bool _isSyncing = false;

        private readonly HikvisionService _hikvision;
        private readonly AttendanceDatabase _database;
        public static event Action AttendanceUpdated;
        private string _checkstatus;


        private Timer? _timer;

        public AttendanceSyncService(
            HikvisionService hikvision,
            AttendanceDatabase database)
        {
            _hikvision = hikvision;
            _database = database;
        }

        public void Start()
        {
            _timer = new Timer(async _ =>
            {
                await Sync();

            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(30));
        }

        public void Stop()
        {
            _timer?.Dispose();
        }


        private async Task Sync()
        {


            if (_isSyncing)
                return;

            _isSyncing = true;

            try
            {
                int localCount = _database.GetAttendanceEventCount();

                int totalMatches = await _hikvision.GetTotalMatches();


                MessageBox.Show($"Local = {localCount}\nDevice = {totalMatches}");

                if (localCount >= totalMatches)
                    return;

                int missing = totalMatches - localCount;

                var events = await _hikvision.DownloadAttendanceEvents(
                    localCount,
                    missing);


                foreach (var item in events)
                {
                    _database.SaveAttendanceEvent(item);
                }



                Application.Current.Dispatcher.Invoke(() =>
                {
                    var home = Application.Current.Windows
                                 .OfType<Home>()
                                 .FirstOrDefault();

                    home?.updateattendacecheck();
                });






            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            finally
            {
                _isSyncing = false;
            }
        }

    }
}