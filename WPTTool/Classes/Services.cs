using System;
using System.Net;

namespace WPTTool.Classes
{
    public class Services
    {
        static public Uri getCorrectUri(string url, bool AutoRedirect = true)
        {
            try
            {
                UriBuilder ub = new UriBuilder(url);
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(ub.Uri.AbsoluteUri);

                req.Method = "GET";
                req.UserAgent = "TestAgent";
                req.Accept = "text/html";
                req.AllowAutoRedirect = AutoRedirect;

                req.UseDefaultCredentials = true;
                HttpWebResponse resp = (HttpWebResponse)req.GetResponse();

                Uri uri = resp.ResponseUri;
                req.Abort();

                return uri;
            }
            catch (WebException ex)
            {
                if (ex.Response != null)
                    return ex.Response.ResponseUri;
                return null;
                //return null;
            }
            catch(Exception ex)
            {
                return null;
            }
        }
    }
}

