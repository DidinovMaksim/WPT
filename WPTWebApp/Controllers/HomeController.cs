
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Caching;
using System.Web.Mvc;
using WPTTool.Classes;
using WPTWebApp.Models;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace WPTWebApp.Controllers
{
    public class HomeController : Controller
    {
        // GET: Home
        public ActionResult Index()
        {
            if (Session["isBuilding"] == null)
                Session["isBuilding"] = false;
            if (Session["isMeasuring"] == null)
                Session["isMeasuring"] = false;
            if (Session["curGuid"] == null)
                Session["curGuid"] = "";

            Session["tree"] = null;
            return View();
        }

        public bool ParseTree(string url, bool checkOnlyDeeper = false, bool parseParams = false, int limit = 0)
        {

            if (!(bool)Session["isBuilding"])
            {
                if (Services.getCorrectUri(url) == null)
                {
                    return false;
                }
                //if (Session["tree"] == null)
                if(string.IsNullOrEmpty(url))
                {
                    if (Session["tree"] == null) return false;
                    url = (string)Session["tempUrl"];
                }
                    
                Session["curUrl"] = url;
                Session["tree"] = new SitemapTree(url, parseParams, checkOnlyDeeper, limit);// По идее GC должен будет уничтожить старое дерево?
                Session["curGuid"] = Guid.NewGuid().ToString();

                SitemapTree tree = (SitemapTree)Session["tree"];
                //tree.ParseAllNodesWithTasks();

                Task t = new Task(tree.ParseAllNodesWithTasks);
                Session["isBuilding"] = true;
                t.Start();
                Session["tree"] = tree;
            }
            return true;

        }
        public string GetSlowestNode()
        {
            SitemapTree tree = (SitemapTree)Session["tree"];
            Node slowest = tree.GetSlowestNode();
            return JsonConvert.SerializeObject(slowest);
        }
        public string GetFastestNode()
        {
            SitemapTree tree = (SitemapTree)Session["tree"];
            Node fastest = tree.GetFastestNode();
            return JsonConvert.SerializeObject(fastest);
        }


        public void MeasureSpeed()
        {
            if (!(bool)Session["isMeasuring"])
            {
                SitemapTree tree;
                if (Session["tree"] != null)
                {

                    Session["curGuid"] = Guid.NewGuid().ToString();
                    tree = (SitemapTree)Session["tree"];
                    tree.MarkAllAsNotMeasured();

                    Task t = new Task(tree.MeasureAllNodesWithTasks);
                    t.Start();
                    Session["isMeasuring"] = true;
                }
            }
        }
        public string GetTree()
        {
            SitemapTree tree = (SitemapTree)Session["tree"];
            return JsonConvert.SerializeObject(tree.RootNodes);
        }
        public string GetProgress()
        {
            Progress pr;
            if (Session["tree"] != null)
                pr = ((SitemapTree)Session["tree"]).GetProgress();
            else
                pr = new Progress();

            if (pr.Parsed == pr.Total)
                Session["isBuilding"] = false;
            if (pr.Measured == pr.Total)
                Session["isMeasuring"] = false;
            return JsonConvert.SerializeObject(pr);
        }
        public void SaveTree(string desc)
        {
            SitemapTree tree = (SitemapTree)Session["tree"];
            using (WptDbEntities db = new WptDbEntities())
            {
                try
                {
                    XmlSerializer formatter = new XmlSerializer(typeof(SitemapTree));
                    StringWriter sw = new StringWriter();
                    formatter.Serialize(sw, tree);
                    string data = sw.ToString();

                    History h = new History()
                    {
                        Guid = (string)Session["curGuid"],
                        Address = (string)Session["curUrl"],
                        Description = desc,
                        Data = data,
                        Date = DateTime.Now
                    };

                    db.History.Add(h);
                    db.SaveChanges();
                }
                catch (Exception ex) { }

            }
        }
        public string GetTreeInfo(string Id)
        {
            object ob = new object();
            using (WptDbEntities db = new WptDbEntities())
            {
                ob = (from his in db.History where string.Compare(his.Guid, Id) == 0
                      select new
                      {
                          Description = his.Description,
                          Date = his.Date
                      }).First<object>();

            }
            return JsonConvert.SerializeObject(ob);
        }
        public string LoadTreeList()
        {
            List<object> list = new List<object>();
            using (WptDbEntities db = new WptDbEntities())
            {
                list = (from his in db.History
                        select new
                        {
                            Id = his.Guid,
                            Address = his.Address,
                            Description = his.Description,
                            Date = his.Date
                        }).ToList<object>();

            }
            return JsonConvert.SerializeObject(list);

        }
        public string LoadTreeFromDB(string Id)
        {
            if (string.IsNullOrEmpty(Id))
                return "";
            SitemapTree tree = null;
            string address = "";
            try
            {
                using (WptDbEntities db = new WptDbEntities())
                {
                    History hs = (from his in db.History where string.Compare(his.Guid, Id) == 0 select his).First();
                    string data = hs.Data; // Дописать чеки (0)
                    TextReader reader = new StringReader(data);
                    XmlSerializer formatter = new XmlSerializer(typeof(SitemapTree));
                    tree = (SitemapTree)formatter.Deserialize(reader);
                    Session["curGuid"] = hs.Guid;

                    Session["tempUrl"] = hs.Address;
                    address = hs.Address;
                }
            }
            catch { return ""; }
            
            Session["tree"] = tree;
            return address;
        }
    }
}