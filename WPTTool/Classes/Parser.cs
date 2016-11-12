using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace WPTTool.Classes
{
    public class Parser
    {
        
        
        static public string[] getAllHref(string url, bool checkFromRoot = false, bool checkOnlyDeeper = false)
        {

            Uri uri = Services.getCorrectUri(url);
            if (uri != null)
            {
                if (!uri.AbsoluteUri.Contains(url)) //redirect 
                    return new string[] { };
            }
            else return new string[] { };

            string startUrl;
            string[] href;

            startUrl = checkFromRoot ? uri.Host.Replace("www.", "") : uri.AbsoluteUri;
            href = GetHref(startUrl, checkOnlyDeeper);
            href = CleanHref(href);
            href = AddHostToRoot(href, uri.Scheme + "://" + uri.Host);
            href = CleanDuplicatesSorted(href);

            return href;
        }
        static string GeneratePattern(string url, bool checkOnlyDeeper = false)
        {
            Uri uri = Services.getCorrectUri(url);
            string segments = "";
            for (int i = 0; i < uri.Segments.Count(); i++)
                segments += uri.Segments[i];
            segments += uri.Query;

            string ex = "\\.css|\\.js|\\.ico|\\.jpg";
            string cleanHost = uri.Host.Replace("www.", "").Replace(".", "\\.");
            string address = "";
            if (!checkOnlyDeeper)
                address = "((https?://)(www\\.)?(\\S*\\.)*(" + cleanHost + ")(\\S*)|[\\.]?/\\S*)"; // https://facebook.com/asd.olx.ua НЕ АРБИТЕН!!!1!!!!
            else

                address = "((" + uri.AbsoluteUri.Replace(".", "\\.") + ")(\\S*)|[\\.]?" + segments + "\\S*)";

            string pattern = "href\\s*=\\s*(?:[\"'](?<a>" + address + "(?<!" + ex + ")))[\"']";

            //string pattern = "href\\s*=\\s*(?:[\"'](?<a>[^\"']*(?<!" + ex + ")))[\"']";
            return pattern;
        }
        static string[] GetHref(string url, bool checkOnlyDeeper = false)
        {
            Uri uri = Services.getCorrectUri(url);

            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri.AbsoluteUri);

            request.Method = "GET";
            request.UserAgent = "TestAgent";
            request.Accept = "text/html";
            request.AllowAutoRedirect = false;

            request.UseDefaultCredentials = true;
            //request.Accept = "*/*";

            HttpWebResponse response;
            string responseFromServer = "";
            bool suceed = false;
            while (!suceed)
            {
                try
                {
                    response = (HttpWebResponse)request.GetResponse();
                    Stream dataStream = response.GetResponseStream();
                    StreamReader reader = new StreamReader(dataStream);
                    responseFromServer = reader.ReadToEnd();

                    reader.Close();
                    dataStream.Close();
                    response.Close();
                    request.Abort();
                    suceed = true;
                }
                catch (Exception ex)
                {
                    return new string[] { };
                }
            }
            //string responseFromServer = new StreamReader("D:\\Dagger 2.html").ReadToEnd();

            MatchCollection m;
            m = Regex.Matches(responseFromServer, GeneratePattern(url, checkOnlyDeeper), RegexOptions.Compiled);
            string[] href = new string[m.Count];
            for (int i = 0; i < m.Count; i++)
                href[i] = m[i].Value;

            return href;
        }
        static string[] CleanHref(string[] Href)
        {
            for (int i = 0; i < Href.Length; i++)
            {
                while (Href[i].Contains(" "))
                    Href[i] = Href[i].Replace(" ", "");

                Href[i] = Href[i].Replace("href=", "");
                //Href[i] = Href[i].Replace("www.", "");
                Href[i] = Href[i].Replace("\"", "");
                Href[i] = Href[i].Replace("'", "");

                //Href[i] = Href[i].Replace("http://", "");
                //Href[i] = Href[i].Replace("https://", "");
            }
            return Href;
        }
        
        static string[] AddHostToRoot(string[] href, string host)
        {
            for (int i = 0; i < href.Length; i++)
            {
                if (href[i][0] == '.')
                    href[i] = href[i].Remove(0, 1);
                if (href[i][0] == '/')
                    href[i] = host + href[i];
            }


            return href;
        }
        static string[] CleanDuplicates(string[] href)
        {
            bool[] Del = new bool[href.Length];
            int count = 0;
            for (int i = 0; i < href.Length; i++)
            {
                for (int j = i + 1; j < href.Length; j++)
                {
                    if (string.Compare(href[i], href[j]) == 0 && !Del[j])
                    {
                        Del[j] = true;
                        count++;
                    }
                    /*else
                        if(isSorted)
                        { 
                            i = j - 1;
                            break;
                        }*/
                }
            }

            string[] newHref = new string[href.Length - count];
            int shift = 0;
            for (int i = 0; i < newHref.Length;)
            {
                if (!Del[i + shift])
                {
                    newHref[i] = href[i + shift];
                    i++;
                }
                else
                    shift++;
            }

            return newHref;
        }
        static string[] CleanDuplicatesSorted(string[] href)
        {
            //строго отсортированный, быстрее фыр-фыр

            Array.Sort<string>(href);
            bool[] Del = new bool[href.Length];
            int count = 0;
            for (int i = 0; i < href.Length; i++)
            {
                for (int j = i + 1; j < href.Length; j++)
                {
                    if (string.Compare(href[i], href[j]) == 0)
                    {
                        Del[j] = true;
                        count++;
                    }
                    else
                    {
                        i = j - 1;
                        break;
                    }
                    if (j == href.Length - 1)
                        i = j;
                }
            }

            string[] newHref = new string[href.Length - count];
            int shift = 0;
            for (int i = 0; i < newHref.Length;)
            {
                if (!Del[i + shift])
                {
                    newHref[i] = href[i + shift];
                    i++;
                }
                else
                    shift++;
            }

            return newHref;
        }
    }
}

