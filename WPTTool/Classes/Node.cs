using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;

namespace WPTTool.Classes
{
    [Serializable]
    public class Node
    {
        int minResp,
            maxResp,
            avgResp;
        string url, fullUrl, display;
        List<Node> childNodes;
        Node parentNode;
        bool isParsed, isMeasured;
        public Node()
        {
            parentNode = null;
            minResp = maxResp = avgResp = 0;
            childNodes = new List<Node>();
            parentNode = null;
            isParsed = false;
            isMeasured = false;
            url = "";
            fullUrl = "";
            display = "";

        }
        public Node(string url):this()
        {           
            this.url = url;
            MakeFullUrl();
        }
        public Node(Node parent, string url):this(url)
        {
            parentNode = parent;
            MakeFullUrl();
        }
        public void AddNode(string url)
        {
            if (!ChildNodeExists(url))
                childNodes.Add(new Node(this, url));
        }
        public void MakeFullUrl()
        {
            fullUrl = url;
            Node tmp = parentNode;
            while (tmp != null)
            {
                fullUrl = tmp.url + fullUrl;
                tmp = tmp.GetParentNode();
            }
            //fullUrl = "http://" + fullUrl;// Services.getCorrectUri(parentNode;
        }
        public void MakeSpeedMeasurments(int count)
        {
            int[] respTime = new int[count];

            //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(fullUrl);
            //Stopwatch timer = new Stopwatch();

            for (int i = 0; i < count; i++)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Services.getCorrectUri(FullUrl));//fullUrl);
                Stopwatch timer = new Stopwatch();

                bool suceed = false;
                //while (!suceed) // Тут иногда может умирать. Нужно что то придумать
                    try
                    {
                        timer.Start();
                        request.GetResponse();
                        timer.Stop();
                        suceed = true;
                    }
                    catch (Exception ex)
                    {
                        timer.Stop();
                        //timer.Reset();
                    }
                respTime[i] = timer.Elapsed.Milliseconds;

                request.Abort();
            }
            minResp = respTime.Min();
            maxResp = respTime.Max();
            for (int i = 0; i < count; i++)
                avgResp += respTime[i];

            avgResp /= count;
            isMeasured = true;
            GenerateDisplayString();

        }
        public bool ChildNodeExists(string url)
        {
            for (int i = 0; i < childNodes.Count; i++)
                if (string.Compare(url, childNodes[i].Url) == 0)
                    return true;

            return false;
        }
        public Node GetChildNode(string url)
        {
            for (int i = 0; i < childNodes.Count; i++)
                if (string.Compare(url, childNodes[i].Url) == 0)
                    return childNodes[i];

            return null;
        }
        public Node GetParentNode()
        {
            return parentNode;
        }
        public int MinResp
        {
            get { return minResp; }
            set { minResp = value; }
        }
        public int MaxResp
        {
            get { return maxResp; }
            set { maxResp = value; }
        }
        public int AvgResp
        {
            get { return avgResp; }
            set { avgResp = value; }
        }
        public List<Node> ChildNodes
        {
            get { return childNodes; }
            set { childNodes = value; }
        }
        /*public Node ParentNode
        {
            // Убрал из-за того, что сериализация не умеет хэндлить лупы
            get { return parentNode; }
            set { parentNode = value; }
        }*/
        public string Url
        {
            get { return url; }
            set { url = value; } // Сеттер здесь по всем принципам явно лишний, но он нужен для сериализации. Как быть?
        }
        public string Display
        {
            get { return display; }
            set { display = value; }
        }
        public bool IsMeasured
        {
            get { return isMeasured; }
            set { isMeasured = value; }
        }
        public string FullUrl
        {
            get { return fullUrl; }
            set { fullUrl = value; }
        }
        public bool IsParsed
        {
            get { return isParsed; }
            set { isParsed = value; }
        }
        public override string ToString()
        {
            return url + "Min: " + minResp + " Max: " + maxResp;
        }
        public void GenerateDisplayString()
        {
            if(minResp !=0 && maxResp !=0 && AvgResp != 0)
            display = url + " || Min: " + minResp + " Max: " + maxResp + " Avg: " + avgResp + " Children: " + childNodes.Count;
            else
                display = url + " || Children: " + childNodes.Count;
        }
        public Node getCopy(bool includeParent = false, bool includeChild = false)
        {
            return new Node()
            {
                url = url,
                minResp = minResp,
                maxResp = maxResp,
                avgResp = avgResp,
                fullUrl = fullUrl,
                isParsed = isParsed

            };
        }
    }
}

