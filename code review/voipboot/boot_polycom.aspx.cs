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
using System.Text.RegularExpressions;
using System.Net.Mail;



public partial class boot_polycom : System.Web.UI.Page
{

    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {

	ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            //configs; change to request.form
            //string ldapserver = "auth.ashoka.org:636";
String ldapserver = "theLdapServer";
            int ldapport = 636;
            string ldaptree = "theLdapPath";
            string ldapuser = "theLdapUserId";
            string ldappass = "theLdapPass";

            string configfile = string.Empty;
            //String ldapextension;
            //String ldapfullname;
            //String ldapmail;
            //String ldapsip;
            //String ldapsippass;
	//String ldapsipauth;
            //String label;
            PrincipalContext context;
            String sesId;
	String vUa = Request.Params["uag"];
	String onsipPass = "theOnsipPass";
        String onsipUser = "theOnsipUser";
        String vIp = Request.UserHostAddress;
        //String vUa = Request.UserAgent;
        long vKey = theApiKey;
        Random rnd = new Random();
        int vHrs = rnd.Next(1, 3);
        int vMin = rnd.Next(0, 5);
        int vSec = rnd.Next(0, 9);
        List<int> vUc = new List<int>(); 
        vUc.Add(6);
        String useruser = "";
        String useruser2 = "";
	String label2 = "";
	String ldapsipauth2 = "";
	//String ldapsippass2 = "";
	String ldapfullname2 = "";
	String ldapsip2 = "";
	String userfull;
        int x = 0;
	String provUser = "";
	String ldapuserid;
	String lNaId = "";

     
        //test 
        if ((String.IsNullOrEmpty(Request.Params["uid"])) || (String.IsNullOrEmpty(Request.Params["key"])) || (Convert.ToInt64(Request.Params["key"])!=vKey))
        {
                //polycom
                Response.End();
        }
	if((!(String.IsNullOrEmpty(Request.Params["uid2"])))&&((String.IsNullOrEmpty(Request.Params["uct2"]))||(Convert.ToInt64(Request.Params["uct2"])<1)||(Convert.ToInt64(Request.Params["uct2"])>5))){
			Response.End();
	}
        
	//configs
useruser = Request.Params["uid"];	
useruser2 = Request.Params["uid2"];

        List<string> userOnsip = new List<string>();
        userOnsip.Add(useruser);

if(useruser2!=""){
userOnsip.Add(useruser2);
vUc[0]=vUc[0]-Convert.ToInt32(Request.Params["uct2"]);
vUc.Add(Convert.ToInt32(Request.Params["uct2"]));
}     
List<string> ldapfullname = new List<string>();
List<string> ldapmail = new List<string>();
List<string> ldapextension = new List<string>();	
List<string> ldapsip = new List<string>();
List<string> label = new List<string>();
List<string> ldapsippass = new List<string>();
List<string> ldapsipauth = new List<string>();

//provisioning user string
provUser = useruser;
if (!(String.IsNullOrEmpty(useruser2))){
	provUser = provUser + "=" + useruser2 + "=" + Convert.ToInt32(Request.Params["uct2"]);
}

        	//get template
        	vUa = "vvx310";
       		System.IO.StreamReader sr = new System.IO.StreamReader(HttpContext.Current.Server.MapPath("polycom/" + vUa + "-line-template.txt"));
 		configfile = sr.ReadToEnd();
       		sr.Close();
       		//LOGGING
                logging("success, boot_polycom, retrieve config template, " + provUser + ", " + vIp + ", " + vUa);
                
                
	//auth
        //ContextOptions options = ContextOptions.SimpleBind | ContextOptions.SecureSocketLayer;
        ContextOptions options = ContextOptions.Negotiate | ContextOptions.Sealing;
	//using(context = new PrincipalContext(ContextType.Domain, ldapserver, ldaptree , options, ldapuser , ldappass)) {//change to ou not including disabled
	using(context = new PrincipalContext(ContextType.Domain, ldapserver, null, options , ldapuser , ldappass)) {
	foreach (string user in userOnsip){
	
	//CONVERT EMAIL TO SAM
        UserPrincipal userPrincipal2 = new UserPrincipal(context);
        userPrincipal2.EmailAddress = user+"@*";//find only email address and any domain
        PrincipalSearcher srch = new PrincipalSearcher(userPrincipal2);
        var found = srch.FindOne();
        
	//GET ONLY USERS IN ASHOKA ACCOUNTS OR ASHOKA RESOURCES
        if(found != null && (found.DistinguishedName.Contains("Ashoka Accounts") || found.DistinguishedName.Contains("Ashoka Resources") || found.DistinguishedName.Contains("MSAs"))){
        	userfull = found.SamAccountName + "@ashoka.org";
        	UserPrincipal userPrincipal = UserPrincipal.FindByIdentity(context, userfull);
        	//LOGGING
		logging("success, boot_polycom, found userPrincipal " + provUser + ", " + vIp + ", " + vUa);
		
                DirectoryEntry de = (userPrincipal.GetUnderlyingObject() as DirectoryEntry);
                ldapfullname.Add(userPrincipal.DisplayName);
                ldapmail.Add(userPrincipal.EmailAddress);
                ldapextension.Add(userPrincipal.VoiceTelephoneNumber);
                ldapsip.Add(de.Properties["ipphone"].Value.ToString());
                int index2 = userPrincipal.DisplayName.IndexOf(" ");
		if (index2 == -1){index2 = userPrincipal.DisplayName.Length;}
                label.Add(userPrincipal.DisplayName.Substring(0, index2));
		
		if (de.Properties["ipphone"].Value.ToString()!=""){
	                //USER ONSIP AS DATA STORE
        	        XmlTextReader reader = new XmlTextReader("https://api.onsip.com/api?Action=SessionCreate&Username=" + onsipUser + "&Password=" + onsipPass);
	                reader.Read();
        	        XmlDocument xmlDoc = new XmlDocument();
	                xmlDoc.Load(reader);
	                XmlNodeList sId = xmlDoc.GetElementsByTagName("SessionId");
	                sesId = sId[0].InnerText;
	                //LOGGING
	                logging("success, boot_polycom, retrieve onsip session id, " + provUser + ", " + vIp + ", " + vUa);

	                XmlTextReader reader2 = new XmlTextReader("https://api.onsip.com/api?Action=UserRead&SessionId=" + sesId + "&UserAddress=" + ldapsip[x]);
	                reader2.Read();
        	        XmlDocument xmlDoc2 = new XmlDocument();
                	xmlDoc2.Load(reader2);
	                XmlNodeList pWd = xmlDoc2.GetElementsByTagName("Password");
	                ldapsippass.Add(pWd[0].InnerText);
			//AuthUsername
			XmlNodeList aUth = xmlDoc2.GetElementsByTagName("AuthUsername");
                	ldapsipauth.Add(aUth[0].InnerText);
			XmlNodeList uId = xmlDoc2.GetElementsByTagName("UserId");
                	ldapuserid = uId[0].InnerText;
			//LOGGING
	                logging("success, boot_polycom, retrieve onsip sip pass, " + provUser + ", " + vIp + ", " + vUa);

			//BROWSE E911 LOCATIONS AND SELECT BASED NO VLAN SEGMENT
			XmlTextReader reader3 = new XmlTextReader("https://api.onsip.com/api?Action=E911LocationBrowse&UserId=" + ldapuserid + "&SessionId=" + sesId);
			reader3.Read();
        	        XmlDocument xmlDoc3 = new XmlDocument();
                	xmlDoc3.Load(reader3);
                	//ITERATE OVER LocationName
                	XmlNodeList lNa = xmlDoc3.GetElementsByTagName("LocationName");
                	for (int i = 0; i < lNa.Count; i++)
            		{
				if (lNa[i].InnerText.Contains(vIp.Split('.')[2])){
					XmlNodeList lNaI = xmlDoc3.GetElementsByTagName("E911LocationId");
					lNaId = lNaI[i].InnerText;
				}
			}
			
			//SET E911 IN ONSIP FOR USER
			XmlTextReader reader4 = new XmlTextReader("https://api.onsip.com/api?Action=E911LocationAssign&Address=" + ldapsip[x] + "&E911LocationId=" + lNaId + "&SessionId=" + sesId);
			reader4.Read();
        	        //XmlDocument xmlDoc4 = new XmlDocument();
                	//xmlDoc4.Load(reader4);
			//LOGGING
	                logging("success, boot_polycom, set onsip e911, " + provUser + ", " + vIp + ", " + vUa);


            	}//if ldapsip 
		
		//REPLACE IN CONFIG
 		configfile = configfile.Replace("[USER"+ x +"]", userOnsip[x] + "@ashoka.onsip.com"); 
 		configfile = configfile.Replace("[SIP"+ x +"]", ldapsip[x]); //sip address
 		configfile = configfile.Replace("[FULLNAME"+ x +"]", ldapfullname[x]); //users full name
		configfile = configfile.Replace("[AUTH"+ x +"]", ldapsipauth[x]); //auth user onsip
		configfile = configfile.Replace("[LABEL"+ x +"]", label[x]); //label
		configfile = configfile.Replace("[PASS"+ x +"]", ldapsippass[x]); //sip password
		configfile = configfile.Replace("[LINES"+ x +"]", vUc[x].ToString()); //lines
        }//if found
        else
	{
		if(found != null){
			logging("error, boot_polycom, found user not in OU, " + user);
			sendIt("There is a polycom phone configured to have a user that is not valid: " + user + ", " +vIp);
		}
	}
        
        x=x+1;
        }//foreach
		
		
		configfile = configfile.Replace("[KEY]", vKey.ToString()); //c# key
		configfile = configfile.Replace("[TIME]", vHrs.ToString() + ":" + vMin.ToString() + vSec.ToString()); //label
		configfile = configfile.Replace("[LINEUSERS]", provUser); //line users

		//CLEAN UP ANY UNUSED VARIABLES IN TEH CONFIG FILE
 		configfile = Regex.Replace(configfile, @"\[[A-Z0-9]+\]",""); 
 		
       		//WRITE TO PAGE
       		Response.Write(configfile);
			
		//LOGGING
		logging("success, boot_polycom, write config to phone, " + provUser + ", " + vIp + ", " + vUa);
        
        }//using
        
	
        }///try
        catch (Exception ex)
        {
            Response.Write(ex.Message);
            logging("error, boot_polycom, " + ex.Message);
            sendIt("There was an error configuring a polycom phone, " + DateTime.Now.ToString("M/d/yyyy HH:mm:ss"));
            
        }
    }
   	

	public void logging(String logText){
   		try{
			System.IO.File.AppendAllText(@"C:\Logs\voipboot_"+ DateTime.Now.ToString("M-d-yyyy") + ".txt", logText + ", " + DateTime.Now.ToString("M/d/yyyy HH:mm:ss") + Environment.NewLine);
			//action
		}
		catch (Exception ex)
		{
			
		}
	}

	public void sendIt(String sendText){

		string from = "theFromAddress";
		string to = "theToAddress";
		MailMessage message = new MailMessage(from, to);
		message.Subject = "re: polycom phone error";
		message.Body = sendText;
		SmtpClient client = new SmtpClient("smtp.office365.com");
		client.Port = 587;
		client.EnableSsl = true;
		client.UseDefaultCredentials = false;
		NetworkCredential cred = new System.Net.NetworkCredential("theUserId", "thePassword"); 
		client.Credentials = cred;

	      	try {
			//client.Send(message);
			logging("success, boot_polycom, send mail completed, " + to);
		}  
		catch (Exception ex) {
			Console.WriteLine("Exception caught in CreateTestMessage2(): {0}", ex.ToString() );
			Response.Write("error: " + ex.ToString());
			logging("error, boot_polycom, sendmailerror, " + ex.Message);
      		}
	}
   
}