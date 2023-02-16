using System;
using System.Collections.Generic;
using System.Net.Http;
using System.IO;
using System.Text;
using System.Net.Http.Headers;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Net;
using Newtonsoft.Json;
using Microsoft.VisualBasic.FileIO;
using System.Linq;
using System.Text.RegularExpressions;
using MailKit.Net.Smtp;
using MimeKit;
using MailKit.Security;
using myextensions;


public partial class tfa2xls : System.Web.UI.Page
{
    String vFid = "";
    String vRid = "";



    protected void Page_Load(object sender, EventArgs e)
    {
            
            
            String vKey = "theApiKey";
        
            if (String.IsNullOrEmpty(Request.Params["formId"]) || String.IsNullOrEmpty(Request.Params["responseId"]) || String.IsNullOrEmpty(Request.Params["apiKey"]))
            {    
                Response.Clear();
                Response.ContentType = "application/json; charset=utf-8";
                Response.Write("{\"status\":\"failed\",\"message\":\"there is an issue with your request\"}");
                Response.End();
            }
            if (Request.Params["apiKey"] != vKey)
            {
                Response.Clear();
                Response.ContentType = "application/json; charset=utf-8";
                Response.Write("{\"status\":\"failed\",\"message\":\"invalid key\"}");
                Response.End();
            }

        vFid = Request.Params["formId"];
        vRid = Request.Params["responseId"];
        logging("starting request");

        //does the file exist
        getFile();

    }


    

    public class Token
    {
        public string access_token { get; set; }

    }


    public async Task<string> TestTask()
    {
        var postData = new List<KeyValuePair<string, string>>
        {
            new KeyValuePair<string, string>("resource", "https://graph.microsoft.com"),
            //new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("grant_type", "password"),
            new KeyValuePair<string, string>("username", "theUsername"),
            new KeyValuePair<string, string>("password", "thePassword"),
            new KeyValuePair<string, string>("scope", "Files.ReadWrite"),
            new KeyValuePair<string, string>("client_id", "theClientId"),
            new KeyValuePair<string, string>("client_secret", "theClientSecret"),

        };



        using (var client = new HttpClient())
        {
            //ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            //string baseUrl = "https://login.windows.net/ashokaoffice365.onmicrosoft.com/oauth2/";
            string baseUrl = "https://login.microsoftonline.com/bc233405-0f65-47d5-9bbb-58dc725df5c6/oauth2/token";

            client.BaseAddress = new Uri(baseUrl);
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var content = new FormUrlEncodedContent(postData);

            HttpResponseMessage response = await client.PostAsync("token", content);
            string jsonString = await response.Content.ReadAsStringAsync();
            logging(jsonString);
            Token responseData = JsonConvert.DeserializeObject<Token>(jsonString);
            return responseData.access_token;

        }
    }


    //-----
    public async void getFile()
    {
        var vToken = await TestTask();
        if (vToken != "")
        {

            String vDriveId = "b!agU9QK73DEurNV0W37DcFugK2-sHqz5DoixtXABop7R-MCyKsnJpTKD7hcRMvoCc";

            HttpClient client = new HttpClient();
            
            using (client)
            {
                
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", vToken);
                //does the file exist
                try
                { 
                    //var serviceEndpoint1 = "https://graph.microsoft.com/v1.0/drives/b!agU9QK73DEurNV0W37DcFugK2-sHqz5DoixtXABop7R-MCyKsnJpTKD7hcRMvoCc/root:/"+vFid+".xlsx";
                    var serviceEndpoint1 = "https://graph.microsoft.com/v1.0/drives/b!XMyRcbxJY0W23L-tFD8uot5gjbdJa-RNj39Wq9BVQIC0m7rlZ2ViSLDsag79mAO1/root:/FormResponses/"+vFid+".xlsx";//add _FILENAME.xls
                    var linkResponse1 = await client.GetAsync(serviceEndpoint1);

                    if (linkResponse1.IsSuccessStatusCode)
                    {
                        var re = await linkResponse1.Content.ReadAsStringAsync();
                        JObject ob = JObject.Parse(re);
                        string vSpId = (string)ob["id"];
                        logging("success, " + vSpId);
                        //CALL FUNCTION TO GET VALUES
                        getTfa(vSpId, vToken);
                    }
                    else
                    {
                        logging("failed to find file, " + linkResponse1);
                        copyFile(vToken);
                    }
                }
                catch (Exception e)
                {
			sendMail(e.ToString());
                }
               
            }

        }
        else
        {
            logging("failed to get token.");
        }
    }

