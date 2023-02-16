using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net;
using System.Text;
using System.Data.SqlClient;

public partial class servertemp : System.Web.UI.Page
{
    String vDriveId = "";

    protected async void Page_Load(object sender, EventArgs e)
    {

        String vKey = "theApiKey";
        

        if (String.IsNullOrEmpty(Request.Params["act"]) || String.IsNullOrEmpty(Request.Params["key"]))
        {
            Response.Clear();
            Response.ContentType = "application/json; charset=utf-8";
            Response.Write("{\"status\":\"failed\",\"message\":\"there is an issue with your request\"}");
            try
            {
                //Response.End();
                Response.Flush();
                Response.SuppressContent = true;
                ApplicationInstance.CompleteRequest();
            }
            catch (Exception exc)
            {

            }
        }
        if (Request.Params["key"] != vKey)
        {
            Response.Clear();
            Response.ContentType = "application/json; charset=utf-8";
            Response.Write("{\"status\":\"failed\",\"message\":\"invalid key\"}");
            try
            {
                //Response.End();
                Response.Flush();
                Response.SuppressContent = true;
                ApplicationInstance.CompleteRequest();
            }
            catch (Exception exc)
            {

            }
        }
        logIt("Server temp status check");

        var vAct = Request.Params["act"];
        var vAl = Request.Params["alr"];
        var vTe = Request.Params["temp"];
        
        if (vAct=="check")
        {
            getStatus();
        }
        else if(vAct=="pause")
        {
            pauseStatus();
        }
        else if(vAct=="clear")
        {
            clearStatus(vTe);
        }
        else if (vAct == "set")
        {
            setStatus(vTe,vAl);
        }
        else
        {
            logIt("act param incorrect");
        }
        
    }

