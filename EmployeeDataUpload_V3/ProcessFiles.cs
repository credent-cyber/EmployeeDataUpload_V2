using EmployeeDataUpload_V3.Sharepoint;
using System;
using System.IO;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EmployeeDataUpload_V3
{
    public class ProcessFiles
    {
        SharepointClientContext sharepointClient = new SharepointClientContext();
        public async Task StartProccessingFiles()
        {
            string folderPath = ConfigurationManager.AppSettings["FolderSource"];
            string processedFolder = ConfigurationManager.AppSettings["ProcessedFolder"];
            string[] folders = Directory.GetDirectories(folderPath);

            foreach (string folder in folders)
            {
                string folderName = new DirectoryInfo(folder).Name;
                string folderCode = GetFolderCode(folderName);
                string[] files = Directory.GetFiles(folder, "*.pdf");

                foreach (string file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    string newFileName = $"{fileName}-{folderCode}.pdf";
                    var getVersions = await sharepointClient.SearchFileByName($"{fileName}-{folderCode}");
                    string highestVersionFileName = "1";
                    if (getVersions.Count > 0)
                    {
                        highestVersionFileName = await sharepointClient.GetHighestVersionFileName($"{fileName}-{folderCode}");
                        if (highestVersionFileName != null)
                        {
                            int fileVersion = int.Parse(highestVersionFileName);
                            newFileName = fileVersion == 1 ? $"{fileName}_{folderCode}-{fileVersion}.pdf" : $"{fileName}_{folderCode}-{++fileVersion}.pdf";
                            //newFileName = $"{fileName}_{folderCode}-{++fileVersion}.pdf";
                        }    
                    }
                    else
                    {
                        newFileName = $"{fileName}_{folderCode}-1.pdf";
                    }

                    string newPath = Path.Combine(folder, newFileName);

                    // Rename the file
                    File.Move(file, newPath);
                    string filePath = newPath;
                    
                    await sharepointClient.UploadFileWithMetadata(filePath, fileName, folderCode);
                    using (FileStream fileStream = File.OpenRead(filePath))
                    {
                        string fname = Path.GetFileName(filePath);
                        //await sharepointClient.UploadFileToSharePoint(fileStream, fname);
                    }

                    // Now Moving file to the Processed Folder
                    string processedFilePath = Path.Combine(processedFolder, newFileName);
                    File.Move(newPath, processedFilePath);
                }
            }
        }



        public string GetFolderCode(string folderName)
        {
            string pattern = @"\(([^)]*)\)";
            Match match = Regex.Match(folderName, pattern);

            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return string.Empty;
        }
    }
}
