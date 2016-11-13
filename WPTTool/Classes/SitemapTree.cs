using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WPTTool.Classes
{
    [Serializable]
    public class SitemapTree
    {
        public enum CheckParse { WholeTree, From, To, FromTo, SpecificLevel };
        public object _lock = new object();
        

        //int pagesParsing = 0;
        string curParsing = "",
            curMeasuring = "";

        
        List<Node>  AllNodes;

        public SitemapTree()
        {
            // Конструктор сугубо без параметров нужно для сериализации. Необязательный параметры он все равно воспринмиает как обязательные.
            MeasureCount = 5;
            RootNodes = new List<Node>();
            AllNodes = new List<Node>();
            ParseParams = false;
            CheckOnlyDeeper = false;
            MaxThreads = Environment.ProcessorCount;
        }
        public SitemapTree(bool ParseParams = false, bool CheckOnlyDeeper = false, int limit = 0) : this()
        {

            this.ParseParams = ParseParams;
            this.CheckOnlyDeeper = CheckOnlyDeeper;
            TotalLimit = limit;
        }
        public SitemapTree(string url, bool ParseParams = false, bool CheckOnlyDeeper = false, int limit = 0) :
            this(ParseParams, CheckOnlyDeeper, limit)
        {
            Uri uri = Services.getCorrectUri(url);
            if (uri != null)
                AddNodesFromStrings(new string[] { uri.AbsoluteUri }, this.CheckOnlyDeeper);
        }
        public Node GetRootNode(string url)
        {
            for (int i = 0; i < RootNodes.Count; i++)
                if (string.Compare(RootNodes[i].Url, url) == 0)
                    return RootNodes[i];

            return null;
        }
        public void AddNodesFromStrings(string[] href, bool CheckOnlyDeeper = false)
        {
            if (href.Count() == 0) return;
            Uri tmpUri = new Uri(href[0]);
            Node curNode;
            int minDepth = tmpUri.Segments.Count() - 1;
            if (ParseParams)
                minDepth += (!string.IsNullOrEmpty(tmpUri.Query) ? 1 : 0);
            int curDepth = 0;
            int added = 0;
            for (int i = 0; i < href.Length; i++)
            {
                if (TotalLimit > 0)
                    if (!(TotalCount + added < TotalLimit))
                        break;

                tmpUri = new Uri(href[i]);
                if (tmpUri == null) continue;

                curDepth = tmpUri.Segments.Count() - 1;
                if (ParseParams)
                    curDepth += !string.IsNullOrEmpty(tmpUri.Query) ? 1 : 0;

                if (curDepth < minDepth)
                    minDepth = curDepth;

                if (GetRootNode(tmpUri.Host + tmpUri.Segments[0]) == null)
                {
                    RootNodes.Add(new Node(tmpUri.Host + tmpUri.Segments[0]));
                    added++;
                }
                curNode = GetRootNode(tmpUri.Host + tmpUri.Segments[0]);

                for (int j = 1; j < tmpUri.Segments.Length; j++)
                {
                    if (!curNode.ChildNodeExists(tmpUri.Segments[j]))
                    {
                        curNode.AddNode(tmpUri.Segments[j]);
                        added++;
                    }

                    curNode = curNode.GetChildNode(tmpUri.Segments[j]);
                }
                if (ParseParams)
                    if (!string.IsNullOrEmpty(tmpUri.Query))
                        if (!curNode.ChildNodeExists(tmpUri.Query))
                        {
                            curNode.AddNode(tmpUri.Query);
                            added++;
                        }
            }

            if (CheckOnlyDeeper)
            {
                for (int i = 0; i < RootNodes.Count; i++)
                {
                    RootNodes[i].IsParsed = true;
                    MarkParsed(RootNodes[i], 1, minDepth);
                }
            }
            CalcTotal();
            PrepareToPrint();
        }
        void MarkParsed(Node node, int curDepth, int Depth)
        {
            if (curDepth < Depth)
            {
                for (int i = 0; i < node.ChildNodes.Count; i++)
                    node.ChildNodes[i].IsParsed = true;

                for (int i = 0; i < node.ChildNodes.Count; i++)
                    MarkParsed(node.ChildNodes[i], curDepth + 1, Depth);
            }
        }
        void ParseSpecificNode(Node node)
        {
            if (!node.IsParsed)
            {
                lock (_lock)
                {
                    curParsing = node.FullUrl;
                }
                Console.WriteLine("T:" + Task.CurrentId + "  " + TotalParsed + "/" + TotalCount + " " + node.FullUrl);
                string[] href = Parser.getAllHref(node.FullUrl, checkOnlyDeeper: CheckOnlyDeeper);
                lock (_lock)
                {
                    AddNodesFromStrings(href);
                    node.IsParsed = true;
                    CalcTotal();
                }
            }
        }
        void MeasureSpecificNode(Node node)
        {
            if (!node.IsMeasured)
            {
                lock (_lock)
                {
                    curMeasuring = node.FullUrl;
                }
                //Console.WriteLine("T:" + Task.CurrentId + "  " + TotalMeasured + "/" + TotalCount + " " + node.FullUrl);
                System.Diagnostics.Debug.WriteLine("T:" + Task.CurrentId + "  " + TotalMeasured + "/" + TotalCount + " " + node.FullUrl);
                node.MakeSpeedMeasurments(MeasureCount);
                lock (_lock)
                {
                    CalcTotal();
                }
            }
        }
        public void CalcTotal()
        {
            TotalCount = 0;
            TotalNotParsed = 0;
            TotalParsed = 0;
            TotalMeasured = 0;
            TotalNotMeasured = 0;

            for (int i = 0; i < RootNodes.Count; i++)
            {
                CountChilds(RootNodes[i]);
            }
            TotalParsed = TotalCount - TotalNotParsed;
            TotalMeasured = TotalCount - TotalNotMeasured;
        }
        void CountChilds(Node node)
        {
            TotalCount += 1;
            TotalNotParsed += node.IsParsed ? 0 : 1;
            TotalNotMeasured += node.IsMeasured ? 0 : 1;

            for (int i = 0; i < node.ChildNodes.Count; i++)
                CountChilds(node.ChildNodes[i]);
        }
        public int CalcMaxDepth()
        {
            int maxDepth = 0, temp = 0;
            for (int i = 0; i < RootNodes.Count; i++)
            {
                temp = NodeDepth(RootNodes[i]);
                if (temp > maxDepth)
                    maxDepth = temp;
            }
            return maxDepth;
        }
        int NodeDepth(Node node, int Depth = 0)
        {
            if (node.ChildNodes.Count == 0)
                return Depth;
            int temp;
            for (int i = 0; i < node.ChildNodes.Count; i++)
            {
                temp = NodeDepth(node.ChildNodes[i], Depth + 1);
                if (Depth < temp)
                    Depth = temp;
            }
            return Depth;
        }
        void GenerateNotMeasuredList()
        {
            AllNodes.Clear();
            for (int i = 0; i < RootNodes.Count; i++)
            {
                if (!RootNodes[i].IsMeasured)
                    AllNodes.Add(RootNodes[i]);
                AllNodes.AddRange(getNotMeasuredChildren(RootNodes[i]));
            }
        }
        List<Node> getNotMeasuredChildren(Node child, List<Node> childNodesList = null)
        {
            if (childNodesList == null)
                childNodesList = new List<Node>();

            for (int i = 0; i < child.ChildNodes.Count; i++)
                if (!child.ChildNodes[i].IsMeasured)
                    childNodesList.Add(child.ChildNodes[i]);//.getCopy());

            for (int i = 0; i < child.ChildNodes.Count; i++)
            {
                getNotMeasuredChildren(child.ChildNodes[i], childNodesList);
            }
            return childNodesList;
        }
        public List<Node> getRootNodes()
        {
            return RootNodes;
        }
        public void GenerateNotParsedList()
        {
            AllNodes.Clear();
            for (int i = 0; i < RootNodes.Count; i++)
            {
                if (!RootNodes[i].IsParsed)
                    AllNodes.Add(RootNodes[i]);//.getCopy());

                AllNodes.AddRange(getNotParsedChildren(RootNodes[i]));
            }
        }
        List<Node> getNotParsedChildren(Node child, List<Node> childNodesList = null)
        {
            if (childNodesList == null)
                childNodesList = new List<Node>();

            for (int i = 0; i < child.ChildNodes.Count; i++)
                if (!child.ChildNodes[i].IsParsed)
                    childNodesList.Add(child.ChildNodes[i]);//.getCopy());

            for (int i = 0; i < child.ChildNodes.Count; i++)
            {
                getNotParsedChildren(child.ChildNodes[i], childNodesList);
            }
            return childNodesList;
        }
        public void ParseAllNodesWithTasks()
        {
            GenerateNotParsedList();

            int taskCount = MaxThreads < AllNodes.Count ? MaxThreads : AllNodes.Count;
            Task[] t = new Task[taskCount];
            /*for(int i = 0; i < AllNodes.Count; i++)
            {
                ParseSpecificNode(AllNodes[i]);
            }*/
            for (int i = 0; i < AllNodes.Count; i++)
            {
                Node temp = AllNodes[i];
                if (t[i % MaxThreads] == null)
                    t[i % MaxThreads] = new Task(() => ParseSpecificNode(temp));
                else
                    t[i % MaxThreads].ContinueWith(new Action<Task>((par) => ParseSpecificNode(temp)));
            }
            for (int i = 0; i < taskCount; i++)
            {
                t[i].Start();
            }
            Task.WaitAll(t);

            if (TotalNotParsed != 0)
                ParseAllNodesWithTasks();

            PrepareToPrint();
            curParsing = "";
        }
        public void MeasureAllNodesWithTasks()
        {
            GenerateNotMeasuredList();

            int taskCount = MaxThreads < AllNodes.Count ? MaxThreads : AllNodes.Count;
            Task[] t = new Task[taskCount];

            /*for (int i = 0; i < AllNodes.Count; i++)
            {
                MeasureSpecificNode(AllNodes[i]);
            }*/
            for (int i = 0; i < AllNodes.Count; i++)
            {
                //pagesParsing++;
                Node temp = AllNodes[i];
                if (t[i % MaxThreads] == null)
                    t[i % MaxThreads] = new Task(() => MeasureSpecificNode(temp));
                else
                    t[i % MaxThreads].ContinueWith(new Action<Task>((par) => MeasureSpecificNode(temp)));
            }
            for (int i = 0; i < taskCount; i++)
            {
                t[i].Start();
            }
            Task.WaitAll(t);
            if (TotalNotMeasured != 0)
                MeasureAllNodesWithTasks();

            PrepareToPrint();
            curMeasuring = "";
        }
        public void PrepareToPrint()
        {
            for (int i = 0; i < RootNodes.Count; i++)
            {
                RootNodes[i].GenerateDisplayString();
                PrepareChildren(RootNodes[i]);
            }
        }
        void PrepareChildren(Node child)
        {
            for (int i = 0; i < child.ChildNodes.Count; i++)
                child.ChildNodes[i].GenerateDisplayString();

            for (int i = 0; i < child.ChildNodes.Count; i++)
                PrepareChildren(child.ChildNodes[i]);
        }
        public void MarkAllAsNotMeasured()
        {
            for (int i = 0; i < RootNodes.Count; i++)
            {
                RootNodes[i].IsMeasured = false;
                MarkChildrenAsNotMeasured(RootNodes[i]);
            }
            CalcTotal();
        }
        void MarkChildrenAsNotMeasured(Node child)
        {
            for (int i = 0; i < child.ChildNodes.Count; i++)
                child.ChildNodes[i].IsMeasured = false;
            for (int i = 0; i < child.ChildNodes.Count; i++)
                MarkChildrenAsNotMeasured(child.ChildNodes[i]);
        }
        public Progress GetProgress()
        {
            lock (_lock)
            {
                return new Progress()
                {
                    Total = TotalCount,
                    Parsed = TotalParsed,
                    NotParsed = TotalNotParsed,
                    Measured = TotalMeasured,
                    NotMeasured = TotalNotMeasured,
                    CurrentlyParsing = curParsing,
                    CurrentlyMeasuring = curMeasuring

                };
            }
        }
        public Node GetSlowestNode()
        {
            if (TotalCount == 0)
                return null;
            Node slowest = RootNodes[0];
            for (int i = 0; i < RootNodes.Count; i++)
            {
                slowest = GetSlowestChild(RootNodes[i], slowest);
            }
            return slowest;
        }
        Node GetSlowestChild(Node child, Node slowest)
        {
            if (child.MaxResp > slowest.MaxResp)
                slowest = child;
            for (int i = 0; i < child.ChildNodes.Count; i++)
                slowest = GetSlowestChild(child.ChildNodes[i], slowest);
            return slowest;
        }
        public Node GetFastestNode()
        {
            if (TotalCount == 0)
                return null;
            Node fastest = RootNodes[0];
            for (int i = 0; i < RootNodes.Count; i++)
            {
                fastest = GetFastestChild(RootNodes[i], fastest);
            }
            return fastest;
        }
        Node GetFastestChild(Node child, Node fastest)
        {
            if (child.MinResp < fastest.MinResp)
                fastest = child;
            for (int i = 0; i < child.ChildNodes.Count; i++)
                fastest = GetFastestChild(child.ChildNodes[i], fastest);
            return fastest;
        }
        public int MaxThreads { get; set; }
        public int MeasureCount { get; set; }
        public List<Node> RootNodes { get; set; }
        public bool ParseParams { get; set; }
        public bool CheckOnlyDeeper { get; set; }
        public int TotalCount { get; set; }
        public int TotalParsed { get; set; }
        public int TotalNotParsed { get; set; }
        public int TotalMeasured { get; set; }
        public int TotalNotMeasured { get; set; }
        public int TotalLimit { get; set; }

    }
}