    public void pauseStatus()
    {
        try
        {

            //CONNECT TO DB
            System.Data.SqlClient.SqlConnectionStringBuilder builder = new System.Data.SqlClient.SqlConnectionStringBuilder();
            builder["Server"] = "theAzureSqlServer";
            builder["User ID"] = "theUserId";
            builder["Password"] = "thePassword";
            builder["Database"] = "theDatabase";
            builder["Trusted_Connection"] = false;
            builder["Integrated Security"] = false;
            builder["Encrypt"] = true;

            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                connection.Open();
                var vState = "";
                var vPause = "";
                var vTemp = "";

                using (SqlCommand cmd = new SqlCommand("SELECT * FROM tblServTemp", connection))
                {
                    
                    SqlDataReader rdr = cmd.ExecuteReader();
                    
                    if (rdr.HasRows)
                    {
                        while (rdr.Read())
                        {
                            vState = rdr["status"].ToString();
                            vPause = rdr["pause"].ToString();
                            vTemp = rdr["temp"].ToString();
                        }   
                    }
                    rdr.Close();
                    rdr.Dispose();
                };
                if(vState=="1"){
                    using (SqlCommand cmd3 = new SqlCommand("UPDATE tblServTemp SET pause = @param1", connection))
                    {
                        cmd3.Parameters.AddWithValue("@param1", 1);
                        cmd3.ExecuteNonQuery();
                    }
                    Response.Write("{\"status\": "+vState+",\"pause\": 1,\"temp\": "+vTemp+"}");
                }else{
                    Response.Write("{\"status\": "+vState+",\"pause\": "+vPause+",\"temp\": "+vTemp+"}");
                }

                //CLOSE DB CONNECTION
                connection.Close();
                logIt("success, servertemp, updated status");
                
            }


        }
        catch (Exception ex)
        {
            logIt("error, servertemp, " + ex.Message);

        }
    }

    public void setStatus(string vT, string vA)
    {
        try
        {

            //CONNECT TO DB
            System.Data.SqlClient.SqlConnectionStringBuilder builder = new System.Data.SqlClient.SqlConnectionStringBuilder();
            builder["Server"] = "theAzureSqlServer";
            builder["User ID"] = "theUserId";
            builder["Password"] = "thePassword";
            builder["Database"] = "theDatabase";
            builder["Trusted_Connection"] = false;
            builder["Integrated Security"] = false;
            builder["Encrypt"] = true;

            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                connection.Open();

                var vState = "";
                var vPause = "";
                var vTemp = "";

                using (SqlCommand cmd3 = new SqlCommand("UPDATE tblServTemp SET status = @param1, temp = @param2", connection))
                {
                    cmd3.Parameters.AddWithValue("@param1", Int32.Parse(vA));
                    cmd3.Parameters.AddWithValue("@param2", float.Parse(vT));
                    cmd3.ExecuteNonQuery();
                }

                using (SqlCommand cmd = new SqlCommand("SELECT * FROM tblServTemp", connection))
                {
                    
                    SqlDataReader rdr = cmd.ExecuteReader();
                    
                    if (rdr.HasRows)
                    {
                        while (rdr.Read())
                        {
                            vState = rdr["status"].ToString();
                            vPause = rdr["pause"].ToString();
                            vTemp = rdr["temp"].ToString();
                        }   
                    }
                    rdr.Close();
                    rdr.Dispose();
                };

                //CLOSE DB CONNECTION
                connection.Close();
                logIt("success, servertemp, updated status");
                Response.Write("{\"status\": "+vState+",\"pause\": "+vPause+"}");
            }


        }
        catch (Exception ex)
        {
            logIt("error, servertemp, " + ex.Message);
            
        }


    }

    public void clearStatus(string vT)
    {
        try
        {

            //CONNECT TO DB
            System.Data.SqlClient.SqlConnectionStringBuilder builder = new System.Data.SqlClient.SqlConnectionStringBuilder();
            builder["Server"] = "theAzureSqlServer";
            builder["User ID"] = "theUserId";
            builder["Password"] = "thePassword";
            builder["Database"] = "theDatabase";
            builder["Trusted_Connection"] = false;
            builder["Integrated Security"] = false;
            builder["Encrypt"] = true;

            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                connection.Open();

                using (SqlCommand cmd3 = new SqlCommand("UPDATE tblServTemp SET status = @param1, pause = @param2, temp = @param3", connection))
                {
                    cmd3.Parameters.AddWithValue("@param1", 0);
                    cmd3.Parameters.AddWithValue("@param2", 0);
                    cmd3.Parameters.AddWithValue("@param3", float.Parse(vT));
                    cmd3.ExecuteNonQuery();
                }

                //CLOSE DB CONNECTION
                connection.Close();
                logIt("success, servertemp, updated status");
                Response.Write("{\"status\": 1}");
            }


        }
        catch (Exception ex)
        {
            logIt("error, servertemp, " + ex.Message);

        }
    }


    public void getStatus()
    {
        try
        {

            //CONNECT TO DB
            System.Data.SqlClient.SqlConnectionStringBuilder builder = new System.Data.SqlClient.SqlConnectionStringBuilder();
            builder["Server"] = "theAzureSqlServer";
            builder["User ID"] = "theUserId";
            builder["Password"] = "thePassword";
            builder["Database"] = "theDatabase";
            builder["Trusted_Connection"] = false;
            builder["Integrated Security"] = false;
            builder["Encrypt"] = true;

            string vState = "";
            string vPause = "";
            string vTemp = "";

            using (SqlConnection connection = new SqlConnection(builder.ConnectionString))
            {
                connection.Open();
                using (SqlCommand cmd = new SqlCommand("SELECT * FROM tblServTemp", connection))
                {
                    
                    SqlDataReader rdr = cmd.ExecuteReader();
                    
                    if (rdr.HasRows)
                    {
                        while (rdr.Read())
                        {
                            vState = rdr["status"].ToString();
                            vPause = rdr["pause"].ToString();
                            vTemp = rdr["temp"].ToString();
                        }   
                    }
                    rdr.Close();
                    rdr.Dispose();
                };


                //CLOSE DB CONNECTION
                connection.Close();
                logIt("success, got alert status");
                Response.Write("{\"status\": "+vState+",\"pause\": "+vPause+",\"temp\": "+vTemp+"}");
            }


        }
        catch (Exception ex)
        {
            logIt("error, get alert status, " + ex.Message);
            
        }
    }

   

    void returnIt(String ch, String fn)
    {
        logIt("Return file uri");
        Response.Clear();
        Response.ContentType = "application/json; charset=utf-8";
        Response.Write("{\"status\":\"success\",\"message\":{\"url\":\"" + ch + "\",\"filename\":\"" + fn + "\"}}");
        try
        {
            Response.End();
        }
        catch (Exception exc)
        {

        }
    }

    public void errorIt(Exception logText, String ms)
    {
        System.IO.File.AppendAllText(@"C:\Logs\servertemp_" + DateTime.Now.ToString("M-d-yyyy") + ".txt", logText.Message + ", " + DateTime.Now.ToString("M/d/yyyy HH:mm:ss") + Environment.NewLine);
        string msg = ms.Replace("\\", "/");
        Response.Clear();
        Response.ContentType = "application/json; charset=utf-8";
        Response.Write("{\"status\":\"failed\",\"message\":\"" + msg + "\"}");
        try
        {
            Response.End();
        }
        catch (Exception exc)
        {

        }
    }

    public void logIt(String ms)
    {
        try
        {
            System.IO.File.AppendAllText(@"C:\Logs\servertemp_" + DateTime.Now.ToString("M-d-yyyy") + ".txt", ms + ", " + DateTime.Now.ToString("M/d/yyyy HH:mm:ss") + Environment.NewLine);
        }
        catch (Exception e)
        {

        }
    }

}