using Core.ConnectionManager;
using Core.FileReader;
using Core.Settings;
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
        private int _freq = AppSettings.Freq;
        private int _period = AppSettings.Interval;
        private AFTimeRange _queryRange;

        public AlarmReader(ILogger logger, IPIConnectionManager piCM)
        {
            _logger = logger;
            _piCM = piCM;
        }

        //Get Alarm String
        public void RetrieveAlarm(IList<Foo> _csvlist, DateTime signalTime)
        {
            _logger.Information($"Start Cycle");
            // Retrieve connected PIServer from PIConnectionManager
            (_IsConnected, _SitePI) = _piCM.Connect();
            _queryTime = signalTime;
            _queryRange = GetQueryRange();

            foreach (var item in _csvlist)
            {
                RetrieveAlarmandUpdate(item);
            }

            _logger.Information($"End Cycle");
        }

        private void RetrieveAlarmandUpdate(Foo csvItem)
        {
            // do search for all PI Points required for alarm processing
            var alarmSearch = GetPIPoint(csvItem.AlarmTagInput, "");
            var sourceSearch = GetPIPoint(csvItem.TagSuffixOutput, "SRC.TEST");
            var messageSearch = GetPIPoint(csvItem.TagSuffixOutput, "MSG.TEST");
            var countSearch = GetPIPoint(csvItem.TagSuffixOutput, "COUNT.TEST");

            // If any of the search above fails, exit the operation
            if ((!alarmSearch.Item1) || (!sourceSearch.Item1) || (!messageSearch.Item1) || (!countSearch.Item1))
            {
                _logger.Error($"Some of the PI Points required for {csvItem.AlarmTagInput} don't exist");
                return;
            }

            // Else continue processing alarms and updating relevant PI Points
            PIPoint AlarmPoint = alarmSearch.Item2;
            PIPoint SourceTagPoint = sourceSearch.Item2;
            PIPoint MSGTagPoint = messageSearch.Item2;
            PIPoint CountTagPoint = countSearch.Item2;

            

            //Retrive PI data with time range
            AFValues valueList = AlarmPoint.RecordedValues(_queryRange, OSIsoft.AF.Data.AFBoundaryType.Inside, null, false);

            //Filter the list of PI Data with "|ACTIVE|"
            var filteredActiveList = valueList.Where((item) =>
            {
                return item.Value.ToString().Contains("|ACTIVE|");
            });

            //Choose the code to execute base on the mode
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
                filteredActiveList = filteredActiveList.Where((item) =>
                {
                    return item.Value.ToString().Split('|')[9].Contains(csvItem.Hierarchy);
                });

                sourceList = filteredActiveList.Select(item => createSource1(item));
            }

            _logger.Information($"Tag {csvItem.AlarmTagInput} has {sourceList.Count()} alarms in the past 10 Minutes");

            //Find the SRC tag and update values into the tag
            TryUpdateValues(SourceTagPoint, sourceList, csvItem);
            
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
            TryUpdateValues(MSGTagPoint, messageList, csvItem);

            //Find the Count tag and update values into the tag        
            AFValue numActive = new AFValue(sourceList.Count(), _RoundDown(_queryTime));
            // Make numActive into an 1-member IEnumerable because TryUpdateValues require IEnumerable as a parameter
            IEnumerable<AFValue> numActiveList = new List<AFValue>() { numActive }; 
            TryUpdateValues(CountTagPoint, numActiveList, csvItem);
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
                Value = item.Value.ToString().Split('|')[0].Split('/')[3]
        };
        }

        private AFTimeRange GetQueryRange()
        {
            DateTime endTime = _RoundDown(_queryTime);
            _logger.Information($"{endTime.ToString()}");

            DateTime startTime = endTime.AddSeconds(-_period);
            _logger.Information($"{startTime.ToString()}");

            //Create AF Start time and End Time
            AFTime startAFTime = new AFTime(startTime);
            AFTime endAFTime = new AFTime(endTime);
            AFTimeRange QueryRange = new AFTimeRange(startAFTime, endAFTime);
            return new AFTimeRange(startAFTime, endAFTime);
        }

        private DateTime _RoundDown(DateTime queryTime)
        {
            var ticksInFreq = TimeSpan.FromSeconds(_freq).Ticks;
            return (queryTime.Ticks % ticksInFreq == 0) ? queryTime : new DateTime((queryTime.Ticks / ticksInFreq) * ticksInFreq);
        }

        // Wrap the UpdateValues in a Try Catch to deal with exceptions and prevent service from hard crashing
        private void TryUpdateValues(PIPoint sourcePoint, IEnumerable<AFValue> values, Foo csvItem)
        {
            try
            {
                sourcePoint.UpdateValues(values.ToList(), OSIsoft.AF.Data.AFUpdateOption.Insert);
            }
            catch (ArgumentException e)
            {
                _logger.Information($"There are no |ACTIVE| alarms in the last 10mins for {csvItem.AlarmTagInput}.");
            }

        }

        // Try to find PI Point, if no point found, return False and a null PI Point, else return true and the PI Point
        private (bool, PIPoint) GetPIPoint(string piPointName, string category = "")
        {
            PIPoint OutputPIPoint;
            string pointName = piPointName + category;
            bool result = PIPoint.TryFindPIPoint(_SitePI, pointName, out OutputPIPoint);
            return (result, OutputPIPoint);
        }
    }
}