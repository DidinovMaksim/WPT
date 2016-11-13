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
        public int MinResp { get; set; }
        public int MaxResp { get; set; }
        public int AvgResp { get; set; }
        public List<Node> ChildNodes { get; set; }
        /*public Node ParentNode
        {
            // Убрал из-за того, что сериализация не умеет хэндлить лупы
            get { return parentNode; }
            set { parentNode = value; }
        }*/
        public string Url { get; set; }
        public string Display { get; set; }
        public bool IsMeasured { get; set; }
        public string FullUrl { get; set; }
        public bool IsParsed { get; set; }

        Node parentNode;
        
        public Node()
        {
            parentNode = null;
            MinResp = MaxResp = AvgResp = 0;
            ChildNodes = new List<Node>();
            parentNode = null;
            IsParsed = false;
            IsMeasured = false;
            Url = "";
            FullUrl = "";
            Display = "";
        }
        public Node(string Url):this()
        {           
            this.Url = Url;
            MakeFullUrl();
        }
        public Node(Node parent, string Url):this(Url)
        {
            parentNode = parent;
            MakeFullUrl();
        }
        public void AddNode(string Url)
        {
            if (!ChildNodeExists(Url))
                ChildNodes.Add(new Node(this, Url));
        }
        public void MakeFullUrl()
        {
            FullUrl = Url;
            Node tmp = parentNode;
            while (tmp != null)
            {
                FullUrl = tmp.Url + FullUrl;
                tmp = tmp.GetParentNode();
            }
            //FullUrl = "http://" + FullUrl;// Services.getCorrectUri(parentNode;
        }
        public void MakeSpeedMeasurments(int count)
        {
            int[] respTime = new int[count];
            
            try
            {
                for (int i = 0; i < count; i++)
                {
                    HttpWebRequest request = (HttpWebRequest)WebRequest.Create(Services.getCorrectUri(FullUrl));//FullUrl);
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
                MinResp = respTime.Min();
                MaxResp = respTime.Max();
                for (int i = 0; i < count; i++)
                    AvgResp += respTime[i];

                AvgResp /= count;
                IsMeasured = true;
            }
            catch(Exception ex)
            {
                IsMeasured = true;
            }
            
            GenerateDisplayString();

        }
        public bool ChildNodeExists(string Url)
        {
            for (int i = 0; i < ChildNodes.Count; i++)
                if (string.Compare(Url, ChildNodes[i].Url) == 0)
                    return true;

            return false;
        }
        public Node GetChildNode(string Url)
        {
            for (int i = 0; i < ChildNodes.Count; i++)
                if (string.Compare(Url, ChildNodes[i].Url) == 0)
                    return ChildNodes[i];

            return null;
        }
        public Node GetParentNode()
        {
            return parentNode;
        }        
        public override string ToString()
        {
            return Url + "Min: " + MinResp + " Max: " + MaxResp;
        }
        public void GenerateDisplayString()
        {
            if(MinResp !=0 && MaxResp !=0 && AvgResp != 0)
            Display = Url + " || Min: " + MinResp + " Max: " + MaxResp + " Avg: " + AvgResp + " Children: " + ChildNodes.Count;
            else
                Display = Url + " || Children: " + ChildNodes.Count;
        }
        public Node getCopy(bool includeParent = false, bool includeChild = false)
        {
            return new Node()
            {
                Url = Url,
                MinResp = MinResp,
                MaxResp = MaxResp,
                AvgResp = AvgResp,
                FullUrl = FullUrl,
                IsParsed = IsParsed
            };
        }
    }
}

