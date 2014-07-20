using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Xml;

namespace TakaoFireStation
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Kaohsiung Fire Station Data Tool v1.0 by Kagami");
            Console.WriteLine("Press Any Key to Do !");
            Console.ReadLine();

            DataDownloader _downloader = new DataDownloader("http://data.kaohsiung.gov.tw/Opendata/DownLoad.aspx?Type=3&CaseNo1=AR&CaseNo2=1&FileType=2&Lang=C&FolderType=O");
            //下載成功
            if (_downloader.DownloadText != "")
            {
                //轉換格式
                Text2OSMXml _converter = new Text2OSMXml(_downloader.DownloadText);

                string result = _converter.SaveData();

                if (result != "")
                    Console.WriteLine(result + "Saved.");
            }

            Console.WriteLine("Press Any Key to Exit!");
            Console.ReadLine();
        }
    }

    /*
     * Reference List:
     * WGS84經緯度與TWD97(TM2)投影坐標轉換程式 ： http://sask989.blogspot.tw/2012/05/wgs84totwd97.html
     * TWD97轉經緯度WGS84 經緯度WGS84轉TWD97 ： http://wangshifuola.blogspot.tw/2010/08/twd97wgs84-wgs84twd97.html
     * 經緯度轉換TWD97 ： http://blog.ez2learn.com/2009/08/15/lat-lon-to-twd97/
    */
    class TWD97ToWGS84
    {
        private static double a = 6378137.0;
        private static double b = 6356752.3142451;
        private double lon0 = 121 * Math.PI / 180;
        private double k0 = 0.9999;
        private int dx = 250000;
        private int dy = 0;
        private double e = 1 - Math.Pow(b, 2) / Math.Pow(a, 2);
        private double e2 = (1 - Math.Pow(b, 2) / Math.Pow(a, 2)) / (Math.Pow(b, 2) / Math.Pow(a, 2));

        public TWD97ToWGS84()
        {
            
        }

        public string TWD97Tolonlat(double x, double y)
        {
            x -= dx;
            y -= dy;

            // Calculate the Meridional Arc
            double M = y / k0;

            // Calculate Footprint Latitude
            double mu = M / (a * (1.0 - e / 4.0 - 3 * Math.Pow(e, 2) / 64.0 - 5 * Math.Pow(e, 3) / 256.0));
            double e1 = (1.0 - Math.Sqrt(1.0 - e)) / (1.0 + Math.Sqrt(1.0 - e));

            double J1 = (3 * e1 / 2 - 27 * Math.Pow(e1, 3) / 32.0);
            double J2 = (21 * Math.Pow(e1, 2) / 16 - 55 * Math.Pow(e1, 4) / 32.0);
            double J3 = (151 * Math.Pow(e1, 3) / 96.0);
            double J4 = (1097 * Math.Pow(e1, 4) / 512.0);

            double fp = mu + J1 * Math.Sin(2 * mu) + J2 * Math.Sin(4 * mu) + J3 * Math.Sin(6 * mu) + J4 * Math.Sin(8 * mu);

            // Calculate Latitude and Longitude
            double C1 = e2 * Math.Pow(Math.Cos(fp), 2);
            double T1 = Math.Pow(Math.Tan(fp), 2);
            double R1 = a * (1 - e) / Math.Pow((1 - e * Math.Pow(Math.Sin(fp), 2)), (3.0 / 2.0));
            double N1 = a / Math.Pow((1 - e * Math.Pow(Math.Sin(fp), 2)), 0.5);

            double D = x / (N1 * k0);

            // Calcualte Latitude
            double Q1 = N1 * Math.Tan(fp) / R1;
            double Q2 = (Math.Pow(D, 2) / 2.0);
            double Q3 = (5 + 3 * T1 + 10 * C1 - 4 * Math.Pow(C1, 2) - 9 * e2) * Math.Pow(D, 4) / 24.0;
            double Q4 = (61 + 90 * T1 + 298 * C1 + 45 * Math.Pow(T1, 2) - 3 * Math.Pow(C1, 2) - 252 * e2) * Math.Pow(D, 6) / 720.0;
            double lat = fp - Q1 * (Q2 - Q3 + Q4);

            // Calcualte Longitude
            double Q5 = D;
            double Q6 = (1 + 2 * T1 + C1) * Math.Pow(D, 3) / 6;
            double Q7 = (5 - 2 * C1 + 28 * T1 - 3 * Math.Pow(C1, 2) + 8 * e2 + 24 * Math.Pow(T1, 2)) * Math.Pow(D, 5) / 120.0;
            double lon = lon0 + (Q5 - Q6 + Q7) / Math.Cos(fp);

            lat = (lat * 180) / Math.PI; //Latitude
            lon = (lon * 180) / Math.PI; //Longitude

            string lonlat = lon + "," + lat;
            return lonlat;
        }
    }

    class Text2OSMXml
    {

        private char _breakLine = '\n';
        private char _dot = ',';

        private XmlDocument _resultDoc = new XmlDocument();

        public Text2OSMXml(string originalText)
        {
            _resultDoc = String2Xml(originalText);
        }

        public string SaveData()
        {
            string _fileName = DateTime.Now.ToString("yyyyMMdd") + "_FireStation.xml";

            try
            {
                _resultDoc.Save(_fileName);
                return _fileName;
            }
            catch (XmlException e)
            {
                Console.WriteLine("Data Save Failed ! Check your xml format is vailed.");
                Console.WriteLine("Code:" + e.Message);
                return "";
            }
        }

        private XmlDocument String2Xml(string text)
        {
            //**************** Prepare Original Doc *****************************
            string[] rows = text.Trim().Split(_breakLine);

            TWD97ToWGS84 _geoConverter = new TWD97ToWGS84();

            //****************** Generate Result XML ****************************
            XmlDocument resultdoc = new XmlDocument();

            XmlDeclaration dec = resultdoc.CreateXmlDeclaration("1.0", "UTF-8", null);

            resultdoc.AppendChild(dec);

            XmlElement osm = resultdoc.CreateElement("osm");
            osm.SetAttribute("version", "0.6");
            osm.SetAttribute("generator", "kagami");

            //********************* Add Data to XML *****************************
            //從1開始跑，因為0是敘述
            for (int i = 1; i < rows.Length; ++i)
            {
                string[] _data = rows[i].Split(_dot);

                //************* Convert TWD97 to WGS84
                string _WGS84 = _geoConverter.TWD97Tolonlat(Convert.ToDouble(_data[1]),Convert.ToDouble(_data[2]));
                string[] _WGS84Seperate = _WGS84.Split(_dot);

                XmlElement node = resultdoc.CreateElement("node");

                node.SetAttribute("id", "-" + i.ToString());
                node.SetAttribute("lat", _WGS84Seperate[1]);
                node.SetAttribute("lon", _WGS84Seperate[0]);
                node.SetAttribute("visible", "true");
                node.SetAttribute("version", "1");

                //************ Add Sub Tags *********************************
                XmlElement staName = resultdoc.CreateElement("tag");
                staName.SetAttribute("k", "name");
                staName.SetAttribute("v", _data[0].Trim());

                XmlElement staNetwork = resultdoc.CreateElement("tag");
                staNetwork.SetAttribute("k", "network");
                staNetwork.SetAttribute("v", "Kaohsiung_FireStation");

                XmlElement staOperator = resultdoc.CreateElement("tag");
                staOperator.SetAttribute("k", "operator");
                staOperator.SetAttribute("v", "高雄市政府消防局");

                XmlElement amenity = resultdoc.CreateElement("tag");
                amenity.SetAttribute("k", "amenity");
                amenity.SetAttribute("v", "fire_station");

                node.AppendChild(staName);
                node.AppendChild(staNetwork);
                node.AppendChild(staOperator);
                node.AppendChild(amenity);

                osm.AppendChild(node);
            }

            resultdoc.AppendChild(osm);

            return resultdoc;
        }
    }

    class DataDownloader
    {
        public string DownloadText
        {
            get
            {
                return _originalText;
            }
        }
        private string _originalText = "";

        public DataDownloader(string url)
        {
            try
            {
                Console.WriteLine("Downloading......");
                WebClient _Downloader = new WebClient();
                _Downloader.Credentials = CredentialCache.DefaultCredentials;

                Byte[] _UnconvertedBytes = _Downloader.DownloadData(url);

                _originalText = Encoding.UTF8.GetString(_UnconvertedBytes);

                _Downloader.Dispose();
            }
            catch (WebException e)
            {
                Console.WriteLine("Download failed , See" + e.Message);
            }
        }
    }
}
