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
        private IList<Foo> _nameList;
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
            _totalCount = _nameList.Count();
            _logger.Information("value : {0}", _nameList);
            foreach (var item in _nameList)
            {
                _logger.Information($"{item.AlarmTagInput} and {item.TagSuffixOutput}");
            }

            foreach (var item in _nameList)
            {
                RetrieveAlarmandUpdate(item);
            }
        }

        private void RetrieveAlarmandUpdate(Foo csvItem)
        {

            PIPoint AlarmPoint = PIPoint.FindPIPoint(_SitePI, csvItem.AlarmTagInput);

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


            //Find the SRC tag and update values into the tag
            string SourceTagname = csvItem.TagSuffixOutput +  "SRC.TEST";
            PIPoint SourceTagPoint = PIPoint.FindPIPoint(_SitePI, SourceTagname);
            SourceTagPoint.UpdateValues(sourceList.ToList(), OSIsoft.AF.Data.AFUpdateOption.Insert);

            //Output message to a messagelist from item
            var messageList = filteredActiveList.Select((item) =>
            {
                return new AFValue
                {
                    Timestamp = item.Timestamp,
                    Value = item.Value.ToString().Split('|')[3]
                };
            });

            //Find the MSG tag and update values into the tag
            string MSGTagname = csvItem.TagSuffixOutput + "MSG.TEST";
            PIPoint MSGTagPoint = PIPoint.FindPIPoint(_SitePI, MSGTagname);
            MSGTagPoint.UpdateValues(messageList.ToList(), OSIsoft.AF.Data.AFUpdateOption.Insert);

            //Find the Count tag and update values into the tag
            string CountTagname = csvItem.TagSuffixOutput + "COUNT.TEST";
            PIPoint CountTagPoint = PIPoint.FindPIPoint(_SitePI, CountTagname);
            var alarmCount = messageList.Count();
            
            AFValue numActive = new AFValue(alarmCount, endTime);
            CountTagPoint.UpdateValue(numActive, OSIsoft.AF.Data.AFUpdateOption.Insert);
        }
    }
}