    public async void copyFile(String vToken)
    {
        
        if (vToken != "")
        {

            String vDriveId = "b!agU9QK73DEurNV0W37DcFugK2-sHqz5DoixtXABop7R-MCyKsnJpTKD7hcRMvoCc";

            HttpClient client = new HttpClient();
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            using (client)
            {

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", vToken);
                //does the file exist
                try
                {
                    //var serviceEndpoint2 = "https://graph.microsoft.com/v1.0/drives/b!agU9QK73DEurNV0W37DcFugK2-sHqz5DoixtXABop7R-MCyKsnJpTKD7hcRMvoCc/items/01YYH5J4FL4FBRZMWJRJDZVHJ22YDYKHLJ/copy";
		    var serviceEndpoint2 = "https://graph.microsoft.com/v1.0/drives/b!XMyRcbxJY0W23L-tFD8uot5gjbdJa-RNj39Wq9BVQIC0m7rlZ2ViSLDsag79mAO1/items/013GNDFIB6SKYSDMKECJBIQMHYB3PIIB4L/copy";
                    var vShare3 = new StringContent("{\"name\": \"" + vFid + ".xlsx\"}"); //ADD _FILENAME.xlsx
                    vShare3.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    
                    var linkResponse2 = await client.PostAsync(serviceEndpoint2, vShare3);

                    if (linkResponse2.IsSuccessStatusCode)
                    {
                        var re = await linkResponse2.Content.ReadAsStringAsync();
                        IEnumerable<string> values;
                        string vCopyStatusUrl = string.Empty;
                        if (linkResponse2.Headers.TryGetValues("location", out values))
                        {
                            vCopyStatusUrl = values.FirstOrDefault();
                        }
                        logging(vCopyStatusUrl);
                        //check if copy completed
                        var counter = 0;
                        var vCopyId = "";
                        while (vCopyId == "" && counter < 60)
                        {
                            vCopyId = await CopyStatusTask(vCopyStatusUrl, vToken);
                            counter++;
                        }

                        if (vCopyId != "")
                        {
                            getTfaAll(vCopyId, vToken);
                            logging("got copy id " + vCopyId);
                        }
                        else
                        {
                            //throw exception
                            logging("failed to complete file copy");
                        }
                        
                    }
                    else
                    {
                        logging("failed to copy file, " + linkResponse2);
                        logging(await linkResponse2.Content.ReadAsStringAsync());
                        
                    }
                }
                catch (Exception e)
                {
                    logging("failed to connect with graph." + e);
		    sendMail(e.ToString());
                }

            }

        }
        else
        {
            logging("failed to get token.");
        }
    }

    public async Task<string> CopyStatusTask(String vUrl, String vToken)
    {
        if (vToken != "")
        {

            HttpClient client2 = new HttpClient();

            using (client2)
            {

                //client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", vToken);
                
                try
                {
ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
//logging("sending req");
                    var serviceEndpoint3 = vUrl;
                    var linkResponse3 = await client2.GetAsync(serviceEndpoint3);
//logging("response");
                    if (linkResponse3.IsSuccessStatusCode)
                    {
                        var re = await linkResponse3.Content.ReadAsStringAsync();
                        JObject ob = JObject.Parse(re);
                        string vS = (string)ob["status"];
//string vP = (string)ob["percentageComplete"];
//logging("copy status: "+vS);
//logging("copy percent: "+vP);
                        if (vS == "completed")
                        {
                            string vSpId = (string)ob["resourceId"];
                            return vSpId;
                        }
                        else
                        {
                            return "";
                        }
                        
                    }
                    else
                    {
                        logging("failed to connect with the status endpoint, " + linkResponse3);
                        return "";
                    }
                }
                catch (Exception e)
                {
logging("failed try to get status." + e);
                    return "";

                }

            }

        }
        else
        {
            logging("failed to get token.");
            return "";
        }

        
    }



