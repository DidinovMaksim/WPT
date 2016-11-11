using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;

namespace WPTTool.Classes
{
    public class Parser
    {
        static public string[] ParseAllHref(string Href, bool checkWholeWebsite = true)
        {
            string[] allHref = getAllHref(Href, checkWholeWebsite);
            string[] temp;
            for (int i = 0; i < allHref.Count(); i++)
            {
                temp = getAllHref(allHref[i], checkWholeWebsite);
                allHref = Concat(allHref, temp);
                allHref = CleanDuplicates(allHref);
            }
            return allHref;
        }
        static Uri getCorrectUri(string url)
        {
            try
            {
                UriBuilder ub = new UriBuilder(url);
                HttpWebRequest req = (HttpWebRequest)WebRequest.Create(ub.Uri.AbsoluteUri);

                req.Method = "GET";
                req.UserAgent = "TestAgent";
                req.Accept = "text/html";

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
            //catch(uri)
        }
        static public string[] getAllHref(string url, bool checkFromRoot = false, bool checkOnlyDeeper = false)
        {

            Uri uri = getCorrectUri(url);
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
            //Array.Sort(href);
            //href = checkWholeWebsite ? GetActualHref(href, startUrl /*uri.Host*/) : GetActualHrefDeeper(href, startUrl);
            href = AddHostToRoot(href, uri.Scheme + "://" + uri.Host);
            href = CleanDuplicatesSorted(href);

            return href;
        }
        static string GeneratePattern(string url, bool checkOnlyDeeper = false)
        {
            Uri uri = getCorrectUri(url);
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
            Uri uri = getCorrectUri(url);

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
        /*

            адский индокод, поправил регуляркой
        
            static string[] GetActualHref(string[] href, string host)
        {
            

            bool[] Actual = new bool[href.Length];
            int cPositive = 0;
            string cur = "";
            string reg = @"^(((http(s)://)(www\.)(\S*\.)*"+ host.Replace(".", "\\.") + "\\S*)|/\\S*)";
            for (int i = 0; i < href.Length; i++)
            {
                cur = href[i].Contains("?") ? href[i].Substring(0, href[i].IndexOf("?")) : href[i];
                if (string.IsNullOrEmpty(href[i]))
                {
                    Actual[i] = false;
                    continue;
                }
                //Uri tmpUri = getCorrectUri(cur);
                //if (tmpUri != null ? tmpUri.Host.Contains(host) : false || (cur[0] == '/' && cur.IndexOf("//") != 0))
                if (Regex.Match(href[i], reg).Success)//cur.Contains(host) || (cur[0] == '/' && cur.IndexOf("//") != 0))
                {
                    //x-zibit mode
                    //facebook.com/olx.ua ^
                    cPositive++;
                    Actual[i] = true;
                }
                else
                    Actual[i] = false;
            }

            string[] newHref = new string[cPositive];
            int shift = 0;
            for (int i = 0; i < cPositive;)
            {
                if (Actual[i + shift])
                {
                    newHref[i] = href[i + shift];
                    i++;
                }
                else
                    shift++;
            }

            return newHref;
        }
        static string[] GetActualHrefDeeper(string[] href, string fullUrl)
        {
            Uri url = getCorrectUri(fullUrl);

            string segments = "";
            for (int i = 0; i < url.Segments.Count(); i++)
                segments += url.Segments[i];
            segments += url.Query;

            bool[] Actual = new bool[href.Length];
            int cPositive = 0;
            string cur = "";
            string reg = "^(" + url.AbsoluteUri.Replace(".", "\\.") + ".*" + "|" + segments + ".*)";
            //Console.WriteLine(reg);
            //string reg =  url.AbsoluteUri + ".*";


            for (int i = 0; i < href.Length; i++)
            {
                cur = href[i].Contains("?") ? href[i].Substring(0, href[i].IndexOf("?")) : href[i];
                if (Regex.Match(href[i], reg).Success)//href[i].Contains(url.AbsoluteUri) || href[i].Contains(segments)) // www.olx.ua/  /
                {
                    cPositive++;
                    Actual[i] = true;
                }
                else
                    Actual[i] = false;
            }

            string[] newHref = new string[cPositive];
            int shift = 0;
            for (int i = 0; i < cPositive;)
            {
                if (Actual[i + shift])
                {
                    newHref[i] = href[i + shift];
                    i++;
                }
                else
                    shift++;
            }

            return newHref;
        }*/
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
        static string[] Concat(string[] h1, string[] h2)
        {
            string[] newH = new string[h1.Count() + h2.Count()];
            for (int i = 0; i < h1.Count(); i++)
                newH[i] = h1[i];
            for (int i = 0; i < h2.Count(); i++)
                newH[h1.Count() + i] = h2[i];

            return newH;
        }
    }
}

