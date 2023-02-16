using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net.Security;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Security.Cryptography;
using System.Management.Automation;
using System.Collections.ObjectModel;
using System.Management.Automation.Runspaces;
using System.Security;
using System.Net.Http;
using System.Net.Http.Headers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;
using System.Xml.Serialization;
using System.IO;
using System.Xml;
using HtmlAgilityPack;
using System.Xml.Linq;
using ExtensionMethods;
using Microsoft.Graph;

namespace ExtensionMethods
{
    public static class IntExtensions
    {
        public static byte[] ReadAllBytes(this BinaryReader reader)
        {
            const int bufferSize = 4096;
            using (var ms = new MemoryStream())
            {
                byte[] buffer = new byte[bufferSize];
                int count;
                while ((count = reader.Read(buffer, 0, buffer.Length)) != 0)
                    ms.Write(buffer, 0, count);
                return ms.ToArray();
            }

        }
    }
    public class O365
    {
        public const string MsGraphEndpoint = "https://graph.microsoft.com/v1/";
        public const string MsGraphBetaEndpoint = "https://graph.microsoft.com/beta/";

        public class AuthenticationHelper : IAuthenticationProvider
        {
            public string AccessToken { get; set; }

            public Task AuthenticateRequestAsync(HttpRequestMessage request)
            {
                request.Headers.Add("Authorization", "Bearer " + AccessToken);
                return Task.FromResult(0);
            }
        }
    }
}

namespace NewMailNotification
{
    

    class Program
    {

        static async Task Main(string[] args)
        {

            var vToken = await TestTask();

            if (vToken != "")
            {

                await TestIt(vToken);
            }
            else
            {
                logIt("Graph returned an empty token.");
                var sent = await sendMail("Graph returned an empty token. Backup process has been aborted.");
            }

        }

