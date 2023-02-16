using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.DirectoryServices;
using System.DirectoryServices.AccountManagement;
using System.Net;
using System.IO;
using System.Xml;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using System.Reflection;

public partial class ad_update : System.Web.UI.Page
{

    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {
            HttpContext httpContext = HttpContext.Current;

            string authHeader = httpContext.Request.Headers["Authorization"];
            string username = "";
            string password = "";

            if (authHeader != null && authHeader.StartsWith("Basic"))
            {
                string encodedUsernamePassword = authHeader.Substring("Basic ".Length).Trim();
                Encoding encoding = Encoding.GetEncoding("iso-8859-1");
                string usernamePassword = encoding.GetString(Convert.FromBase64String(encodedUsernamePassword));

                int seperatorIndex = usernamePassword.IndexOf(':');

                username = usernamePassword.Substring(0, seperatorIndex);
                password = usernamePassword.Substring(seperatorIndex + 1);

		if (username != "jitbit@ashoka.org")
                {
                    throw new Exception("Not authorized to access this application.");
                }

            }
            else
            {
                //Handle what happens if that isn't the case
                throw new Exception("The authorization header is either empty or misconfigured.");
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

            //BEGIN AUTHENTICATION
            bool valid = false;
	    ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ContextOptions options = ContextOptions.Negotiate | ContextOptions.Sealing;
            using (context = new PrincipalContext(ContextType.Domain, ldapserver, null, options, ldapuser, ldappass))
            {

                //CONVERT EMAIL TO SAM AND FIND USER TO TEST
                UserPrincipal userPrincipal2 = new UserPrincipal(context);
                userPrincipal2.EmailAddress = useruser + "@*";
                PrincipalSearcher srch = new PrincipalSearcher(userPrincipal2);
                var found = srch.FindOne();
                userfull = found.SamAccountName + "@ashoka.org";

                //CHECK USER ID AND CREDENTIALS 
                valid = context.ValidateCredentials(userfull, userpass, options);

                //IF USER ID AND CREDS ARE GOOD THEN PROCEED FOR INTEGRATION USER -------ONLY IN MSA OU-------------------------
                if (valid == true && found.DistinguishedName.Contains("MSAs"))
                {
                    //GET JSON FROM REQUEST
                    using (Stream receiveStream = Request.InputStream)
                    {
                        using (StreamReader readStream = new StreamReader(receiveStream, Encoding.UTF8))
                        {
                            var body = readStream.ReadToEnd();
                            JObject ob = JObject.Parse(body);
                            //Response.Write(ob.ToString());


                            //GET USER TO UPDATE FROM POST AND URLDECODE
                            String userUpdate = (string)ob["uid"];
                            int counter2 = 0;

                            using (context = new PrincipalContext(ContextType.Domain, ldapserver, null, options, ldapuser, ldappass))
                            {
                                //SET THE USER WE ARE LOOKING FOR AND USE SEARCHER TO FIND USER ID WITH ANY DOMAIN (E.G., TBELL@). @ IS REQUIRED IN THE SEARCH TO AVOID FINDING TBELLLLLL.
                                UserPrincipal userPrincipal = new UserPrincipal(context);
                                index = userUpdate.IndexOf("@");
                                userfull = userUpdate.Substring(0, index);
                                userPrincipal.EmailAddress = userfull + "@*";//find only email address and any domain
                                PrincipalSearcher srch2 = new PrincipalSearcher(userPrincipal);
                                //WRITE PROGRESS TO THE LOG FILE
				var disab = false;
				var expCheck = false;
                                logging("success, ad_update, retrieve userPrincipal, " + userUpdate);

                                //LOOP ACROSS ALL FOUND ITEMS IN AD AND UPDATE AS NEEDED. THE SCRIPT WILL IGNORE PARAMS THAT ARE NOT SENT.
                                //ON TESTING WITH JITTERBIT WE FOUND THAT WHEN SENDING NUMBERS THE SCRIPT WOULD THROUGH AN ERROR UNLESS WE REMOVED URLDECODE WHEN GRABBING THE PARAM. ALPHANUMERIC WORKS WITH URLDECODE AND IS RETAINED BELOW IN THOSE INSTANCES.
                                foreach (var found2 in srch2.FindAll())
                                {
                                    counter2 = counter2 + 1;
                                    DirectoryEntry de = (found2.GetUnderlyingObject() as DirectoryEntry);

                                    
                                    if ((string)ob["dna"] != null)
                                    {
                                        String displayName = (string)ob["dna"];
                                        if (displayName != "")
                                        {
                                            //found2.DisplayName = displayName;
                                        }
                                        else
                                        {
                                            //found2.DisplayName = null;
                                        }
                                    }

                                    if ((string)ob["vtl"] != null)
                                    {
                                        //String voiceTel = HttpUtility.UrlDecode(Request.Params["vtl"]);
                                        String voiceTel = (string)ob["vtl"];
                                        if (voiceTel != "")
                                        {
                                            //found.VoiceTelephoneNumber = voiceTel;
                                            de.Properties["telephoneNumber"].Value = voiceTel;
                                        }
                                        else
                                        {
                                            //found.VoiceTelephoneNumber = null;
                                            de.Properties["telephoneNumber"].Clear();
                                        }

                                    }

                                    if ((string)ob["mob"] != null)
                                    {
                                        //String mobileTel = HttpUtility.UrlDecode(Request.Params["mob"]);
                                        String mobileTel = (string)ob["mob"];
                                        if (mobileTel != "")
                                        {
                                            de.Properties["mobile"].Value = mobileTel;
                                        }
                                        else
                                        {
                                            de.Properties["mobile"].Clear();
                                        }
                                    }

                                    if ((string)ob["ipp"] != null)
                                    {
                                        String ipPhone = (string)ob["ipp"];
                                        if (ipPhone != "")
                                        {
                                            de.Properties["ipphone"].Value = ipPhone;
                                        }
                                        else
                                        {
                                            de.Properties["ipphone"].Clear();
                                        }
                                    }

                                    if ((string)ob["ttl"] != null)
                                    {
                                        String title = (string)ob["ttl"];
                                        if (title != "")
                                        {
                                            de.Properties["title"].Value = title;
                                        }
                                        else
                                        {
                                            de.Properties["title"].Clear();
                                        }
                                    }

                                    if ((string)ob["dep"] != null)
                                    {
                                        String department = (string)ob["dep"];
                                        if (department != "")
                                        {
                                            de.Properties["department"].Value = department;
                                        }
                                        else
                                        {
                                            de.Properties["department"].Clear();
                                        }
                                    }

                                    if ((string)ob["off"] != null)
                                    {
                                        String office = (string)ob["off"];
                                        if (office != "")
                                        {
                                            de.Properties["physicalDeliveryOfficeName"].Value = office;
                                        }
                                        else
                                        {
                                            de.Properties["physicalDeliveryOfficeName"].Clear();
                                        }
                                    }

                                    if ((string)ob["exp"] != null)
                                    {
                                        String expire = (string)ob["exp"];

                                        DateTime dt;
                                        if (DateTime.TryParse(expire, out dt))
                                        {
                                            String dtft = dt.ToFileTime().ToString();
					    
					    //if(ConvertLargeIntegerToLong(de.Properties["accountExpires"].Value) < dt.ToFileTime()){expCheck=true;}
    					    if(DateTime.Today.ToFileTime() < dt.ToFileTime()){expCheck=true;}
					    logging("current exp: " + ConvertLargeIntegerToLong(de.Properties["accountExpires"].Value) + " dtft: " + dt.ToFileTime());
                                            de.Properties["accountExpires"].Value = dtft;
					    
					    logging(DateTime.Today.ToFileTime().ToString());                                           
                                        }
                                        else if (expire == "0")
                                        {
					    if(ConvertLargeIntegerToLong(de.Properties["accountExpires"].Value).ToString()!="0"){expCheck=true;}
					    //logging("current exp: " + ConvertLargeIntegerToLong(de.Properties["accountExpires"].Value));
                                            de.Properties["accountExpires"].Value = 0;
                                            //logging("test; accountExpires: set to 0");//REMOVE
                                        }

                                    }

                                    if ((string)ob["mgr"] != null)
                                    {
                                        String manager = (string)ob["mgr"];
                                        if (manager != "")
                                        {
                                            int index2 = manager.IndexOf("@");
                                            manager = manager.Substring(0, index2);
                                            UserPrincipal userPrincipal3 = new UserPrincipal(context);
                                            userPrincipal3.EmailAddress = manager + "@*";
                                            PrincipalSearcher srch3 = new PrincipalSearcher(userPrincipal3);
                                            foreach (var found3 in srch3.FindAll())
                                            {
                                                logging("success, ad_update, found manager " + found3.DisplayName);
                                                DirectoryEntry de3 = (found3.GetUnderlyingObject() as DirectoryEntry);
                                                de.Properties["manager"].Value = de3.Properties["distinguishedName"].Value;
                                            }
                                            userPrincipal3.Dispose();
                                        }
                                        else
                                        {
                                            de.Properties["manager"].Clear();
                                        }
                                    }

                                    //WRITE CHANGES TO AD
//if(found2.DistinguishedName.Contains("MSAs")||found2.DistinguishedName.Contains("Disabled")){
                                    found2.Save();
//}
                                    //WRITE TO LOG FILE
                                    logging("success, ad_update, write to ad, " + userUpdate);
				    if(found2.DistinguishedName.Contains("Disabled")){disab=true;}
                                }
                                //WRITE BACK
Response.ContentType = "application/json";
                                if (counter2 > 0)
                                {
				    var sucdis="";
				    //IF IN DISABLED OU AND THE END DATE HAS CHANGED
				    if(disab==true && expCheck==true){
					sucdis="success_adsync_disabled";
				    }else{
					sucdis="success_adsync_active";
				    }

                                    Response.Write("{\"value\":\""+sucdis + "; " + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "; " + counter2 + " users found in directory.\"}");//GS provide structure for the write-back
                                    logging("success; " + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "; " + counter2 + " users found in directory.");
                                }
                                else
                                {
                                    Response.Write("{\"value\":\"fail_adsync_errorcode; " + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "; " + counter2 + " users found in directory.\"}");
                                    logging("fail; " + DateTime.Now.ToString("yyyyMMddHHmmssfff") + "; " + counter2 + " users found in directory.");
                                }
                            }

                        }
                        
                    }
                }
                else
                {

                    //Response.Write("no auth user" + username);
		    throw new Exception("Not authorized to access this application.");

                }
            }



        }
        catch (Exception ex)
        {
            //WRITE BACK TO JITTERBIT WITH FAILURE
		    Response.Write("{\"value\":\"fail; " + DateTime.Now.ToString("yyyyMMddHHmmssfff") +"; " + ex.Message + "\"}");
		    logging("fail; "  + DateTime.Now.ToString("yyyyMMddHHmmssfff") +"; " + ex.Message);
            
        }
    }
   	
    private static long ConvertLargeIntegerToLong(object largeInteger)
    {
        Type type = largeInteger.GetType();

        int highPart = (int)type.InvokeMember("HighPart", BindingFlags.GetProperty, null, largeInteger, null);
        int lowPart = (int)type.InvokeMember("LowPart", BindingFlags.GetProperty | BindingFlags.Public, null, largeInteger, null);

        return (long)highPart << 32 | (uint)lowPart;
    }


	//LOG FILE FUNCTION
	public void logging(String logText){
   		try{
			System.IO.File.AppendAllText(@"C:\Logs\adsync-t_"+ DateTime.Now.ToString("M-d-yyyy") + ".txt", logText + ", " + DateTime.Now.ToString("M/d/yyyy HH:mm:ss") + Environment.NewLine);
			//action
		}
		catch (Exception ex)
		{
			
		}
	}
   
}