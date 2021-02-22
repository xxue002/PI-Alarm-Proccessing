using Core.ConnectionManager;
using Core.FileReader;
using OSIsoft.AF.Asset;
using OSIsoft.AF.PI;
using OSIsoft.AF.Time;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.AlarmProcessor
{
    public class AlarmReader
    {
        private ILogger _logger;
        private IPIConnectionManager _piCM;
        private bool _IsConnected;
        private PIServer _SitePI;
        private DateTime _queryTime;

        public AlarmReader(ILogger logger, IPIConnectionManager piCM)
        {
            _logger = logger;
            _piCM = piCM;
        }

        //Get Alarm String
        public void RetrieveAlarm(IList<Foo> _csvlist)
        {
            // Retrieve connected PIServer from PIConnectionManager
            (_IsConnected, _SitePI) = _piCM.Connect();
            _queryTime = DateTime.Now;

            foreach (var item in _csvlist)
            {
                _logger.Information($"{item.AlarmTagInput} and {item.TagSuffixOutput}");
            }

            foreach (var item in _csvlist)
            {
                RetrieveAlarmandUpdate(item);
            }
        }

        private void RetrieveAlarmandUpdate(Foo csvItem)
        {

            PIPoint AlarmPoint = PIPoint.FindPIPoint(_SitePI, csvItem.AlarmTagInput);
            AFTimeRange QueryRange = GetQueryRange();

            //Retrive PI data with time range
            AFValues valueList = AlarmPoint.RecordedValues(QueryRange, OSIsoft.AF.Data.AFBoundaryType.Inside, null, false);

            //Filter the list of PI Data with "|ACTIVE|"
            var filteredActiveList = valueList.Where((item) =>
            {
                return item.Value.ToString().Contains("|ACTIVE|");
            });

            IEnumerable<AFValue> sourceList;
            if (csvItem.Mode != "3")
            {
                sourceList = filteredActiveList.Select(item =>
                {
                    if (csvItem.Mode == "1") return createSource1(item);
                    else return createSource2(item);
                });
            }
            else
            {
                var filterdHierarchyList = filteredActiveList.Where((item) =>
                {
                    return item.Value.ToString().Split('|')[9].Contains(csvItem.Hierarchy);

                });

                sourceList = filterdHierarchyList.Select(item => createSource1(item));
            }
            
            //foreach (var item in sourceList)
            //{
            //    _logger.Information($"{item.Timestamp} and {item.Value}");
            //}

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
            var alarmCount = sourceList.Count();
            _logger.Information($"{alarmCount}");            
            AFValue numActive = new AFValue(alarmCount, _queryTime);
            CountTagPoint.UpdateValue(numActive, OSIsoft.AF.Data.AFUpdateOption.Insert);
        }

        private AFValue createSource1(AFValue item)
        {
            return new AFValue
            {
                Timestamp = item.Timestamp,
                Value = item.Value.ToString().Split('|')[0]
            };
        }

        private AFValue createSource2(AFValue item)
        {
            return new AFValue
            {
                Timestamp = item.Timestamp,
                Value = item.Value.ToString().Split('|')[0].Split('/')[3],
            };
        }

        private AFTimeRange GetQueryRange()
        {
            DateTime endTime = _queryTime;
            DateTime startTime = endTime.AddMinutes(-10);

            //Create AF Start time and End Time
            AFTime startAFTime = new AFTime(startTime);
            AFTime endAFTime = new AFTime(endTime);
            AFTimeRange QueryRange = new AFTimeRange(startAFTime, endAFTime);
            return new AFTimeRange(startAFTime, endAFTime);
        }
    }
}