        static async Task<String> TestIt(string vToken)
        {
            logIt("Start backup process");

            try
            {
                logIt("Check for backup files on SFDC.");
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

                var httpClient = new HttpClient();
                var someXmlString = @"<?xml version='1.0' encoding='utf-8' ?><soapenv:Envelope xmlns:soapenv = 'http://schemas.xmlsoap.org/soap/envelope/' xmlns:urn='urn:enterprise.soap.sforce.com'><soapenv:Header ></soapenv:Header><soapenv:Body><urn:login><urn:username>theSfdcUser</urn:username ><urn:password >theSfdcPass</urn:password></urn:login></soapenv:Body></soapenv:Envelope>";

                var stringContent = new StringContent(someXmlString, Encoding.UTF8, "text/xml");
                stringContent.Headers.ContentType = new MediaTypeHeaderValue("text/xml");
                httpClient.DefaultRequestHeaders.Add("SOAPAction", "''");
                var respone = await httpClient.PostAsync("https://login.salesforce.com/services/Soap/c/47.0", stringContent);

                string tes = await respone.Content.ReadAsStringAsync();
                var docXml = XDocument.Parse(tes);

                XNamespace ns = "urn:enterprise.soap.sforce.com";
                var vSid = (from fc in docXml.Descendants(ns + "sessionId") select fc.Value).FirstOrDefault();


                var baseAddress = new Uri("https://ashoka.my.salesforce.com/");
                HttpResponseMessage response;
                var cookieContainer = new CookieContainer();
                var handler = new HttpClientHandler() { CookieContainer = cookieContainer };


                using (var client2 = new HttpClient(handler))
                {

                    var uric = "https://ashoka.my.salesforce.com/ui/setup/export/DataExportPage/d";
                    cookieContainer.Add(baseAddress, new Cookie("sid", vSid));
                    response = await client2.GetAsync(uric);
                    string r = await response.Content.ReadAsStringAsync();

                    //Response.Write(r);
                    HtmlDocument doc = new HtmlDocument();
                    doc.LoadHtml(r);

                    HtmlNodeCollection links = doc.DocumentNode.SelectNodes("//a[@class='actionLink']");
                    //HtmlNodeCollection links = doc.DocumentNode.SelectNodes("//a");
                    logIt("Check for backup files in SFDC complete.");
                    if (!(links == null))
                    {
                        logIt("Backup files found in SFDC.");
                        logIt("Check SP for backup file.");
                        //GET GRAPH FILE LIST
                        //https://graph.microsoft.com/v1.0/drives/b!euTGV3t4okK63wt54AZx3bfb_wl-SnpKsARXVIONB21J8zaFMqQAS5AefXpvbYrz/root:/Weekly Backups:/children
                        client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", vToken);
                        var serviceEndpoint2 = "https://graph.microsoft.com/v1.0/drives/b!euTGV3t4okK63wt54AZx3bfb_wl-SnpKsARXVIONB21J8zaFMqQAS5AefXpvbYrz/root:/Weekly Backups:/children";
                        var linkResponse = await client2.GetAsync(serviceEndpoint2);

                        JArray items = null;
                        DateTime dt = DateTime.Now;
                        bool vExistTest = false;
                        if (linkResponse.IsSuccessStatusCode)
                        {
                            var vFiles = await linkResponse.Content.ReadAsStringAsync();
                            JObject ob = JObject.Parse(vFiles);
                            items = (JArray)ob["value"];

                            foreach (JObject o in items)
                            {
                                //string vFileName = (string)o["name"];
                                string vMime = "";
                                if (o.ContainsKey("file")) { vMime = (string)o["file"]["mimeType"]; }
                                string vFileDate = (string)o["fileSystemInfo"]["createdDateTime"];
                                var t = DateTime.Parse(vFileDate);

                                if (((dt - t).TotalDays < 4) && vMime == "application/zip")
                                {
                                    logIt("Current backup files found in SP.");
                                    vExistTest = true;
                                    break;
                                }
                            }

                        }
                        else
                        {
                            logIt("Graph returned failure code when attempting get SP files list");
                            throw new Exception("Graph returned failure code when attempting get SP files list.");
                        }


                        int i = 0;
                        if (vExistTest == false)
                        {
                            logIt("Current backup files not found in SP.");
                            logIt("Start reading backup files from SFDC.");
                            foreach (var l in links)
                            {

                                var lFileName = l.Attributes["href"].Value.Split('?')[1];
                                
                                //var lFileName = "me=meme.zip";
                                ///servlet/servlet.OrgExport?fileName=WE_00D400000007YpGEAU_1.ZIP&id=0928Z00001SMMXU
                                var downloadString = @"https://ashoka.my.salesforce.com/servlet/servlet.OrgExport?" + HtmlEntity.DeEntitize(lFileName);
                                //logIt(HtmlEntity.DeEntitize(lFileName));
                                string dtS = dt.ToString("yyyyMMdd");
                                var uriL = lFileName.Split('=')[1];
                                uriL = uriL.Split('&')[0];
                                var fn = dtS + "_" + uriL;

                                //downloadString= "https://api.ashoka.org/test.zip";
                                //var fn = "test.zip";

                                var URL = downloadString;
                                var uri = new Uri(URL);


                                var wReq = (HttpWebRequest)WebRequest.Create(URL);
                                wReq.Method = "GET";
                                wReq.CookieContainer = new CookieContainer();
                                wReq.CookieContainer.Add(baseAddress, new Cookie("sid", vSid));
                                WebResponse objResponse = wReq.GetResponse();
                                BinaryReader readStream = new BinaryReader(objResponse.GetResponseStream());
                                byte[] ByteBucket = readStream.ReadAllBytes();
                                logIt("Reading backup files from SFDC complete.");

                                //WRITE FILE
                                var vDriveId = "b!euTGV3t4okK63wt54AZx3bfb_wl-SnpKsARXVIONB21J8zaFMqQAS5AefXpvbYrz";//GRAPH DRIVE TO WRITE TO
                                //https://graph.microsoft.com/v1.0/drives/b!euTGV3t4okK63wt54AZx3bfb_wl-SnpKsARXVIONB21J8zaFMqQAS5AefXpvbYrz/root:/Weekly Backups/file.zip:/content
                                var folder = "Weekly Backups";

                                using (MemoryStream stream = new MemoryStream(ByteBucket))
                                {
                                    try
                                    {
                                        logIt("Start writing files to SP.");
                                        ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
                                        //REQUIRES TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384 CYPHER SUITE ON THE SERVER BE ENABLED 3-28-2022 TO FUNCTION.
                                        //USE IIS CRYPTO TO ENABLE AND RESTART
                                        //TO VALIDATE, RUN WIRESHARK ON THE INTERFACE WHEN RUNNING THE APPLICATION AND LOOK FOR THE HELLO PACKETS BETWEEN THE SERVER AND GRAPH API
                                        var authHelper = new O365.AuthenticationHelper() { AccessToken = vToken };
                                        GraphServiceClient gcs = new GraphServiceClient(authHelper);
                                        //https://graph.microsoft.com/v1.0/drives/" + vDriveId + "/root:/" + folder + "/" + fn + "
                                        //gcs.Drives["dkdkdk"].Root.ItemWithPath[Folder + "/" + fn].CreateUploadSession().Request().PostAsync();
                                        var uploadSession = await gcs.Drives[vDriveId].Root.ItemWithPath(folder + "/" + fn).CreateUploadSession().Request().PostAsync();
                                        var maxChunkSize = 320 * 1024; // 320 KB - Change this to your chunk size. 5MB is the default.
                                        var largeFileUpload = new LargeFileUploadTask<DriveItem>(uploadSession, stream);
                                        IProgress<long> progress = new Progress<long>(prog => { logIt("Uploaded " + prog/1024/1024 + " MB of "+ stream.Length/1024/1024 +" MB"); });

                                        var uploadResult = await largeFileUpload.UploadAsync(progress);

                                        if (uploadResult.UploadSucceeded)
                                        {
                                            logIt("Write file to SP complete.");
                                        }
                                        else
                                        {
                                            logIt("Graph returned failure code when attempting to upload");
                                            throw new Exception("Graph returned failure code when attempting to upload. Process aborted.");
                                        }

                                    }
                                    catch (Exception ex)
                                    {
                                        logIt("Issue uploading file to SP: " + ex.InnerException);
                                        throw new Exception("There was an issue uploading the file to SP: " + ex);
                                    }
                                }
  
                            }

                        }
                        else
                        {
                            logIt("Backup already exists in SP");
                        }
                    }
                    else
                    {
                        logIt("No files in SFDC to process.");
                    }

                }
            }
            catch (Exception ex)
            {
                logIt("There was an error in the main thread: " + ex);
                var sent = await sendMail("There was an error in backing up the SFDC backup files to SharePoint. Check the server logs in ARL for more information.");
            }
            logIt("End backup process.");

            return "true";
        }

