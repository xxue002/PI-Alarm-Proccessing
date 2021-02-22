using System;
using System.IO;
using System.Globalization;
using System.Collections.Generic;
using Core.Settings;
using Serilog;
using System.Linq;

namespace Core.FileReader
{
    public class CsvReader : IReader
    {
        private ILogger _logger;
        private string _path = AppSettings.Path;
        private string[] _fileList;
        private IList<string> _csvData = new List<string>();
        private object row;

        public CsvReader(ILogger logger)
        {
            _logger = logger;
        }

        // show list of CSV files in the folder defined in AppSettings.Path

        private string showCsv()
        {
            _logger.Information("RETRIEVING FILES FROM {0}", _path);

            _fileList = Directory.GetFiles(_path, "*.csv");

            if (_fileList.Length > 0)
            {
                _logger.Information("LIST OF ALARM TAGS CSV FILES AVAILABLE ...");
                for (int i = 0; i < _fileList.Length; i++)
                {
                    _logger.Information("File:{0}", _fileList[i]);
                    return _fileList[i];
                }
            }
            else
            {
                _logger.Error("THERE ARE NO CSV FILES IN THIS LOCATION");
            }

            return _fileList[0];
        }

        //private void showUserChoicesCsv()
        //{
        //    _logger.Information("RETRIEVING FILES FROM {0}", _path);

        //    _fileList = Directory.GetFiles(_path,"*.csv");

        //    if (_fileList.Length > 0)
        //    {
        //        _logger.Information("LIST OF ALARM TAGS CSV FILES AVAILABLE ...");
        //        for (int i = 0; i < _fileList.Length; i++)
        //        {
        //            _logger.Information("     Choice {0}: {1}", i + 1, _fileList[i]);
        //        }
        //    }
        //    else
        //    {
        //        _logger.Error("THERE ARE NO CSV FILES IN THIS LOCATION");
        //    }
        //}

        // get the name of the file selected by the user
        //private string getUserChoiceCsv()
        //{
        //    string choice = "";
        //    int choiceInt;

        //    while (!int.TryParse(choice, out choiceInt) || choiceInt < 1 || choiceInt > _fileList.Length)
        //    {
        //        // keep asking for user input if input is invalid
        //        // for e.g. not an integer, integer not from 1 to 6
        //        Console.Write("SELECT THE CSV FILE TO READ FROM (FROM 1 TO {0}): ", _fileList.Length);
        //        choice = Console.ReadLine();
        //    }

        //    _logger.Information("CSV file selected for Alarm: {0}", _fileList[choiceInt-1]);

        //    return _fileList[choiceInt-1];
        //}

        // Read and export the list of tags from the selected CSV File
        //public IList<string> readFile()
        //{
        //    try
        //    {
        //        // Retrieve files in folder, if empty, terminate reader and return empty list
        //        showCsv();
        //        if (_fileList.Length == 0) return _csvData;

        //        using var streamReader = File.OpenText(_fileList[0]);
        //        using var csvReader = new CsvHelper.CsvReader(streamReader, CultureInfo.CurrentCulture);
        //        csvReader.Configuration.HasHeaderRecord = true;

        //        //csvReader.Configuration.ShouldSkipRecord = row => row[0].Contains("ALARM_TAGS");
        //        csvReader.Configuration.ShouldSkipRecord = row => row[0].Contains("Alarm Tag (Input)");

        //        while (csvReader.Read())
        //        {
        //            for (int i = 0; csvReader.TryGetField(i, out string value); i++)
        //            {
        //                _csvData.Add(value);
        //                _logger.Information("value : {0}", _csvData);
        //            }
        //        }
        //        csvReader.Dispose();
        //        streamReader.Close();                
        //    } 

        public IList<Foo> readFile()
        {
            IList<Foo> records;
            using (var streamReader = File.OpenText(showCsv())) 
            {
                using (var csvReader = new CsvHelper.CsvReader(streamReader, CultureInfo.CurrentCulture))
                {
                    records = csvReader.GetRecords<Foo>().ToList();
                }
            }
            return records;
        }
    }
}
