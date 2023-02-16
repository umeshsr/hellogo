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




public partial class boot_polycom_main : System.Web.UI.Page
{

    protected void Page_Load(object sender, EventArgs e)
    {
        try
        {

		ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
	        //CONFIGURE VARIABLES
		//string ldapserver = "auth.ashoka.org:636"; //SECURE LDAP SERVER
String ldapserver = "theLdapServer";
            	//int ldapport = 636;
            	string ldaptree = "theLdapPath";
           	string ldapuser = "theLdapUser";
            	string ldappass = "theLdapPass";

            	string configfile = string.Empty;
            	String ldapextension;
            	String ldapfullname;
            	String ldapmail;
           	String ldapsip;
            	String ldapsippass;
		String ldapsipauth;
            	String label;
            	PrincipalContext context;
            	String sesId;
		String useragent = "";
		String onsipPass = "theOnsipPass";
	        String onsipUser = "theOnsipUser";
        	String vIp = Request.UserHostAddress;
	        String vUa = Request.UserAgent;
        	String vTemplatePath = "";
        	long vKey = theApiKey;
       	 	Random rnd = new Random();
        	int vHrs = rnd.Next(1, 3);
        	int vMin = rnd.Next(0, 5);
       	 	int vSec = rnd.Next(0, 9);

     
        	//TEST POST FOR REQUIRED PARAMS
        	if ((String.IsNullOrEmpty(vUa)) || (String.IsNullOrEmpty(Request.Params["key"])) || (Convert.ToInt64(Request.Params["key"])!=vKey))
        	{
        	        //IF FAIL TEST THEN END
	                Response.End();

        	}

			try{
			//ContextOptions options = ContextOptions.SimpleBind | ContextOptions.SecureSocketLayer;
			ContextOptions options = ContextOptions.Negotiate | ContextOptions.Sealing;
        		//using(context = new PrincipalContext(ContextType.Domain, ldapserver, ldaptree , options, ldapuser , ldappass)) {
			using(context = new PrincipalContext(ContextType.Domain, ldapserver, null, options , ldapuser , ldappass)) {
			}
			logging("success, boot_polycom_main, ldap available, " + vIp);
		}
		catch (Exception er){
			//WRITE TO LOG
			logging("error, boot_polycom_main, ldap not available, " + vIp);
			
			//IF FAIL TEST THEN END
	                Response.End();
		}


	        //WRITE TO LOG
		logging("success, boot_polycom_main, retrieve config template, " + vUa);

		//SET TEMPLATE PATH BASED ON USER AGENT FROM PHONE CONTAINS
		//FileTransport PolycomVVX-VVX_310-UA/5.1.1.2986
		if (vUa.Contains("VVX_310")){
			vTemplatePath = "polycom/vvx310-main-template.txt";
			useragent = "vvx310";
		}
		else
		{
			Response.End();
		}

		//LOAD THE CONFIGURATION TEMPLATE			
       		using(System.IO.StreamReader sr = new System.IO.StreamReader(HttpContext.Current.Server.MapPath(vTemplatePath))){
       			configfile = sr.ReadToEnd();
        		sr.Close();
	        }

	        //WRITE TO LOG
		logging("success, boot_polycom_main, retrieve config template, " + vTemplatePath);
			
		//REPLACE VARIABLES IN CONFIG. CURRENTLY NOT USED 
	      	//configfile = configfile.Replace("[USERAGENT]", useragent); 

       		//WRITE TO PHONE
	        Response.Write(configfile);
			
		//WRITE TO LOG
		logging("success, boot_polycom_main, write config to phone, " + useragent);

       
        }///try
        catch (Exception ex)
        {
            //Response.Write(ex.Message);
            logging("error, boot_polycom_main, " + ex.Message);
            //throw;
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
   
}