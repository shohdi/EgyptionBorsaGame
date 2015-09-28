using Ninject;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Globalization;

namespace getBorsaStatistics
{

    public interface IGetLinks
    {
        IEnumerable<borsaModel> getLinks(string url);
    }

    public class MubasherLinks : IGetLinks
    {
        public IEnumerable<borsaModel> getLinks(string url)
        {
            List<string> lstUrls = new List<string>();
            List<borsaModel> lstLinks = new List<borsaModel>();
            string allUrlsInConfig = ConfigurationManager.AppSettings["urls"];
            lstUrls = allUrlsInConfig.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            string val = "href=\"" + ConfigurationManager.AppSettings["mainLink"];
            val = Regex.Escape(val);
            foreach (string mainUrl in lstUrls)
            {

                string str = "";

                if (mainUrl.StartsWith("local://", StringComparison.OrdinalIgnoreCase))
                {
                    str = File.ReadAllText(mainUrl.Substring(8));
                }
                else
                {

                    WebClient client = new WebClient();
                    client.Headers.Add("User-Agent", ConfigurationManager.AppSettings["browserAgent"]);
                    client.Encoding = Encoding.UTF8;

                    Program.tryUntilSuccess(() => { str = client.DownloadString(mainUrl); });
                }


                Regex reg = new Regex(val + "[^\\\"]+\\\"", RegexOptions.IgnoreCase);
                Match msh = null;
               
                do
                {

                    if (msh == null)
                    {
                        msh = reg.Match(str);
                    }
                    else
                    {
                        msh = msh.NextMatch();
                    }
                    if (msh.Success)
                    {
                        string foundUrl = msh.Value.Substring(6, msh.Value.Length - 7);

                        string fullUrl = Program.getFullUrl(mainUrl, foundUrl);

                        borsaModel model = new borsaModel();
                        model.Link = fullUrl;
                        if (lstLinks.Where(obj=>obj.Link == fullUrl).Count() == 0)
                            lstLinks.Add(model);
                    }
                } while (msh.Success);
            }
            return lstLinks;
        }
    }


    public class EgyptLinks : IGetLinks
    {
        public IEnumerable<borsaModel> getLinks(string url)
        {
            List<string> lstUrls = new List<string>();
            List<borsaModel> lstLinks = new List<borsaModel>();
            string allUrlsInConfig = ConfigurationManager.AppSettings["urls"];
            lstUrls = allUrlsInConfig.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries).ToList();
            string mainLink = ConfigurationManager.AppSettings["mainLink"];
            
            foreach (string mainUrl in lstUrls)
            {
                string str = "";
                WebClient client = new WebClient();
                client.Headers.Add("User-Agent", ConfigurationManager.AppSettings["browserAgent"]);
                client.Encoding = Encoding.UTF8;

                Program.tryUntilSuccess(() => { str = client.DownloadString(mainUrl); });

                Regex reg = new Regex(@"<tr[^>]*RowStyle[^>]*>", RegexOptions.IgnoreCase);
                Match mch = reg.Match(str);
                while (mch.Success)
                {
                    int lastIndex = getLastIndex(mch,str, "<tr[^>]*>", "</tr>");
                    string row = str.Substring(mch.Index, lastIndex-mch.Index);

                    Regex regCol = new Regex(@"<td[^>]*>", RegexOptions.IgnoreCase);

                    List<string> lstCols = new List<string>();

                    Match mchCol = regCol.Match(row);
                    while (mchCol.Success)
                    {
                        int lastColIndex = getLastIndex(mchCol, row, "<td[^>]*>", "</td>");
                        string col = row.Substring(mchCol.Index, lastColIndex-mchCol.Index);
                        col = Regex.Replace(col, "<[^>]+>", "").Trim();
                        lstCols.Add(col);
                       mchCol =  mchCol.NextMatch();
                    }

                    borsaModel newModel = new borsaModel();
                    newModel.Link = string.Format(mainLink,lstCols[0]);
                    newModel.name = lstCols[1];
                    newModel.typeOfComp = lstCols[2];
                    newModel.profitDate = lstCols[3];
                    double test = 0;
                    double.TryParse(lstCols[5],out test);
                    newModel.profitAnn = test*1000;
                    test = 0;
                    double.TryParse(lstCols[6], out test);
                    newModel.profitComp = test*1000;
                    newModel.change = lstCols[7];
                    newModel.currency = lstCols[8];

                    if (lstLinks.Where(obj => obj.Link == newModel.Link).Count() == 0)
                        lstLinks.Add(newModel);

                    mch = mch.NextMatch();
                }

            }
            return lstLinks;
        }

