using Core.AlarmProcessor;
using Core.ConnectionManager;
using Core.FileReader;
using Core.Settings;
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
        private static IList<Foo> _csvlist;
        private static Timer _aTimer;

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

                _aTimer = new Timer(AppSettings.Freq*1000);
                _aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
                _aTimer.Enabled = true;
                //_aTimer.AutoReset = false;
                //_aTimer.Interval = GetInterval();
                //_aTimer.Start();
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


        public async void OnTimedEvent(object source, ElapsedEventArgs e)
        {
            //_aTimer.Interval = GetInterval();
            //_aTimer.Start();
            await _alarmReader.RetrieveAlarmAsync(_csvlist, e.SignalTime);
        }


        //private double GetInterval()
        //{
        //    DateTime now = DateTime.Now;
        //    //var interval = (AppSettings.Freq - now.Minute*60 - now.Second) *1000 - now.Millisecond;
        //    var interval = (AppSettings.Freq - now.Minute * 60 - now.Second) * 1000 - now.Millisecond;
        //    if (interval > 0) return interval;
        //    else return (AppSettings.Freq*1000 + interval);
        //}
    }
}
