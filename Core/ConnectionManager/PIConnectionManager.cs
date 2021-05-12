using Core.Settings;
using OSIsoft.AF.PI;
using Serilog;
using System;
using System.Net;

namespace Core.ConnectionManager
{
    public class PIConnectionManager : IPIConnectionManager
    {
        private static PIServer _SitePI;
        private ILogger _logger;
        private string _PICollectiveName = AppSettings.PICollectiveName;
        private static NetworkCredential _credential = new NetworkCredential("pivisionservice", "Zzb7Bdfm", "WIL");

        public PIConnectionManager(ILogger logger)
        {
            _logger = logger;
        }

        public (bool, PIServer) Connect()
        {
            _logger.Information("Establishing Connection...");
            // if _SitePI not initialized before, try get a handle for _SitePI specified in .config file.
            if (_SitePI == null)
            {
                _SitePI = new PIServers()[_PICollectiveName];
                if (_SitePI == null)
                {
                    _logger.Error("Unable to find the PI Collective {0} specified in configuration file", _PICollectiveName);
                    return (false, _SitePI);
                }
                else
                {
                    _logger.Information("Found PI Collective {0} specified in .config file", _PICollectiveName);
                }
            }

            try
            {
                _logger.Information("Connecting to PI {0}", _PICollectiveName);
                _SitePI.Connect(_credential, PIAuthenticationMode.WindowsAuthentication);
                _logger.Information("Connection to {0} successfully established", _PICollectiveName);
                // _logger.Information("AllowWriteValues: {0}", _SitePI.Collective.AllowWriteValues);
            }
            catch (Exception e)
            {
                _logger.Error("Unable to connect to PI Data Collective. Error: {0}", e.Message);
                return (false, _SitePI);
            }

            return (_SitePI.ConnectionInfo.IsConnected, _SitePI);
        }

        public bool Disconnect()
        {
            _logger.Information("Disconnecting from PI");
            try
            {
                _SitePI.Disconnect();
                _logger.Information("Successfully disconnected from PI Data Collective");
            }
            catch (Exception e)
            {
                _logger.Error("Unable to disconnect from PI Data Collective. Error {0}", e.Message);
                return false;
            }

            return true;
        }
    }
}
