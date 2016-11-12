using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WPTTool.Classes
{
    [Serializable]
    public class SitemapTree
    {
        public enum CheckParse { WholeTree, From, To, FromTo, SpecificLevel };
        public object _lock = new object();
        int maxThreads = 1,
            measureCount,
            totalCount,
            totalParsed,
            totalNotParsed,
            totalMeasured,
            totalNotMeasured,
            totalLimit;

        //int pagesParsing = 0;
        string curParsing = "",
            curMeasuring = "";

        bool parseParams, checkOnlyDeeper;
        List<Node> rootNodes, AllNodes;

        public SitemapTree()
        {
            // Конструктор сугубо без параметров нужно для сериализации. Необязательный параметры он все равно воспринмиает как обязательные.
            measureCount = 5;
            rootNodes = new List<Node>();
            AllNodes = new List<Node>();
            parseParams = false;
            checkOnlyDeeper = false;
            maxThreads = Environment.ProcessorCount;
        }
        public SitemapTree(bool parseParams = false, bool checkOnlyDeeper = false, int limit = 0) : this()
        {

            this.parseParams = parseParams;
            this.checkOnlyDeeper = checkOnlyDeeper;
            totalLimit = limit;
        }
        public SitemapTree(string url, bool parseParams = false, bool checkOnlyDeeper = false, int limit = 0) :
            this(parseParams, checkOnlyDeeper, limit)
        {
            Uri uri = Services.getCorrectUri(url);
            if (uri != null)
                AddNodesFromStrings(new string[] { uri.AbsoluteUri }, this.checkOnlyDeeper);
        }
        public Node GetRootNode(string url)
        {
            for (int i = 0; i < rootNodes.Count; i++)
                if (string.Compare(rootNodes[i].Url, url) == 0)
                    return rootNodes[i];

            return null;
        }
        public void AddNodesFromStrings(string[] href, bool checkOnlyDeeper = false)
        {
            if (href.Count() == 0) return;
            Uri tmpUri = new Uri(href[0]);
            Node curNode;
            int minDepth = tmpUri.Segments.Count() - 1;
            if (parseParams)
                minDepth += (!string.IsNullOrEmpty(tmpUri.Query) ? 1 : 0);
            int curDepth = 0;
            int added = 0;
            for (int i = 0; i < href.Length; i++)
            {
                if (totalLimit > 0)
                    if (!(totalCount + added < TotalLimit))
                        break;

                tmpUri = new Uri(href[i]);
                if (tmpUri == null) continue;

                curDepth = tmpUri.Segments.Count() - 1;
                if (parseParams)
                    curDepth += !string.IsNullOrEmpty(tmpUri.Query) ? 1 : 0;

                if (curDepth < minDepth)
                    minDepth = curDepth;

                if (GetRootNode(tmpUri.Host + tmpUri.Segments[0]) == null)
                {
                    rootNodes.Add(new Node(tmpUri.Host + tmpUri.Segments[0]));
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
                if (parseParams)
                    if (!string.IsNullOrEmpty(tmpUri.Query))
                        if (!curNode.ChildNodeExists(tmpUri.Query))
                        {
                            curNode.AddNode(tmpUri.Query);
                            added++;
                        }
            }

            if (checkOnlyDeeper)
            {
                for (int i = 0; i < rootNodes.Count; i++)
                {
                    rootNodes[i].IsParsed = true;
                    MarkParsed(rootNodes[i], 1, minDepth);
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
                Console.WriteLine("T:" + Task.CurrentId + "  " + totalParsed + "/" + totalCount + " " + node.FullUrl);
                string[] href = Parser.getAllHref(node.FullUrl, checkOnlyDeeper: checkOnlyDeeper);
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
                //Console.WriteLine("T:" + Task.CurrentId + "  " + totalMeasured + "/" + totalCount + " " + node.FullUrl);
                System.Diagnostics.Debug.WriteLine("T:" + Task.CurrentId + "  " + totalMeasured + "/" + totalCount + " " + node.FullUrl);
                node.MakeSpeedMeasurments(measureCount);
                lock (_lock)
                {
                    CalcTotal();
                }
            }
        }
        public void CalcTotal()
        {
            totalCount = 0;
            totalNotParsed = 0;
            totalParsed = 0;
            totalMeasured = 0;
            totalNotMeasured = 0;

            for (int i = 0; i < rootNodes.Count; i++)
            {
                CountChilds(rootNodes[i]);
            }
            totalParsed = totalCount - totalNotParsed;
            totalMeasured = totalCount - totalNotMeasured;
        }
        void CountChilds(Node node)
        {
            totalCount += 1;
            totalNotParsed += node.IsParsed ? 0 : 1;
            totalNotMeasured += node.IsMeasured ? 0 : 1;

            for (int i = 0; i < node.ChildNodes.Count; i++)
                CountChilds(node.ChildNodes[i]);
        }
        public int CalcMaxDepth()
        {
            int maxDepth = 0, temp = 0;
            for (int i = 0; i < rootNodes.Count; i++)
            {
                temp = NodeDepth(rootNodes[i]);
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
            for (int i = 0; i < rootNodes.Count; i++)
            {
                if (!rootNodes[i].IsMeasured)
                    AllNodes.Add(rootNodes[i]);
                AllNodes.AddRange(getNotMeasuredChildren(rootNodes[i]));
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
            return rootNodes;
        }
        public void GenerateNotParsedList()
        {
            AllNodes.Clear();
            for (int i = 0; i < rootNodes.Count; i++)
            {
                if (!rootNodes[i].IsParsed)
                    AllNodes.Add(rootNodes[i]);//.getCopy());

                AllNodes.AddRange(getNotParsedChildren(rootNodes[i]));
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

            int taskCount = maxThreads < AllNodes.Count ? maxThreads : AllNodes.Count;
            Task[] t = new Task[taskCount];
            /*for(int i = 0; i < AllNodes.Count; i++)
            {
                ParseSpecificNode(AllNodes[i]);
            }*/
            for (int i = 0; i < AllNodes.Count; i++)
            {
                Node temp = AllNodes[i];
                if (t[i % maxThreads] == null)
                    t[i % maxThreads] = new Task(() => ParseSpecificNode(temp));
                else
                    t[i % maxThreads].ContinueWith(new Action<Task>((par) => ParseSpecificNode(temp)));
            }
            for (int i = 0; i < taskCount; i++)
            {
                t[i].Start();
            }
            Task.WaitAll(t);

            if (totalNotParsed != 0)
                ParseAllNodesWithTasks();

            PrepareToPrint();
            curParsing = "";
        }
        public void MeasureAllNodesWithTasks()
        {
            GenerateNotMeasuredList();

            int taskCount = maxThreads < AllNodes.Count ? maxThreads : AllNodes.Count;
            Task[] t = new Task[taskCount];

            /*for (int i = 0; i < AllNodes.Count; i++)
            {
                MeasureSpecificNode(AllNodes[i]);
            }*/
            for (int i = 0; i < AllNodes.Count; i++)
            {
                //pagesParsing++;
                Node temp = AllNodes[i];
                if (t[i % maxThreads] == null)
                    t[i % maxThreads] = new Task(() => MeasureSpecificNode(temp));
                else
                    t[i % maxThreads].ContinueWith(new Action<Task>((par) => MeasureSpecificNode(temp)));
            }
            for (int i = 0; i < taskCount; i++)
            {
                t[i].Start();
            }
            Task.WaitAll(t);
            if (totalNotMeasured != 0)
                MeasureAllNodesWithTasks();

            PrepareToPrint();
            curMeasuring = "";
        }
        public void PrepareToPrint()
        {
            for (int i = 0; i < rootNodes.Count; i++)
            {
                rootNodes[i].GenerateDisplayString();
                PrepareChildren(rootNodes[i]);
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
            for (int i = 0; i < rootNodes.Count; i++)
            {
                rootNodes[i].IsMeasured = false;
                MarkChildrenAsNotMeasured(rootNodes[i]);
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
                    Total = totalCount,
                    Parsed = totalParsed,
                    NotParsed = totalNotParsed,
                    Measured = totalMeasured,
                    NotMeasured = totalNotMeasured,
                    CurrentlyParsing = curParsing,
                    CurrentlyMeasuring = curMeasuring

                };
            }
        }
        public Node GetSlowestNode()
        {
            if (totalCount == 0)
                return null;
            Node slowest = rootNodes[0];
            for (int i = 0; i < rootNodes.Count; i++)
            {
                slowest = GetSlowestChild(rootNodes[i], slowest);
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
            if (totalCount == 0)
                return null;
            Node fastest = rootNodes[0];
            for (int i = 0; i < rootNodes.Count; i++)
            {
                fastest = GetFastestChild(rootNodes[i], fastest);
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
        public int MaxThreads
        {
            get { return maxThreads; }
            set { this.maxThreads = value; }
        }
        public int MeasureCount
        {
            get { return measureCount; }
            set { measureCount = value; }
        }
        public List<Node> RootNodes
        {
            get { return rootNodes; }
            set { rootNodes = value; }
        }
        public bool ParseParams
        {
            get { return parseParams; }
            set { parseParams = value; }
        }
        public bool CheckOnlyDeeper
        {
            get { return checkOnlyDeeper; }
            set { checkOnlyDeeper = value; }
        }
        public int TotalCount
        {
            get { return totalCount; }
            set { totalCount = value; }
        }
        public int TotalParsed
        {
            get { return totalParsed; }
            set { totalParsed = value; }
        }
        public int TotalNotParsed
        {
            get { return totalNotParsed; }
            set { totalNotParsed = value; }
        }
        public int TotalMeasured
        {
            get { return totalMeasured; }
            set { totalMeasured = value; }
        }
        public int TotalNotMeasured
        {
            get { return totalNotMeasured; }
            set { totalNotMeasured = value; }
        }
        public int TotalLimit
        {
            get { return totalLimit; }
            set { totalLimit = value; }
        }

    }
}

