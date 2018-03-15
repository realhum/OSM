using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;


namespace GraphOpenStreetMap
{
    class Program
    {

        private static readonly double major = 6378137.0;
        private static readonly double minor = 6356752.3142;
        private static readonly double ratio = minor / major;
        private static readonly double e = Math.Sqrt(1.0 - (ratio * ratio));
        private static readonly double com = 0.5 * e;
        private static readonly double degToRad = Math.PI / 180.0;
        
        struct coord
        {
            public double lat;
            public double lon;
        }

        private static double minlon;
        private static double maxlat;

        private static SortedDictionary<long, coord> Nodes = new SortedDictionary<long, coord>();
        private static SortedDictionary<long, List<long>> AddjestedList = new SortedDictionary<long, List<long>>();
        private static List<string> Valid = new List<string>() {"motorway", "motorway_link", "trunk", "trunk_link", "primary", "primary_link", "secondary",
                                            "secondary_link", "tertiary", "tertiary_link", "unclassified", "road", "service", "living_street", "residential" };

        static void Osm(string path)
        {   

            XmlDocument xDoc = new XmlDocument();
            xDoc.Load(path);
            XmlElement xRoot = xDoc.DocumentElement;
            XmlNodeList nodes = xRoot.SelectNodes("node");
            maxlat = double.Parse(xRoot.SelectSingleNode("bounds").Attributes["maxlat"].Value, CultureInfo.InvariantCulture);
            minlon = double.Parse(xRoot.SelectSingleNode("bounds").Attributes["minlon"].Value, CultureInfo.InvariantCulture);
            foreach (XmlNode n in nodes)
            {
                long id = long.Parse(n.SelectSingleNode("@id").Value);
                double lat = double.Parse(n.SelectSingleNode("@lat").Value, CultureInfo.InvariantCulture);
                double lon = double.Parse(n.SelectSingleNode("@lon").Value, CultureInfo.InvariantCulture);
                coord Node_coord;
                Node_coord.lat = lat;
                Node_coord.lon = lon;
                Nodes.Add(id, Node_coord);
            }
            Valid.Sort();
            XmlNodeList ways = xRoot.SelectNodes("//way[.//tag[@k = 'highway']]");
            foreach (XmlNode n in ways)
            {
                string validway = n.SelectSingleNode("tag[@k = 'highway']").Attributes["v"].Value;
                if (Valid.BinarySearch(validway) >= 0)
                {
                    XmlNodeList nd = n.SelectNodes("nd");
                    List<long> nodes_list_id = new List<long>();
                    foreach (XmlNode m in nd)
                    {
                        long id = long.Parse(m.SelectSingleNode("@ref").Value);
                        nodes_list_id.Add(id);
                    }
                    for (int i = 0; i < nodes_list_id.Count(); ++i)
                    {
                        if (i < nodes_list_id.Count() - 1)
                        {
                            if (AddjestedList.ContainsKey(nodes_list_id[i]))
                            {
                                AddjestedList[nodes_list_id[i]].Add(nodes_list_id[i + 1]);
                            }
                            else
                            {
                                AddjestedList.Add(nodes_list_id[i], new List<long>());
                                AddjestedList[nodes_list_id[i]].Add(nodes_list_id[i + 1]);
                            }
                        }
                        if (i >= 1)
                        {
                            if (AddjestedList.ContainsKey(nodes_list_id[i]))
                            {
                                AddjestedList[nodes_list_id[i]].Add(nodes_list_id[i - 1]);
                            }
                            else
                            {
                                AddjestedList.Add(nodes_list_id[i], new List<long>());
                                AddjestedList[nodes_list_id[i]].Add(nodes_list_id[i - 1]);
                            }
                        }
                    }
                }
            }
        }

        static void Csv()
        {
            string pathСsv;
            Console.WriteLine("Insert path for .csv:");
            pathСsv = Console.ReadLine();
            System.IO.StreamWriter outputFile = new System.IO.StreamWriter(pathСsv + ".csv");
            outputFile.WriteLine("Nodes;Addjested Nodes");
            ICollection<long> keys = AddjestedList.Keys;
            foreach (long i in keys)
            {
                string newLine = "";
                newLine += i;
                newLine += ";";
                newLine += "{";
                for (int j = 0; j < AddjestedList[i].Count(); ++j)
                {
                    newLine += AddjestedList[i][j];
                    newLine += ",";
                }
                newLine += "}";
                outputFile.WriteLine(newLine);
            }
            outputFile.Close();
        }

        private static double DegToRad(double deg)
        {
            return deg * degToRad;
        }

        public static double lonToX(double lon)
        {
            return major * DegToRad(lon) * 0.1;
        }

        public static double latToY(double lat)
        {
            lat = Math.Min(89.5, Math.Max(lat, -89.5));
            double phi = DegToRad(lat);
            double sinphi = Math.Sin(phi);
            double con = e * sinphi;
            con = Math.Pow(((1.0 - con) / (1.0 + con)), com);
            double ts = Math.Tan(0.5 * ((Math.PI * 0.5) - phi)) / con;
            return 0 - major * Math.Log(ts) * 0.1;
        }

        static void Svg()
        {
            string pathSvg;
            Console.WriteLine("Insert path for .svg");
            pathSvg = Console.ReadLine();
            System.IO.StreamWriter outputFile = new System.IO.StreamWriter(pathSvg + ".svg");
            outputFile.WriteLine("<svg version = \"1.1\" baseProfile = \"full\" xmlns = \"http://www.w3.org/2000/svg\" >");
            ICollection<long> keys = AddjestedList.Keys;
            foreach (long i in keys)
            {
                for (int j = 0; j < AddjestedList[i].Count(); ++j)
                {
                    string newLine = "<line ";
                    newLine += "x1=\"" + System.Convert.ToString(lonToX(Nodes[i].lon) - lonToX(minlon)).Replace(",", ".") + "\" x2=\"" + System.Convert.ToString(lonToX(Nodes[AddjestedList[i][j]].lon) - lonToX(minlon)).Replace(",", ".") + "\" y1=\"" + System.Convert.ToString(-latToY(Nodes[i].lat) + latToY(maxlat)).Replace(",", ".") + "\" y2=\"" + System.Convert.ToString(-latToY(Nodes[AddjestedList[i][j]].lat) + latToY(maxlat)).Replace(",", ".") + "\" ";
                    newLine += "stroke = \"green\" stroke-width= \"1\" />";
                    int k = 0;
                    outputFile.WriteLine(newLine);
                }

            }
            outputFile.WriteLine("</svg>");
            outputFile.Close();
        }

        static void Main(string[] args)
        {
            string path;
            Console.WriteLine("Insert full way to osm file:");
            path = Console.ReadLine();
            Osm(path);
            Csv();
            Svg();
            Console.ReadLine();
        }
    }
}