	async void getTfa(String vSpId, String vToken)
	{

		ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

		var client = new HttpClient();

		HttpResponseMessage response;
		HttpResponseMessage response2;
		var urit = "https://app.formassembly.com/oauth/access_token";

		var values = new List<KeyValuePair<string, string>>();
		values.Add(new KeyValuePair<string, string>("grant_type", "authorization_code"));
		values.Add(new KeyValuePair<string, string>("type", "web_server"));
		values.Add(new KeyValuePair<string, string>("client_id", "theClientId"));
		values.Add(new KeyValuePair<string, string>("client_secret", "theClientSecret"));
		values.Add(new KeyValuePair<string, string>("redirect_uri", "https://api3.ashoka.org"));
		values.Add(new KeyValuePair<string, string>("code", "theCode"));
		var content = new FormUrlEncodedContent(values);

		try
		{
			using (client)
			{

				//response = await client.PostAsync(urit, content);
				//string m = await response.Content.ReadAsStringAsync();
				//JObject o = JObject.Parse(m);
				//string t = o["access_token"].ToString();
				string t = "theTfaToken";

				//var uric = "https://app.formassembly.com/api_v1/responses/export/" + vFid + ".csv?access_token=" + t;
				var uric = "https://app.formassembly.com/api_v1/responses/export/" + vFid + ".csv?response_ids=" + vRid + "&access_token=" + t;
				//var uric = "https://app.formassembly.com/api_v1/responses/export/"+vFid+".csv?response_ids=140479000&access_token=" + t;
				response2 = await client.GetAsync(uric);
				if (response2.IsSuccessStatusCode)
				{

					string r = await response2.Content.ReadAsStringAsync();
					//Response.Write(r);
					StringReader sr = new StringReader(r);
					using (TextFieldParser parser = new TextFieldParser(sr))
					{
						parser.Delimiters = new[] { "," };
						parser.HasFieldsEnclosedInQuotes = true;
						bool firstLine = true;
						while (!parser.EndOfData)
						{

							string[] line = parser.ReadFields();
							//Response.Write(line.Length); //number of cols in the form
							string con = "[";
							string con2 = "[";
							if (firstLine)
							{
								//this returns the col headers for the table
								firstLine = false;
							}
							else
							{
								foreach (var f in line)
								{

									//con = con + "\"" + (StringToCSVCell(f)) + "\",";
									con = con + '"' + StringToCSVCell(f) + '"' + ',';
									con2 = con2 + '"' + f + '"' + ',';
									//Response.Write(l + "\n");
								}
								con = con.Substring(0, con.Length - 1);
								con = con + "]";
								con2 = con2.Substring(0, con2.Length - 1);
								con2 = con2 + "]";
								//Response.Write(con + "\n");
								//Response.Write(con);
								//Response.Write(con2);
								updateXls(vSpId, vToken, con);
							}
						}

					}
				}
				else
				{
					throw new Exception("error connecting to tfa");
				}
			}

			//Response.Write(r);


		}
		catch (Exception e)
		{
			logging("error, failed in communicating with tfa, " + e);
			sendMail(e.ToString());
		}

	}


