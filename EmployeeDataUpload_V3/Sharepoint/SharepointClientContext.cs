using Microsoft.SharePoint.Client;
using System.IO;
using System.Linq;
using System.Security;
using System;
using System.Configuration;
using Microsoft.SharePoint;
using System.Collections.Generic;
using global::EmployeeDataUpload_V3.FTP.Logger;
using System.Threading.Tasks;

namespace EmployeeDataUpload_V3.Sharepoint
{
    public class SharepointClientContext
    {
        private readonly ClientContext _spContext;
        string url = ConfigurationManager.AppSettings["siteUrl"];
        string LibraryName = ConfigurationManager.AppSettings["LibraryName"];
        string user = ConfigurationManager.AppSettings["sharepointUsername"];
        string pwd = ConfigurationManager.AppSettings["sharepointPassword"];
        public SharepointClientContext()
        {
            _spContext = new ClientContext(url);

            SecureString sPwd = new SecureString();
            foreach (char c in pwd.ToCharArray())
            {
                sPwd.AppendChar(c);
            }

            _spContext.Credentials = new SharePointOnlineCredentials(user, sPwd);
        }

        public async Task<List<string>> SearchFileByName(string searchName)
        {
            List<string> foundFiles = new List<string>();

            try
            {
                Web web = _spContext.Web;
                _spContext.Load(web);
               _spContext.ExecuteQuery();

                List sharedDocuments = web.Lists.GetByTitle("Documents");
                CamlQuery query = new CamlQuery
                {
                    ViewXml = $@"
                        <View>
                            <Query>
                                <Where>
                                    <BeginsWith>
                                        <FieldRef Name='FileLeafRef'/>
                                        <Value Type='Text'>{searchName}</Value>
                                    </BeginsWith>
                                </Where>
                            </Query>
                            <RowLimit>100</RowLimit>
                        </View>"
                };

                ListItemCollection items = sharedDocuments.GetItems(query);
                _spContext.Load(items);
               _spContext.ExecuteQuery();

                foreach (ListItem item in items)
                {
                    foundFiles.Add(item["FileLeafRef"].ToString());
                }

                return foundFiles;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return foundFiles;
            }
        }

        public async Task<string> GetHighestVersionFileName(string searchName)
        {
            try
            {
                List<string> foundFiles = await SearchFileByName(searchName);

                int highestVersion = foundFiles
                    .Select(fileName =>
                    {
                        int versionIndex = fileName.LastIndexOf('-');
                        if (versionIndex != -1 && versionIndex < fileName.Length - 1)
                        {
                            string versionString = fileName.Substring(versionIndex + 1, fileName.Length - versionIndex - 5);
                            if (int.TryParse(versionString, out int version))
                            {
                                return version;
                            }
                        }
                        return -1;
                    })
                    .Where(version => version != -1)
                    .DefaultIfEmpty(1)
                    .Max();

                string highestVersionFileName = $"{highestVersion}";

                return highestVersionFileName;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                LogHelper.ExceptionWriteLine($"Error: {ex.Message}");
                return null;
            }
        }

        public async Task<bool> UploadFileWithMetadata(string filePath, string empCode, string docType)
        {
            try
            {
                byte[] fileBytes = System.IO.File.ReadAllBytes(filePath);
                string fileName = System.IO.Path.GetFileName(filePath);

                Web web = _spContext.Web;
                _spContext.Load(web);
               _spContext.ExecuteQuery();

                List sharedDocuments = web.Lists.GetByTitle("EmployeeDocuments");
                FileCreationInformation newFile = new FileCreationInformation
                {
                    Content = fileBytes,
                    Url = fileName,
                    Overwrite = true
                };

                Microsoft.SharePoint.Client.File uploadFile = sharedDocuments.RootFolder.Files.Add(newFile);
               _spContext.ExecuteQuery();

                ListItem item = uploadFile.ListItemAllFields;

                var empDetails = await GetEmpID(empCode);
                var docDetails = await GetDocumentType(docType);
                string hrCode = await GetHRManagerCode(empDetails.HRManagerEmail);

                item["EmployeeRefCodeLookupId"] = empDetails.Id; // lookup field
                item["EmployeeCode"] = empCode; // Text field
                item["DocumentCategoryLookupId"] = docDetails.docTypeId; // lookup field
                item["HRManagerCode"] = hrCode; // Text field
                item["IsBulkUploaded"] = "True"; // Text field or boolean as string
                item.Update();

               _spContext.ExecuteQuery();

                Console.WriteLine("Metadata updated successfully.");
                LogHelper.WriteLine("Metadata updated successfully.");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                LogHelper.ExceptionWriteLine($"Error: {ex.Message}");
                return false;
            }
        }

