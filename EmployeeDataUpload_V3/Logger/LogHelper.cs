using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EmployeeDataUpload_V3.FTP.Logger
{
    public class LogHelper
    {
        private static TextWriter Writer { get; }
        private static TextWriter ExceptionWriter { get; set; }

        static LogHelper()
        {
            if (!Directory.Exists("Logs"))
            {
                Directory.CreateDirectory("Logs");
            }
            Writer = new StreamWriter(File.Open(Path.Combine("Logs", "Log_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log"), FileMode.OpenOrCreate, FileAccess.ReadWrite));

        }

        public static void WriteLine(string message)
        {
            try
            {
                //Console.WriteLine(message);
                Writer.WriteLine(message);
                Writer.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured while writing to log.\n" + ex.Message + "\n" + ex.StackTrace);
            }
        }

        public static void ExceptionWriteLine(string message)
        {
            try
            {
                LogHelper.EnsureExceptionLogFileCreated();
                //Console.WriteLine(message);
                ExceptionWriter.WriteLine(message);
                ExceptionWriter.Flush();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occured while writing to log.\n" + ex.Message + "\n" + ex.StackTrace);
            }
        }

        public static void EnsureExceptionLogFileCreated()
        {
            if (ExceptionWriter == null)
            {
                if (!Directory.Exists("Logs/Exceptions"))
                {
                    Directory.CreateDirectory("Logs/Exceptions");
                }
                
                ExceptionWriter = new StreamWriter(File.Open(Path.Combine("Logs/Exceptions", "ExceptionLog_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".log"), FileMode.OpenOrCreate, FileAccess.ReadWrite));
            }
        }

        public static void CloseLogFiles()
        {
            try
            {
                Writer?.Close();
                ExceptionWriter?.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred while closing log files.\n" + ex.Message + "\n" + ex.StackTrace);
            }
        }
    }
}
