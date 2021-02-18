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

            DateTime endTime = DateTime.Now;
            DateTime startTime = endTime.AddMinutes(-10);

            AFTime startAFTime = new AFTime(startTime);
            AFTime endAFTime = new AFTime(endTime);
            AFTimeRange QueryRange = new AFTimeRange(startAFTime, endAFTime);

            AFValues valueList = AlarmPoint.RecordedValues(QueryRange, OSIsoft.AF.Data.AFBoundaryType.Inside, null, false);

            var filteredActiveList = valueList.Where((item) =>
            {
                return item.Value.ToString().Contains("|ACTIVE|");
            });

            foreach (var item in filteredActiveList)
            {
                _logger.Information("Timestamp : {0} Value : {1}", item.Timestamp, item.Value.ToString());
            }
            
            var sourceList = filteredActiveList.Select((item) =>
            {
                return item.Value.ToString().Split('|')[0];
            });

            var messageList = filteredActiveList.Select((item) =>
            {
                return item.Value.ToString().Split('|')[3];
            });

            var numActive = filteredActiveList.ToList().Count;

            foreach (var item in messageList)
            {
                _logger.Information("Message : {0}", item);
            }
            foreach (var item in sourceList)
            {
                _logger.Information("Source : {0}", item);
            }
            _logger.Information("Count : {0}", numActive);

            //_logger.Information("Number of ACTIVE Alarms {0}", filteredList);

            //foreach(var item in filteredList)
            //{
            //    _logger.Information("Timestamp: {0}; Value: {1}", item.Timestamp, item.Value.ToString());
            //}
            //_logger.Information("Number of ACTIVE Alarmrs: {0}", filteredList.ToList().Count);

            //_logger.Information("Timestamp: {0}; Value: {1}", AlarmValue.Timestamp, AlarmValue.Value.ToString());

        }
    }
}