        public async Task<(int Id, string HRManagerEmail)> GetEmpID(string empCode)
        {
            try
            {
                Web web = _spContext.Web;
                _spContext.Load(web);
               _spContext.ExecuteQuery();

                List employeesList = web.Lists.GetByTitle("Employees");
                CamlQuery query = new CamlQuery
                {
                    ViewXml = $@"
                        <View>
                            <Query>
                                <Where>
                                    <Eq>
                                        <FieldRef Name='Code'/>
                                        <Value Type='Text'>{empCode}</Value>
                                    </Eq>
                                </Where>
                            </Query>
                            <RowLimit>1</RowLimit>
                        </View>"
                };

                ListItemCollection items = employeesList.GetItems(query);
                _spContext.Load(items);
               _spContext.ExecuteQuery();

                if (items.Count > 0)
                {
                    var item = items[0];
                    int id = item.Id;
                    string hrManagerEmail = item["HRManagerEmail"].ToString();

                    return (id, hrManagerEmail);
                }
                else
                {
                    throw new Exception("Employee not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (0, string.Empty);
            }
        }

        public async Task<(int docTypeId, string docCategoryName)> GetDocumentType(string docType)
        {
            try
            {
                Web web = _spContext.Web;
                _spContext.Load(web);
                _spContext.ExecuteQuery();

                List documentCategoriesList = web.Lists.GetByTitle("DocumentCategories");
                CamlQuery query = new CamlQuery
                {
                    ViewXml = $@"
                        <View>
                            <Query>
                                <Where>
                                    <Eq>
                                        <FieldRef Name='Code'/>
                                        <Value Type='Text'>{docType.ToLower()}</Value>
                                    </Eq>
                                </Where>
                            </Query>
                            <RowLimit>1</RowLimit>
                        </View>"
                };

                ListItemCollection items = documentCategoriesList.GetItems(query);
                _spContext.Load(items);
                _spContext.ExecuteQuery();

                if (items.Count > 0)
                {
                    var item = items[0];
                    int docTypeId = item.Id;
                    string docCategoryName = item["Title"].ToString();

                    return (docTypeId, docCategoryName);
                }
                else
                {
                    throw new Exception("Document type not found.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return (0, string.Empty);
            }
        }

        public async Task<string> GetHRManagerCode(string hrEmail)
        {
            try
            {
                string hrCode = string.Empty;

                Web web = _spContext.Web;
                _spContext.Load(web);
               _spContext.ExecuteQuery();

                List employeesList = web.Lists.GetByTitle("Employees");
                CamlQuery query = new CamlQuery
                {
                    ViewXml = $@"
                        <View>
                            <Query>
                                <Where>
                                    <Eq>
                                        <FieldRef Name='Email'/>
                                        <Value Type='Text'>{hrEmail}</Value>
                                    </Eq>
                                </Where>
                            </Query>
                            <RowLimit>1</RowLimit>
                        </View>"
                };

                ListItemCollection items = employeesList.GetItems(query);
                _spContext.Load(items);
               _spContext.ExecuteQuery();

                if (items.Count > 0)
                {
                    var item = items[0];
                    hrCode = item["Code"].ToString();
                }
                else
                {
                    throw new Exception("HR Manager not found.");
                }

                return hrCode;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return string.Empty;
            }
        }
    }
}


