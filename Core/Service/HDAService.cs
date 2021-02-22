using Core.AlarmProcessor;
using Core.ConnectionManager;
using Core.FileReader;
using OSIsoft.AF.PI;
using Serilog;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Core.Service
{
    public class HDAService : IHDAService
    {
        private ILogger _logger;
        private IPIConnectionManager _piCM;
        //private IHistoryBackfiller _backfiller;
        private PIServer _SitePI;
        private bool _IsConnected;
        private AlarmReader _alarmReader;
        private IReader _reader;
        private IList<Foo> _csvlist;

        public HDAService(IPIConnectionManager piCM, ILogger logger, AlarmReader alarmReader, IReader reader)
        {
            _piCM = piCM;
            _logger = logger;
            _alarmReader = alarmReader;
            _reader = reader;
        }

        public async Task Start()
        {
            _logger.Information("Alarm Service started successfully");
            (_IsConnected, _SitePI) = _piCM.Connect();

            // If cannot connecto to PI Data Collective, return to terminate console app
            if (!_IsConnected) return;
            else
            {
                // Retrieve list of Alarm PI Points from CSV
                _csvlist = _reader.readFile();
                while (true)
                {
                    _alarmReader.RetrieveAlarm(_csvlist);
                    await Task.Delay(5000);
                }
            } 
        }

        public void Stop()
        {
            if (_IsConnected) _piCM.Disconnect();
            _logger.Information("History Backfill Service completed");
            _logger.Information("=============================================================================================");
            _logger.Information("=============================================================================================");
        }


        //public void OnTimedEvent(object source, ElapsedEventArgs e)
        //{
        //    System.Timers.Timer aTimer;
        //    aTimer = new System.Timers.Timer();
        //    aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
        //    aTimer.Interval = 2000;
        //    aTimer.Enabled = true;
        //}
    }
}