        private int getLastIndex(Match mch,string str, string regexPatternToFind,string closePattern)
        {
            string fromMatch = str.Substring(mch.Index);
            string fromAfterMatch = str.Substring(mch.Index + mch.Length);
            Regex regFoundIt = new Regex("("+regexPatternToFind+"|"+closePattern+")",RegexOptions.IgnoreCase);
            Match mchNext = regFoundIt.Match(fromAfterMatch);
            while (mchNext.Success)
            {
                if (Regex.Match(mchNext.Value, closePattern).Success)
                {
                    return mch.Index + mch.Length + mchNext.Index;

                }
                else
                {
                    mchNext = mchNext.NextMatch();
                    mchNext = mchNext.NextMatch();
                }

            }
            return -1;
        }
    }

    public class borsaModel
        {
			[DisplayName("الكود")]	
			public string ruitersCode { get; set; }

            [DisplayName("الاسم")]
            public string name { get; set; }
            [DisplayName("متوسط السعر")]
            public double currentPrice { get; set; }
            [DisplayName("التغير")]
            public string change { get; set; }
           [DisplayName("الحجم")]
            public double size { get; set; }
             [DisplayName("قيمة التداول")]
            public double sellBuy { get; set; }
             [DisplayName("فتح")]
            public double open { get; set; }
             [DisplayName("آخر صفقة")]
            public double last { get; set; }
            [DisplayName("القيمة الأسمية")]
            public double nameVal { get; set; }
            
            [DisplayName("معدل نمو اجمالي الأصول")]
            public double rangeOfGrowOfMain { get; set; }
             [DisplayName("القيمة الدفترية")]
            public double fileVal { get; set; }
             [DisplayName("مضاعف القيمة الدفترية")]
            public double growFile { get; set; }
             [DisplayName("ربحية السهم")]
            public double profit { get; set; }
            
            [DisplayName("العملة")]
            public string currency { get; set; }
            [DisplayName("عدد أسهم الشركة الحالي")]
            public double noOfStocks { get; set; }
            [DisplayName("رأس المال")]
            public double money { get; set; }
            [DisplayName("تاريخ إعلان الأرباح")]
            public string profitDate { get; set; }
            [DisplayName("الربح المعلن")]
            public double profitAnn { get; set; }
            [DisplayName("الربح المعلن مجزأ ربع")]
            public double profitAnnDiv
            {
                get
                {
                    try
                    {
                        if (profitAnn > 0)
                        {
                            if (profitType.Contains("تراكمي"))
                            {
                                if (profitType.Contains("ثان"))
                                    return profitAnn / 2;
                                else
                                    return profitAnn / 3;
                            }
                            else if (profitType.Contains("سنو"))
                            {
                                return profitAnn / 4;
                            }
                            else
                            {
                                return profitAnn;
                            }
                        }
                        else
                        {
                            return profitAnn;
                        }
                    }
                    catch
                    {
                        return 0;
                    }


                }
            }
            [DisplayName("الربح المقارن")]
            public double profitComp {get;set;}
            [DisplayName("حالة الميزانية")]
            public string profitType { get; set; }
            [DisplayName("القيمة السوقية")]
            public double marketVal { get; set; }
            [DisplayName("القيمة السوقية المحسوبة")]
            public double marketValCalc { get; set; }
            [DisplayName("مضاعف الربحية")]
            public double growProfit { get; set; }
            [DisplayName("مضاعف الربحية على آخر صفقة وربحية السهم")]
            public double? lastProfitGrow
            {
                get
                {
                    if (profit == 0)
                        return null;
                    else
                        return last / profit;
                }
            }

            [DisplayName("مضاعف الربحية على احصائيات السوق")]
            public double? lastProfitGrowFullStocks
            {
                get
                {
                    if (profitAnn == 0)
                        return null;
                    else
                        return marketVal / profitAnn;
                }
            }


            [DisplayName("مضاعف الربحية على احصائيات السوق المحسوبة")]
            public double? lastProfitGrowFullStocksCalculated { get {
                if (profitAnn == 0)
                    return null;
                else
                    return marketValCalc / profitAnn;
            } }

            [DisplayName("مضاعف الربحية على الربع سنوي")]
            public double? lastProfitGrowFullStocksQuarterCalculated
            {
                get
                {
                    if (profitAnnDiv == 0)
                        return null;
                    else
                    {
                        double marVal = marketValCalc;
                        if (marVal == 0)
                            marVal = marketVal;
                        return marVal / profitAnnDiv;
                    }
                }
            }


            [DisplayName("سنة اخر إعلان ارباح")]
            public int YearOfLastProfit
            {
                get
                {
                    DateTime dtOut = new DateTime();
				CultureInfo cal = new CultureInfo ("ar-EG");
				if (DateTime.TryParse(this.profitDate,cal,DateTimeStyles.None,out dtOut))
                    {
                        return dtOut.Year;
                    }
                    else
                        return 0;
                   
                }
            }

		[DisplayName("فرق السنين")]
		public int YearsDifference
		{
			get
			{
				DateTime dtOut = new DateTime();
				CultureInfo cal = new CultureInfo ("ar-EG");
				if (DateTime.TryParse(this.profitDate,cal,DateTimeStyles.None,out dtOut))
				{
					return (int)(((DateTime.Now - dtOut).TotalDays)/365.25);
				}
				else
					return 0;

			}
		}
            //public List<string> newsLinks { get; set; }
            //public List<string> publishLinks { get; set; }

            [DisplayName("internet link")]
            public string Link { get; set; }

            [DisplayName("نوع النشاط")]
            public string typeOfComp { get; set; }

        }
    public class Program
    {
        public static IKernel kernel = null;
        static Program()
        {
            kernel = new StandardKernel(new MyNinjectModule());
        }
        private static List<string> visited = new List<string>();
        private static object syncObj = new Object();
        private static object syncFileObj = new Object();
        

        public static void tryUntilSuccess (ThreadStart func, int retry = 10)
        {
            int count = 0;
            Exception ex = null;
            do
            {
                count++;
                ex = null;
                try
                {
                    func();
                }
                catch (Exception exFound)
                {
                    ex = exFound;
                    
                }

            } while ((ex != null) && count <= retry);
        }

        public static object getProperty(string nameOfProperty, List<string> lstPage, Type type, string analysisPath = "analysis\\")
        {
            string strAnalysis = File.ReadAllText(analysisPath + nameOfProperty + ".txt", Encoding.UTF8);
			strAnalysis = strAnalysis.Replace ("\r", "");

            List<string> lstAnalysis = strAnalysis.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries).ToList();
            string val =  getValue(lstAnalysis, lstPage);
            if (type == typeof(double))
            {

                double newVal = getDoubleValue(val);
                return newVal;
            }
            return val;
        }

        private static double getDoubleValue(string val)
        {
            val = Regex.Replace(val, "[^\\-.,0-9]+", "");
            int checkComma = val.LastIndexOf(",");
            if (checkComma >= 0)
            {
                if (checkComma >= (val.Length - 3))
                {
                    val = val.Substring(0, checkComma) + "." + val.Substring(checkComma + 1);
                }
            }
            double newVal = 0;
            double.TryParse(val, out newVal);
            return newVal;
        }

        private static string getValue(List<string> lstAnalysis, List<string> lstPage)
        {
            if (lstAnalysis.Count < 6)
            {
                throw new ArgumentException("Not enough analysis");
            }

            int first = getTagPlace(0, lstPage, lstAnalysis[2]);
            if (first == -1)
            {
                first = getTagPlace(0, lstPage, lstAnalysis[1]);
                if (first == -1)
                {
                    first = getTagPlace(0, lstPage, lstAnalysis[0]);
                    if (first == -1)
                    {
                        throw new ArgumentException("Analysis fail");
                    }
                }
            }

            int last = getTagPlace(first, lstPage, lstAnalysis[3]);
            if (last == -1)
            {
                last = getTagPlace(first, lstPage, lstAnalysis[4]);
                if (last == -1)
                {
                    last = getTagPlace(first, lstPage, lstAnalysis[5]);
                    if (last == -1)
                    {
                        throw new ArgumentException("Analysis fail");
                    }
                }
            }


            StringBuilder val = new StringBuilder();
            for (int i = first + 1; i < last; i++)
            {
                string newVal = lstPage[i];
                newVal = Regex.Replace(newVal, "<[^>]+>", "");
                val.Append(newVal);
            }
            return val.ToString();
        }

        private static int getTagPlace(int startIndex, List<string> lstPage, string tag)
        {
            int index = -1;
            for (int i = startIndex+1; i < lstPage.Count; i++)
            {
                if (lstPage[i].Equals(tag, StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }
            return index;
        }



        public static List<string> getHtmlPageInList(string html)
        {
            List<string> tagsRet = new List<string>();
            Regex reg = new Regex("<[^>]+>", RegexOptions.IgnoreCase);
            Match msh = reg.Match(html);
            int lastIndex = 0;
            while (msh.Success)
            {
                if (msh.Index > lastIndex)
                {
                    string found = html.Substring(lastIndex, msh.Index - lastIndex).Trim();
                    if (!string.IsNullOrEmpty(found))
                    {
                        tagsRet.Add(found);
                    }
                    
                }
                lastIndex = msh.Index + msh.Length;
                if (!msh.Value.StartsWith("</"))
                {
                    tagsRet.Add(msh.Value);
                }
                msh =  msh.NextMatch();
            }
            return tagsRet;
        }
        private static bool isWriteFile = false;
        public static string analysisFolder = "";
		public static List<borsaModel> lstModels = new List<borsaModel> ();
        public static void Main(string[] args)
        {
            string LinksExtractor = ConfigurationManager.AppSettings["LinksExtractor"];
            IGetLinks linksExtractorClass = kernel.Get<IGetLinks>(LinksExtractor);
            if (args.Length > 0)
            {
                isWriteFile = true;
            }
            File.WriteAllText("output.html", "");
            writeHeader();
            
            

            


            int threadCount = int.Parse(ConfigurationManager.AppSettings["threadCount"]);

            bool bHaveAnnounce = bool.Parse(ConfigurationManager.AppSettings["haveAnnounce"]);
            string annUrl = ConfigurationManager.AppSettings["annUrl"];

            analysisFolder = ConfigurationManager.AppSettings["analysisFolder"];





            List<borsaModel> lst = linksExtractorClass.getLinks("").ToList();


                List<Thread> lstThreads = new List<Thread>();

                while (lstThreads.Count < threadCount)
                {
                    Thread thNew = new Thread(() => { 

                    foreach (borsaModel model in lst)
                    {
                        bool bNotFound = false;
                        lock (syncObj)
                        {
                            if (!visited.Contains(model.Link))
                            {
                                visited.Add(model.Link);
                                bNotFound = true;

                            }
                        }

                        if (bNotFound)
                        {
                            getBorsaModel(model);
                            model.marketValCalc = model.noOfStocks * model.last;
                            if (bHaveAnnounce)
                            {
                                getAnnounceOfModel(annUrl, model);
                            }
                            
                            //lst.Add(model);
							Console.WriteLine(model.ruitersCode + "     " + model.currentPrice + "     " + model.profit + "     " + (model.lastProfitGrowFullStocksQuarterCalculated??0.0));
                            //writeModelText(model);
							lstModels.Add(model);
                            //Thread.Sleep(1000);
                        }

                    }
                    });
                    thNew.Start();
                    lstThreads.Add(thNew);
                }
                foreach (Thread th in lstThreads)
                {
                    if (th.ThreadState != ThreadState.Stopped)
                    {
                        th.Join();
                    }
                }


			lstModels = lstModels.OrderBy (b => b.lastProfitGrowFullStocksCalculated).OrderBy (b => b.YearsDifference).ToList();


			for (int i = 0; i < lstModels.Count (); i++) {
				writeModelText (lstModels [i]);
			}
            
            File.AppendAllText("output.html", "\r\n</table>\r\n</body></html>");
        }

        private static borsaModel getAnnounceOfModel(string annUrl, borsaModel model)
        {
            string link = model.Link;
            string annLink = link + annUrl;

            WebClient client = new WebClient();
            client.Headers.Add("User-Agent", ConfigurationManager.AppSettings["browserAgent"]);
            client.Encoding = Encoding.UTF8;
            string ret = "";
            tryUntilSuccess(() => { ret = client.DownloadString(annLink); });
            string tableRowReg = @"\<tr[^/\>]*\>";
            string endRowReg = @"\</tr\>";
            Match mch = Regex.Match(ret, tableRowReg, RegexOptions.IgnoreCase);
            while (mch.Success)
            {
                Match end = Regex.Match(ret.Substring(mch.Index), endRowReg, RegexOptions.IgnoreCase);

                string inBetween = ret.Substring(mch.Index, end.Index + end.Length);

                if (inBetween.Contains("مدققة"))
                {
                    List<string> cells = new List<string>();

                    string tableCellReg = @"\<td[^/\>]*\>";
                    string endCellReg = @"\</td\>";
                    Match cellMatch = Regex.Match(inBetween, tableCellReg, RegexOptions.IgnoreCase);
                    while (cellMatch.Success)
                    {
                        Match endCellMatch = Regex.Match(inBetween.Substring(cellMatch.Index), endCellReg, RegexOptions.IgnoreCase);
                        string cellString = inBetween.Substring(cellMatch.Index, endCellMatch.Index + endCellMatch.Length);
                        cells.Add(cellString);
                        cellMatch = cellMatch.NextMatch();
                    }

                    string date = Regex.Replace(cells[0], "<[^>]+>", "", RegexOptions.IgnoreCase);
                    string ann = Regex.Replace(cells[1], "<[^>]+>", "", RegexOptions.IgnoreCase);
                    string annComp = Regex.Replace(cells[2], "<[^>]+>", "", RegexOptions.IgnoreCase);
                    string note = Regex.Replace(cells[4], "<[^>]+>", "", RegexOptions.IgnoreCase);

                    model.profitDate = date;
                    model.profitAnn = getDoubleValue(ann);
                    model.profitComp = getDoubleValue(annComp);
                    model.profitType = note;

                    return model;
                    
                }

                mch = mch.NextMatch();
            }
          


            return model;
        }

        public static string getFullUrl(string mainUrl, string foundUrl)
        {
            if (mainUrl.StartsWith("local://", StringComparison.OrdinalIgnoreCase))
            {
                return foundUrl;
            }
            string ret = "";
            Uri mainUri = new Uri(mainUrl);
            if (foundUrl.StartsWith("/") || foundUrl.StartsWith("\\"))
            {
                string host = mainUri.Host;
                ret = "http://" + host + foundUrl;
            }
            else
            {
                ret = mainUrl.Substring(0,mainUrl.LastIndexOf("/")) + "/" + foundUrl;

            }

            if (ret.LastIndexOf("/") >= 0)
            {
                ret = ret.Substring(0, ret.LastIndexOf("/") + 1) + System.Web.HttpUtility.UrlEncode(ret.Substring(ret.LastIndexOf("/") + 1));
            }
            
            
            return ret;
            
        }

        private static void writeModelText(borsaModel borsaModel)
        {
            StringBuilder sb = new StringBuilder();
            PropertyInfo[] pis = borsaModel.GetType().GetProperties();
            sb.AppendLine("<tr>");
            foreach (PropertyInfo pi in pis)
            {
				string value = (pi.GetValue (borsaModel, null) ?? "").ToString();

				sb.Append("<td style='border:1px solid black;'>");
			 
				if (pi.Name == "Link")
					sb.Append("<a href=\""+ value +"\">");
                sb.Append(value);
				if (pi.Name == "Link")
					sb.Append("</a>");
                sb.AppendLine("</td>");
            }
            sb.AppendLine("</tr>");
            lock (syncFileObj)
            {
                File.AppendAllText("output.html", sb.ToString());
            }
        }


        private static void writeHeader()
        {
            StringBuilder sb = new StringBuilder();
            PropertyInfo[] pis = typeof(borsaModel).GetProperties();
			sb.AppendLine("<!DOCTYPE html><html><head><meta charset=\"UTF-8\"></head><body>\r\n<table>\r\n<tr>");
            foreach (PropertyInfo pi in pis)
            {
                sb.Append("<td>");
                object[] arr = pi.GetCustomAttributes(typeof(DisplayNameAttribute), false);
                if (arr.Length > 0)
                {
                    sb.AppendLine(((DisplayNameAttribute)arr[0]).DisplayName);
                }
                else
                {
                    sb.AppendLine(pi.Name);
                }
                sb.AppendLine("</td>");
            }
            sb.AppendLine("</tr>");

            File.AppendAllText("output.html", sb.ToString());
        }

        private static borsaModel getBorsaModel(borsaModel model)
        {
            string link = model.Link;
            WebClient client = new WebClient();
            client.Headers.Add("User-Agent", ConfigurationManager.AppSettings["browserAgent"]);
            client.Encoding = Encoding.UTF8;
            string ret = "";
            tryUntilSuccess(() => { ret = client.DownloadString(link);});
          
            List<string> lstPage = getHtmlPageInList(ret);
            if (isWriteFile)
            {
                writeFileOfPage(lstPage);
            }
           

            PropertyInfo[] pis = model.GetType().GetProperties();
            foreach (PropertyInfo pi in pis)
            {
                try
                {
                    if (pi.GetValue(model, null) == null || pi.GetValue(model, null).ToString().Trim() == "" || Regex.IsMatch( pi.GetValue(model, null).ToString().Trim(),"^[0.]+$"))
                    {
                        object val = getProperty(pi.Name, lstPage, pi.PropertyType, analysisFolder);
                        pi.SetValue(model, val, null);
                    }
                }
				catch (Exception ex)
                {
					//Console.WriteLine (ex.ToString());
                }
            }


            return model;
        }

        private static void writeFileOfPage(List<string> lstPage)
        {
            StringBuilder sb = new StringBuilder();
            foreach (string pageLine in lstPage)
            {
                sb.AppendLine(pageLine);
            }
            string dir = ".\\temp\\";
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            string fileName = Guid.NewGuid() + ".txt";
            fileName = dir + fileName;
            lock (syncFileObj)
            {
                File.WriteAllText(fileName, sb.ToString(), Encoding.UTF8);
            }

        }
    }

    }