	async void getTfaAll(String vSpId, String vToken)
	{

		ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;

		var client = new HttpClient();

		HttpResponseMessage response;
		HttpResponseMessage response2;
		var urit = "https://app.formassembly.com/oauth/access_token";

		var values = new List<KeyValuePair<string, string>>();
		values.Add(new KeyValuePair<string, string>("grant_type", "authorization_code"));
		values.Add(new KeyValuePair<string, string>("type", "web_server"));
		values.Add(new KeyValuePair<string, string>("client_id", "theClientId"));
		values.Add(new KeyValuePair<string, string>("client_secret", "theClientSecret"));
		values.Add(new KeyValuePair<string, string>("redirect_uri", "https://api3.ashoka.org"));
		values.Add(new KeyValuePair<string, string>("code", "theCode"));
		var content = new FormUrlEncodedContent(values);

		try
		{
			using (client)
			{

				//response = await client.PostAsync(urit, content);
				//string m = await response.Content.ReadAsStringAsync();
				//JObject o = JObject.Parse(m);
				//string t = o["access_token"].ToString();
				string t = "theTfaToken";

				//var uric = "https://app.formassembly.com/api_v1/responses/export/" + vFid + ".csv?access_token=" + t;
				//var uric = "https://app.formassembly.com/api_v1/responses/export/" + vFid + ".csv?response_ids="+vRid+"&access_token=" + t;
				var uric = "https://app.formassembly.com/api_v1/responses/export/" + vFid + ".csv?&access_token=" + t;
				response2 = await client.GetAsync(uric);
				if (response2.IsSuccessStatusCode)
				{
					string r = await response2.Content.ReadAsStringAsync();
					//Response.Write(r);
					StringReader sr = new StringReader(r);
					string con = "";
					string con2 = "";
					using (TextFieldParser parser = new TextFieldParser(sr))
					{
						parser.Delimiters = new[] { "," };
						parser.HasFieldsEnclosedInQuotes = true;
						bool firstLine = true;

						while (!parser.EndOfData)
						{

							string[] line = parser.ReadFields();
							//line.Length //number of cols in the form

							if (firstLine == true)
							{
								//this returns the col headers for the table
								firstLine = false;
								//create table with
								var client2 = new HttpClient();
								using (client2)
								{
									client2.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", vToken);

									//var vTable = "https://graph.microsoft.com/v1.0/drives/b!agU9QK73DEurNV0W37DcFugK2-sHqz5DoixtXABop7R-MCyKsnJpTKD7hcRMvoCc/items/" + vSpId + "/workbook/worksheets/tfa-results/tables/add";
									var vTable = "https://graph.microsoft.com/v1.0/drives/b!XMyRcbxJY0W23L-tFD8uot5gjbdJa-RNj39Wq9BVQIC0m7rlZ2ViSLDsag79mAO1/items/" + vSpId + "/workbook/worksheets/tfa-results/tables/add";

									var vTableC = new StringContent("{\"address\": \"A1:" + GetColumnName(line.Length - 1) + "1\",\"hasHeaders\": true}");
									vTableC.Headers.ContentType = new MediaTypeHeaderValue("application/json");

									var linkResponse2 = await client2.PostAsync(vTable, vTableC);

									if (linkResponse2.IsSuccessStatusCode)
									{
										var head = "[";
										var head2 = "[";
										string ff = "";

										foreach (var f in line)
										{

											ff = new string(f.Take(100).ToArray());
											head = head + '"' + StringToCSVCell(ff) + '"' + ',';
											head2 = head2 + '"' + ff + '"' + ',';
										}
										head = head.Substring(0, head.Length - 1);
										head2 = head2.Substring(0, head2.Length - 1);

										head = head + "]";
										head2 = head2 + "]";

										//PATCH
										//logging("header values " + head);
										//logging("header values 2 " + head2);
										//var vTableHead = "https://graph.microsoft.com/v1.0/drives/b!agU9QK73DEurNV0W37DcFugK2-sHqz5DoixtXABop7R-MCyKsnJpTKD7hcRMvoCc/items/"+vSpId+"/workbook/worksheets/tfa-results/tables/Table1/headerRowRange";
										var vTableHead = "https://graph.microsoft.com/v1.0/drives/b!XMyRcbxJY0W23L-tFD8uot5gjbdJa-RNj39Wq9BVQIC0m7rlZ2ViSLDsag79mAO1/items/" + vSpId + "/workbook/worksheets/tfa-results/tables/Table1/headerRowRange";

										var vTableHeadC = new StringContent("{\"values\": [" + head2 + "]}");
										vTableHeadC.Headers.ContentType = new MediaTypeHeaderValue("application/json");

										var linkResponse3 = await client2.PatchAsync(vTableHead, vTableHeadC);
										if (linkResponse3.IsSuccessStatusCode)
										{
											logging("success updating headers " + linkResponse3.Content.ReadAsStringAsync());

										}
										else
										{
											logging("failed updating headers " + linkResponse3);
										}


									}
									else
									{
										logging("failed to create table" + await linkResponse2.Content.ReadAsStringAsync());
									}

								}
							}
							else
							{
								con = con + "[";
								con2 = con2 + "[";

								foreach (var f in line)
								{

									//con = con + "\"" + (StringToCSVCell(f)) + "\",";
									con = con + "\"" + StringToCSVCell(f) + "\"" + ',';
									con2 = con2 + "\"" + f + "\"" + ',';
									//Response.Write(l + "\n");
								}

								con = con.Substring(0, con.Length - 1);
								con = con + "],";
								con2 = con2.Substring(0, con2.Length - 1);
								con2 = con2 + "],";
								//updateXls(vSpId, vToken, con);

							}
						}
						con = con.Substring(0, con.Length - 1);
						con2 = con2.Substring(0, con2.Length - 1);
						//Response.Write(con);


					}

					updateXls(vSpId, vToken, con);

				}
				else
				{
                    			
                    			throw new Exception("error connecting to tfa: " + response2);

				}
				//Response.Write(r);
			}

		}
		catch (Exception e)
		{
			logging("error, failed in communicating with tfa, " + e);
			sendMail(e.ToString());
		}

	}



    static string GetColumnName(int index)
    {
        const string letters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";

        var value = "";

        if (index >= letters.Length)
            value += letters[index / letters.Length - 1];

        value += letters[index % letters.Length];

        return value;
    }


