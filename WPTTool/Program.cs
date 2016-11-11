using System;
using System.IO;
using System.Xml.Serialization;
using WPTTool.Classes;

namespace WPTTool
{
    class Program
    {
        static void Main(string[] args)
        {
            SitemapTree tree = new SitemapTree("http://startandroid.ru/", checkOnlyDeeper:true);
            //SitemapTree tree = new SitemapTree("http://startandroid.ru/ru/uroki/vse-uroki-spiskom/9-urok-2-ustanovka-i-nastrojka-sredy-razrabotki.html", checkOnlyDeeper: true);
            tree.ParseAllNodesWithTasks();
            tree.MeasureAllNodesWithTasks();

            XmlSerializer formatter = new XmlSerializer(typeof(SitemapTree));
            StringWriter sw = new StringWriter();
            formatter.Serialize(sw, tree);
            string info = sw.ToString();
            TextReader reader = new StringReader(info); 
            SitemapTree result = (SitemapTree)formatter.Deserialize(reader);

            
            // получаем поток, куда будем записывать сериализованный объект
            using (FileStream fs = new FileStream("tree.xml", FileMode.OpenOrCreate))
            {
                //formatter.Serialize()
                Console.WriteLine("Объект сериализован");
            }

            // десериализация
            using (FileStream fs = new FileStream("tree.xml", FileMode.OpenOrCreate))
            {
                SitemapTree newPerson = (SitemapTree)formatter.Deserialize(fs);

                Console.WriteLine("Объект десериализован");
            }

        }
    }
}
