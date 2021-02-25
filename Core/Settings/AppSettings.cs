using System;
using System.Configuration;

namespace Core.Settings
{
    public class AppSettings
    {
        public static string PICollectiveName => StringRetriever("PICollectiveName");
        public static string Path => StringRetriever("CSVLocation");
        public static int Freq => Convert.ToInt16(StringRetriever("FrequencyInSeconds"));
        public static int Interval => Convert.ToInt16(StringRetriever("PeriodInSeconds"));
        private static string StringRetriever(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }
    }
}