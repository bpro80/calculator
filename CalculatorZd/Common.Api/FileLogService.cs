using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using log4net;

namespace Common.Api
{
    public interface ILogService
    {
        void Fatal(string message);
        void Error(string message);
        void Warn(string message);
        void Info(string message);
        void Debug(string message);
    }
    public class FileLogService: ILogService
    {
        static FileLogService()
        {
            // Gets directory path of the calling application
            // RelativeSearchPath is null if the executing assembly i.e. calling assembly is a
            // stand alone exe file (Console, WinForm, etc). 
            // RelativeSearchPath is not null if the calling assembly is a web hosted application i.e. a web site
            var log4NetConfigDirectory = AppDomain.CurrentDomain.RelativeSearchPath ?? AppDomain.CurrentDomain.BaseDirectory;
            var log4NetConfigFilePath = Path.Combine(log4NetConfigDirectory, "log4net.config");
            log4net.Config.XmlConfigurator.ConfigureAndWatch(new FileInfo(log4NetConfigFilePath));
        }

        private ILog _logger;
        public FileLogService(Type logClass)
        {
            _logger = LogManager.GetLogger(logClass);
        }
        public void Info(string message)
        {
            if (_logger.IsInfoEnabled)
                _logger.Info(message);
        }
        public void Fatal(string message)
        {
            throw new NotImplementedException();
        }

        public void Error(string message)
        {
            throw new NotImplementedException();
        }

        public void Warn(string message)
        {
            throw new NotImplementedException();
        }

        public void Debug(string message)
        {
            throw new NotImplementedException();
        }
    }
}