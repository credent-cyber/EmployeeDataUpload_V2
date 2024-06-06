using Renci.SshNet.Sftp;
using Renci.SshNet;
using System;
using System.Configuration;
using System.IO;
using EmployeeDataUpload_V3.FTP.Logger;
using EmployeeDataUpload_V3.SuccessFactorsClient;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EmployeeDataUpload_V3.FTP
{
    public class FetchFtpData
    {
        string host = ConfigurationManager.AppSettings["FtpURL"];
        string remoteDirectory = ConfigurationManager.AppSettings["remoteDirectory"];
        string localDirectory = ConfigurationManager.AppSettings["FTPLocalDirectory"];
        string username = ConfigurationManager.AppSettings["FTPUser"];
        string password = ConfigurationManager.AppSettings["FTPPass"];
        string EmployeeIdNotFound = ConfigurationManager.AppSettings["EIdNotFound"];
        SuccessFactorsClient.SuccessFactorsClient client = new SuccessFactorsClient.SuccessFactorsClient();

        public async Task DownloadFromFTP(DateTime targetDate)
        {
            try
            {
                
                // Establish a connection to the SFTP server
                using (var sftp = new SftpClient(host, username, password))
                {
                    sftp.Connect();
                    Console.WriteLine("Connected to the SFTP server");
                    LogHelper.WriteLine("Connected to the SFTP server");

                    // List files in the specified remote directory
                    var files = sftp.ListDirectory(remoteDirectory);
                    int count = 0;
                    foreach (var file in files)
                    {
                        // Skip directories and hidden files
                        if (!file.IsDirectory && !file.Name.StartsWith("."))
                        {
                            SftpFileAttributes fileAttributes = sftp.GetAttributes(file.FullName);

                            DateTime modificationDate = fileAttributes.LastWriteTime;

                            // Check if the modification date matches the target date
                            if (modificationDate.Date >= targetDate.Date)
                            {
                                string remoteFilePath = remoteDirectory + "/" + file.Name;
                                string localFilePath = Path.Combine(localDirectory, file.Name);

                                using (Stream fileStream = File.Create(localFilePath))
                                {
                                    ++count;
                                    sftp.DownloadFile(remoteFilePath, fileStream);
                                    Console.WriteLine($"{count}. Downloaded: {file.Name}");
                                    LogHelper.WriteLine($"{count}. Downloaded: {file.Name}");
                                }
                                
                                // Rename the file after downloading
                                var match = System.Text.RegularExpressions.Regex.Match(file.Name, @"^\d+");
                                if (match.Success)
                                {
                                    string fileCode = match.Value;
                                    string userId = await client.GetUserIdAsync(fileCode);

                                    if (!string.IsNullOrEmpty(userId))
                                    {
                                        string newFilePath = Path.Combine(localDirectory, userId + Path.GetExtension(file.Name));
                                        File.Move(localFilePath, newFilePath);
                                        Console.WriteLine($"Renamed to: {userId + Path.GetExtension(file.Name)}");
                                        LogHelper.WriteLine($"Renamed to: {userId + Path.GetExtension(file.Name)}");
                                    }
                                }
                            }
                        }
                    }

                    sftp.Disconnect();
                    Console.WriteLine($"Total {count} files downloaded from the SFTP server");
                    Console.WriteLine("Disconnected from the SFTP server");
                    LogHelper.WriteLine($"Total {count} files downloaded from the SFTP server");
                    LogHelper.WriteLine("Disconnected from the SFTP server");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("An error occurred: " + ex.Message);
                LogHelper.ExceptionWriteLine("An error occurred: " + ex.Message);
            }
        }

    }
}
