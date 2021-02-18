using Core.ConnectionManager;
using OSIsoft.AF.Asset;
using OSIsoft.AF.PI;
using OSIsoft.AF.Time;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.AlarmProcessor
{
    public class AlarmReader
    {
        private ILogger _logger;
        private IPIConnectionManager _piCM;
        private bool _IsConnected;
        private PIServer _SitePI;
        public AlarmReader(ILogger logger, IPIConnectionManager piCM)
        {
            _logger = logger;
            _piCM = piCM;
        }

        public void RetrieveAlarm()
        {
            string TagName = "CN.BJG.HMI0001.FLM01";
            (_IsConnected, _SitePI) = _piCM.Connect();
            PIPoint AlarmPoint = PIPoint.FindPIPoint(_SitePI, TagName);
            AFValue AlarmValue = AlarmPoint.CurrentValue();

            DateTime endTime = DateTime.Now;
            DateTime startTime = endTime.AddMinutes(-10);

            AFTime startAFTime = new AFTime(startTime);
            AFTime endAFTime = new AFTime(endTime);
            AFTimeRange QueryRange = new AFTimeRange(startAFTime, endAFTime);

            AFValues valueList = AlarmPoint.RecordedValues(QueryRange, OSIsoft.AF.Data.AFBoundaryType.Inside, null, false);
            foreach(var item in valueList)
            {
                _logger.Information("Timestamp: {0}; Value: {1}", item.Timestamp, item.Value.ToString());
            }
            //_logger.Information("Timestamp: {0}; Value: {1}", AlarmValue.Timestamp, AlarmValue.Value.ToString());

        }
    }
}
