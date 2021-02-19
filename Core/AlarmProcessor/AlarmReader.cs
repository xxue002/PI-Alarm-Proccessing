using Core.ConnectionManager;
using Core.FileReader;
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
        private IReader _reader;
        private IList<string> _nameList;
        private int _totalCount;
        private IList<string> _errorList = new List<string>();

        public AlarmReader(ILogger logger, IPIConnectionManager piCM, IReader reader)
        {
            _logger = logger;
            _piCM = piCM;
            _reader = reader;
        }

        //Get Alarm String
        public void RetrieveAlarm()
        {

            // Retrieve connected PIServer from PIConnectionManager
            (_IsConnected, _SitePI) = _piCM.Connect();

            // Retrieve list of Alarm PI Points from CSV
            _nameList = _reader.readFile();
            _totalCount = _nameList.Count;

            var tasks = new List<Task>();
            foreach (string Tagname in _nameList)
            {
                RetrieveAlarmandUpdate(Tagname);
                //string TagName = "CN.BJG.HMI0001.FLM01";
            }
            Task.WhenAll(tasks);
        }

        private void RetrieveAlarmandUpdate(string Tagname)
        {

            PIPoint AlarmPoint = PIPoint.FindPIPoint(_SitePI, Tagname);

            // Get current time and start time of 10 mins ago
            DateTime endTime = DateTime.Now;
            DateTime startTime = endTime.AddMinutes(-10);

            //Create AF Start time and End Time
            AFTime startAFTime = new AFTime(startTime);
            AFTime endAFTime = new AFTime(endTime);
            AFTimeRange QueryRange = new AFTimeRange(startAFTime, endAFTime);

            //Retrive PI data with time range
            AFValues valueList = AlarmPoint.RecordedValues(QueryRange, OSIsoft.AF.Data.AFBoundaryType.Inside, null, false);

            //Filter the list of PI Data with "|ACTIVE|"
            var filteredActiveList = valueList.Where((item) =>
            {
                return item.Value.ToString().Contains("|ACTIVE|");
            });

            foreach (var item in filteredActiveList)
            {
                _logger.Information("Timestamp : {0} Value : {1}", item.Timestamp, item.Value.ToString());
            }

            //Output source to a sourcelist from item
            var sourceList = filteredActiveList.Select((item) =>
            {
                // return as an AF Value
                //return item.Value.ToString().Split('|')[0];
                return new AFValue
                {
                    Timestamp = item.Timestamp,
                    Value = item.Value.ToString().Split('|')[0]
                };
            });

            //Convert Source List to AFValues IList
            AFValues AFsourceList = new AFValues();
            foreach (var i in sourceList)
            {
                AFValue AFSourceItem = new AFValue(i.Value, i.Timestamp);
                AFsourceList.Add(AFSourceItem);
            }
            //Find the SRC tag and update values into the tag
            string SourceTagname = "CN.BJG.FLM01.ALYS.ALARMS.SRC.TEST";
            PIPoint SourceTagPoint = PIPoint.FindPIPoint(_SitePI, SourceTagname);
            var InsertSourceData = SourceTagPoint.UpdateValues(AFsourceList, OSIsoft.AF.Data.AFUpdateOption.Insert);

            //Output message to a messagelist from item
            var messageList = filteredActiveList.Select((item) =>
            {
                return new AFValue
                {
                    Timestamp = item.Timestamp,
                    Value = item.Value.ToString().Split('|')[3]
                };
            });

            //Convert Message List to AFValues IList
            AFValues AFMessageList = new AFValues();
            foreach (var i in messageList)
            {
                AFValue AFMessagItem = new AFValue(i.Value, i.Timestamp);
                AFMessageList.Add(AFMessagItem);
            }

            //Find the MSG tag and update values into the tag
            string MSGTagname = "CN.BJG.FLM01.ALYS.ALARMS.MSG.TEST";
            PIPoint MSGTagPoint = PIPoint.FindPIPoint(_SitePI, MSGTagname);
            var InsertMessageData = MSGTagPoint.UpdateValues(AFMessageList, OSIsoft.AF.Data.AFUpdateOption.Insert);

            //Find the Count tag and update values into the tag
            string CountTagname = "CN.BJG.FLM01.ALYS.ALARMS.COUNT.TEST";
            PIPoint CountTagPoint = PIPoint.FindPIPoint(_SitePI, CountTagname);
            var alarmCount = messageList.ToList().Count;
            var timeNow = DateTime.Now;
            AFValue numActive = new AFValue(alarmCount, timeNow);
            CountTagPoint.UpdateValue(numActive, OSIsoft.AF.Data.AFUpdateOption.Insert);


            foreach (var item in messageList)
            {
                _logger.Information("Timstamp :{0}, Message : {1}", item.Timestamp, item.Value);
            }
            foreach (var item in sourceList)
            {
                _logger.Information("Timstamp :{0}, Message : {1}", item.Timestamp, item.Value);
            }
            _logger.Information("Timestamp :{0}, Count : {1}", timeNow, numActive);

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