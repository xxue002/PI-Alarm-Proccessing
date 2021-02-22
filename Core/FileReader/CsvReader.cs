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
