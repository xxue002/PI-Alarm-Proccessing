using Core.AlarmProcessor;
using Core.ConnectionManager;
using OSIsoft.AF.PI;
using Serilog;
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

        public HDAService(IPIConnectionManager piCM, ILogger logger, AlarmReader alarmReader) //IHistoryBackfiller backfiller)
        {
            _piCM = piCM;
            _logger = logger;
            _alarmReader = alarmReader;
            //_backfiller = backfiller;
        }

        public async Task Start()
        {
            _logger.Information("History Backfill Service started successfully");
            (_IsConnected, _SitePI) = _piCM.Connect();
            
            // If cannot connecto to PI Data Collective, return to terminate console app
            if (!_IsConnected) return;
            else
            {
                _alarmReader.RetrieveAlarm(); 
            }
        }

        public void Stop()
        {
            if (_IsConnected) _piCM.Disconnect();
            _logger.Information("History Backfill Service completed");
            _logger.Information("=============================================================================================");
            _logger.Information("=============================================================================================");
        }
    }
}