        static async Task<string> sendMail(string msgText)
        {
            var vToken = await TestTask();
            var authProvider = new O365.AuthenticationHelper() { AccessToken = vToken };
            GraphServiceClient client = new GraphServiceClient(authProvider);
            var msg = new Message();
            //msg.ToRecipients = new List<Recipient>();
            msg.ToRecipients = new List<Recipient>()
        {
            new Recipient
            {
                EmailAddress = new EmailAddress
                {
                    Address = "gseth@ashoka.org"
                    //Address = vPersonalEmail
                }
            }
        };

            msg.Subject = "re: error with sfdc backup";
            msg.Body = new ItemBody()
            {
                Content = msgText,
                ContentType = BodyType.Html,
            };


            await client.Users["api-notifications@ashoka.org"].SendMail(msg, true).Request().PostAsync();
            logIt("email message sent");
            return "sent";
        }

        /*
        static void sendMail(string msg)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("api-notifications@ashoka.org", "api-notifications@ashoka.org"));
            message.To.Add(new MailboxAddress("gseth@ashoka.org", "gseth@ashoka.org"));
            message.Subject = "re: error with sfdc backup";
            message.Body = new TextPart("html")
            {
                Text = msg
            };
            try
            {
                using (var client = new SmtpClient())
                {
                    //client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    client.Connect("smtp.office365.com", 587, SecureSocketOptions.StartTls);
                    client.Authenticate("jitterbitnotifications@ashoka.org", "Z&8fH$C9hQ^CYWO!VsJZ");
                    client.Send(message);
                    client.Disconnect(true);
                }
                //logging("success, tfa2xls error, send mail completed, ");
            }
            catch (Exception ex)
            {
                //Console.WriteLine("Exception caught in CreateTestMessage2(): {0}", ex.ToString());
                //Response.Write("error: " + ex.ToString());
                logIt("error, tfa2xls, sendmailerror, " + ex.Message);
            }

        }
        */


        public class Token
        {
            public string access_token { get; set; }

        }

        static async Task<string> TestTask()
        {
            var postData = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("resource", "https://graph.microsoft.com"),
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", "theClientId"),
            new KeyValuePair<string, string>("client_secret", "theClientSecret"),

        };



            using (var client = new HttpClient())
            {
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
                //string baseUrl = "https://login.windows.net/ashokaoffice365.onmicrosoft.com/oauth2/";
                string baseUrl = "https://login.microsoftonline.com/bc233405-0f65-47d5-9bbb-58dc725df5c6/oauth2/token";

                client.BaseAddress = new Uri(baseUrl);
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                var content = new FormUrlEncodedContent(postData);

                HttpResponseMessage response = await client.PostAsync("token", content);
                string jsonString = await response.Content.ReadAsStringAsync();

                Token responseData = JsonConvert.DeserializeObject<Token>(jsonString);
                return responseData.access_token;

            }
        }

        static void logIt(String ms)
        {
            try
            {
                System.IO.File.AppendAllText(@"C:\Logs\sfdcbackup_" + DateTime.Now.ToString("M-d-yyyy") + ".txt", ms + ", " + DateTime.Now.ToString("M/d/yyyy HH:mm:ss") + Environment.NewLine);
            }
            catch (Exception e)
            {

            }
        }

    }
}