    public async void updateXls(String fid, String vToken, String vVals)
    {
        //get values from tfa

        String vDriveId = "b!agU9QK73DEurNV0W37DcFugK2-sHqz5DoixtXABop7R-MCyKsnJpTKD7hcRMvoCc";

        HttpClient client = new HttpClient();


        using (client)
        {

            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", vToken);
                
            try
            {

                //var serviceEndpoint3 = "https://graph.microsoft.com/v1.0/drives/b!agU9QK73DEurNV0W37DcFugK2-sHqz5DoixtXABop7R-MCyKsnJpTKD7hcRMvoCc/items/"+fid+"/workbook/worksheets/tfa-results/tables/Table1/rows/add";
                var serviceEndpoint3 = "https://graph.microsoft.com/v1.0/drives/b!XMyRcbxJY0W23L-tFD8uot5gjbdJa-RNj39Wq9BVQIC0m7rlZ2ViSLDsag79mAO1/items/"+fid+"/workbook/worksheets/tfa-results/tables/Table1/rows/add";

                //var vShare3 = new StringContent("{\"values\": [[1, 2, 3]]}");
                var vShare3 = new StringContent("{\"values\": ["+vVals+"]}", Encoding.UTF8, "application/json");
                //logging("{\"values\": [" + vVals + "]}\n\r");
                
                var linkResponse3 = await client.PostAsync(serviceEndpoint3, vShare3);
                    

                    if (linkResponse3.IsSuccessStatusCode)
                    {
                        var re = await linkResponse3.Content.ReadAsStringAsync();
                        logging("success, " + re);
                    }
                    else
                    {
			var re = await linkResponse3.Content.ReadAsStringAsync();
                        //logging("failed updatexls, " + re);
			throw new Exception("failed updatexls" + re);
                    }


                }
                catch (Exception e)
                {
                    //errorIt(e);
                    logging("failed main thread: " + e.ToString());
		    sendMail(e.ToString());
		    
                }

            }

        
    }

    public string StringToCSVCell(string str)
    {
        try
        {
            str = str.Trim();
            bool mustQuote = (str.Contains("\\") || str.Contains(",") || str.Contains("\"") || str.Contains("\r") || str.Contains("\n"));
            bool mustEscape = (str.StartsWith("*") || str.StartsWith(".") || str.StartsWith("+") || str.StartsWith("=") || str.StartsWith("-") || str.StartsWith("$"));

            if (mustEscape)
            {
                str = "'" + str;
            }
            if (mustQuote)
            {

                str = str.Replace("\\", "\\\\");
                str = Regex.Replace(str, @"\t|\n|\r", "\n");
                str = str.Replace("\"", "\\\"");
                
                return str;
            }

            return str;
        }
        catch (Exception ex)
        {
            //logging("error, powerbi, " + ex.Message);
            return ex.Message;
        }

    }

    public void sendMail(string msg)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("api-notifications@ashoka.org"));
        message.To.Add(new MailboxAddress("gseth@ashoka.org"));
        message.Subject = "re: error with api4 tfa";
        message.Body = new TextPart("html")
        {
            Text = "Form id: " + vFid + "<br>Response id: " + vRid + "<br>Error message:" + msg
        };
        try
        {
            using (var client = new SmtpClient())
            {
                //client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                client.Connect("smtp.office365.com", 587, SecureSocketOptions.StartTls);
                client.Authenticate("theUserName", "thePassword");
                client.Send(message);
                client.Disconnect(true);
            }
            //logging("success, tfa2xls error, send mail completed, ");
        }
        catch (Exception ex)
        {
            //Console.WriteLine("Exception caught in CreateTestMessage2(): {0}", ex.ToString());
            //Response.Write("error: " + ex.ToString());
            logging("error, tfa2xls, sendmailerror, " + ex.Message);
        }
        
    }



    public void logging(String logText)
    {
        try
        {
            System.IO.File.AppendAllText(@"C:\Logs\tfa2xls_"+ DateTime.Now.ToString("M-d-yyyy") + ".txt", logText + ", " + DateTime.Now.ToString("M/d/yyyy HH:mm:ss") + Environment.NewLine);
            //action
        }
        catch (Exception ex)
        {

        }

    }


}
namespace myextensions
{
public static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> PatchAsync(this HttpClient client, String requestUri, HttpContent iContent)
    {
        var method = new HttpMethod("PATCH");
        var request = new HttpRequestMessage(method, requestUri)
        {
            Content = iContent
        };

        HttpResponseMessage response = new HttpResponseMessage();
        try
        {
            response = await client.SendAsync(request);
        }
        catch (TaskCanceledException e)
        {

        }

        return response;
    }
}
}
