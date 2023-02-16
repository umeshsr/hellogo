using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Net.Http;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.IO;
using System.Text;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text.RegularExpressions;
using Salesforce.Common;
using Salesforce.Force;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using HtmlAgilityPack;
using Newtonsoft.Json;
using MimeKit;
using MailKit.Security;
using MailKit.Net.Smtp;
using System.Xml;
using System.Xml.Linq;
using ExtensionMethods2;
using Microsoft.Graph;

namespace ExtensionMethods2
{
 
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


public partial class getusers : System.Web.UI.Page
{
    String act = "";


    protected async void Page_Load(object sender, EventArgs e)
    {
        logIt("success, start page_load");
        try
        {
            var vToken = "";

            HttpContext httpContext = HttpContext.Current;

            string authHeader = httpContext.Request.Headers["Authorization"];
            string apiHeader = httpContext.Request.Headers["apikey"];
            
            string username = "";
            string password = "";

            if (authHeader != null && authHeader.StartsWith("Basic") && apiHeader== "theApiHeader")
            {
                string encodedUsernamePassword = authHeader.Substring("Basic ".Length).Trim();
                Encoding encoding = Encoding.GetEncoding("iso-8859-1");
                string usernamePassword = encoding.GetString(Convert.FromBase64String(encodedUsernamePassword));

                int seperatorIndex = usernamePassword.IndexOf(':');

                username = usernamePassword.Substring(0, seperatorIndex);
                password = usernamePassword.Substring(seperatorIndex + 1);

                if (username != "jitbit@ashoka.org")
                {
                    logIt("fail, jitbit is not the username");
                    throw new Exception("Not authorized to access this application.");
                }
            }
            else
            {
                //Handle what happens if that isn't the case
                logIt("fail, The authorization header is either empty or misconfigured");
                throw new Exception("Not authorized to access this application.");
            }

            //CONFIGURE VARIABLES IF THE ABOVE TEST IS PASSED. 
            string ldapserver = "theLdapServer";
            string ldapuser = "theLdapUid";
            string ldappass = "theLdapPass";
            PrincipalContext context;
            string useruser = username;
            int index = useruser.IndexOf("@");
            
            if (index > 0)
            {
                useruser = useruser.Substring(0, index);
            }
            string userfull = useruser + "@ashoka.org";
            string userpass = password;

            
            bool valid = false;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ContextOptions options = ContextOptions.Negotiate | ContextOptions.Sealing;
            using (context = new PrincipalContext(ContextType.Domain, ldapserver, null, options, ldapuser, ldappass))
            {
                valid = context.ValidateCredentials(username, password, options);
                if (valid) {
                    logIt("success, start retieving users");
                    UserPrincipal userPrincipal2 = new UserPrincipal(context);
                    userPrincipal2.EmailAddress = "*@ashoka.org";
                    PrincipalSearcher srch = new PrincipalSearcher(userPrincipal2);
                    var fAll = srch.FindAll();
                    logIt("success, complete retieving users");
                    JArray array = new JArray();
                    foreach (var f in fAll)
                    {
                        if ((f.DistinguishedName.Contains("Ashoka Accounts") || f.DistinguishedName.Contains("Disabled")) && !(f.DistinguishedName.Contains("Archived")))
                        {
                            var upn = f.UserPrincipalName.ToString();
                            array.Add(upn);
                        }

                    }
                    JObject oo = new JObject();
                    oo["value"] = array;
                    Response.ContentType = "application/json; charset=utf-8";
                    Response.Write(oo.ToString());
                    logIt("success, write user to response");
                    logIt("success, end");
                }
                else
                {
                    logIt("fail, Authentication failed for jitbit user");
                    throw new Exception("Not authorized to access this application.");
                }
                

            }
        }
        catch (Exception ex)
        {
            //WRITE BACK FAILURE
            Response.ContentType = "application/json; charset=utf-8";
            Response.Write("{\"value\":\"fail; " + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "; " + ex.Message + "\"}");
            logIt("fail, end");
            //var sent = sendMail("There was a failure in processing the SAGE AD user report request. Check the server logs in AZU-Boot for more information.");
        }
    }


    public class Token
    {
        public string access_token { get; set; }

    }



    protected void writeIt(String response)
    {
        Response.Write(response);

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
            System.IO.File.AppendAllText(@"C:\Logs\getusers_" + DateTime.Now.ToString("M-d-yyyy") + ".txt", ms + ", " + DateTime.Now.ToString("M/d/yyyy HH:mm:ss") + Environment.NewLine);
        }
        catch (Exception e)
        {

        }
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

        msg.Subject = "re: error with SAGE AD users check";
        msg.Body = new ItemBody()
        {
            Content = msgText,
            ContentType = BodyType.Html,
        };


        await client.Users["api-notifications@ashoka.org"].SendMail(msg, true).Request().PostAsync();
        logIt("success, email message sent");
        return "sent";
    }
}
