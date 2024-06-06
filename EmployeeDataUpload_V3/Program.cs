using EmployeeDataUpload_V3.DatabaseHelper;
using EmployeeDataUpload_V3.FTP;
using EmployeeDataUpload_V3.Sharepoint;
using System;
using System.Configuration;
using System.Threading.Tasks;

namespace EmployeeDataUpload_V3
{
    class Program
    {
        static async Task Main(string[] args)
        {
            FetchFtpData fetchFtpData = new FetchFtpData();
            ProcessFiles processFiles = new ProcessFiles();
            SharepointClientContext sharepointClient = new SharepointClientContext();
            DbHelper dbHelper = new DbHelper();

            Console.WriteLine("===========================================================");
            Console.WriteLine("=               EMPLOYEE DATA UPLOAD V2.0                 =");
            Console.WriteLine("===========================================================");

            Console.WriteLine($"\n\n     *** Process Started at {DateTime.Now} ***    \n\n");

            #region Download and Rename file from FTP Server
            string targetDateString = ConfigurationManager.AppSettings["TargetDownloadDate"];
            DateTime targetDate;

            if (!DateTime.TryParse(targetDateString, out targetDate))
            {
                targetDate = DateTime.Now.AddDays(-1);
            }

            await fetchFtpData.DownloadFromFTP(targetDate);
            #endregion

            #region Update file name with ShortCode and version
            await processFiles.StartProccessingFiles();
            #endregion
        }
    }
}
