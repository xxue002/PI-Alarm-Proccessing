using Core.AlarmProcessor;
using Core.ConnectionManager;
using Core.FileReader;
using OSIsoft.AF.PI;
using Serilog;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Timers;

namespace Core.Service
{
    public class AlarmService : IAlarmService
    {
        private ILogger _logger;
        private IPIConnectionManager _piCM;
        //private IHistoryBackfiller _backfiller;
        private PIServer _SitePI;
        private bool _IsConnected;
        private AlarmReader _alarmReader;
        private IReader _reader;
        private IList<Foo> _csvlist;
        private Timer _aTimer;

        public AlarmService(IPIConnectionManager piCM, ILogger logger, AlarmReader alarmReader, IReader reader)
        {
            _piCM = piCM;
            _logger = logger;
            _alarmReader = alarmReader;
            _reader = reader;


        }

        public async Task Start()
        {
            _logger.Information("Alarm Service started successfully");
            _logger.Information($"{AppDomain.CurrentDomain.BaseDirectory}");
            (_IsConnected, _SitePI) = _piCM.Connect();

            // If cannot connecto to PI Data Collective, return to terminate console app
            if (!_IsConnected) return;
            else
            {
                // Retrieve list of Alarm PI Points from CSV
                _csvlist = _reader.readFile();

                _aTimer = new Timer(15000);
                _aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                _aTimer.Enabled = true;
            } 
        }

        public void Stop()
        {
            _aTimer.Dispose();
            if (_IsConnected) _piCM.Disconnect();
            _logger.Information("PI Alarm Service completed");
            _logger.Information("=============================================================================================");
            _logger.Information("=============================================================================================");
        }


        public void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            _alarmReader.RetrieveAlarm(_csvlist);
        }
    }
}
