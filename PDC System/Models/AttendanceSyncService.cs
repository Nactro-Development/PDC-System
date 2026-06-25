using System;
using System.Threading;
using System.Threading.Tasks;

namespace PDC_System.Services
{
    public class AttendanceSyncService
    {
        private bool _isSyncing = false;

        private readonly HikvisionService _hikvision;
        private readonly AttendanceDatabase _database;

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

            }, null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
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
                var events = await _hikvision.DownloadAttendanceEvents();

                foreach (var item in events)
                {
                    _database.SaveAttendanceEvent(item);
                }
            }
            catch
            {
            }
            finally
            {
                _isSyncing = false;
            }
        }

    }
}