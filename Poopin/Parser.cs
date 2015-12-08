using HtmlAgilityPack;
using Poopin.DELFI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Poopin
{
    class Parser
    {
        public int cc = 0;
        public List<Delfi> NewDelfiComments = new List<Delfi>();
        public string gip = "0";
        public string gurl = "";
        public int ParseComments()
        {
            try
            {
                GetCommentsFromDelfi();
                return cc;
            }
            catch
            {
                return -1;
            }
        }
        public void GetCommentsFromDelfi()
        {
            WebClient client = new WebClient();
            string downloadString = client.DownloadString("http://m.delfi.lt/");
            List<LinkItem> AllLinks = new List<LinkItem>();
            AllLinks = Find(downloadString);
            AllLinks.ForEach(i=>FindComents(i.Href, i.Text));
        }
        public struct LinkItem
        {
            public string Href;
            public string Text;

            public override string ToString()
            {
                return Href + "\n\t" + Text;
            }
        }
        public static List<LinkItem> Find(string file)
        {
            List<LinkItem> list = new List<LinkItem>();

            // 1.
            // Find all matches in file.
            MatchCollection m1 = Regex.Matches(file, @"(<a.*?>.*?</a>)",
                RegexOptions.Singleline);

            // 2.
            // Loop over each match.
            foreach (Match m in m1)
            {
                string value = m.Groups[1].Value;
                LinkItem i = new LinkItem();

                // 3.
                // Get href attribute.
                Match m2 = Regex.Match(value, @"href=\""(.*?)\""",
                RegexOptions.Singleline);
                if (m2.Success)
                {
                    i.Href = m2.Groups[1].Value;
                }

                // 4.
                // Remove inner tags from text.
                string t = Regex.Replace(value, @"\s*<.*?>\s*", "",
                RegexOptions.Singleline);
                i.Text = t;

                list.Add(i);
            }
            return list;
        }
        public void FindComents(string href, string Text)
        {
            
            List<string> CommentIDs = new List<string>();
            List<string> CommentIPs = new List<string>();
            List<string> CommentTexts = new List<string>();
            List<string> CommentTitles = new List<string>();
            List<string> CommentDates = new List<string>();
            List<string> ArticleUrl = new List<string>();
            if (href != null)
            {
                if(!href.Contains("http://www.delfi.lt/video/"))
                {
                if (href.Contains("com=1"))
                {
                    href = href.Replace("amp;", string.Empty);
                    string newhref = href + @"&reg=0&no=0&s=2";
                    HtmlWeb hw = new HtmlWeb();
                    HtmlDocument doc = hw.Load(@newhref);
                    DateTime CommentDate = new DateTime();
                    string CommentTitle = "";
                    string CommUrl = href.Replace("&com=1", string.Empty);
                    int i = 0;
                    int k = 0;
                    
                    foreach (HtmlNode node in doc.DocumentNode.SelectNodes("//div[@id='comments-list']"))
                    {
                        if(node != null)
                        {
                     HtmlNodeCollection hNodes = node.SelectNodes(".//div[@data-post-id]");
                     if(hNodes != null)
                     {                        
                        foreach (HtmlNode node2 in node.SelectNodes(".//div[@data-post-id]"))
                        {
                            if(node2 !=null)
                            {
                            string commentid = node2.GetAttributeValue("data-post-id", "");
                            cc++;
                            ArticleUrl.Add(CommUrl);
                            CommentIDs.Add(commentid);
                            CommentDates.Add(DateTime.Now.ToString());
                            foreach (HtmlNode node3 in node.SelectNodes(".//div[@class='comment-date']"))
                            {
                                string date = node3.InnerHtml;
                                List<string> Ips = new List<string>();
                                Ips = getParser(date, "<span>", "</span>");
                                foreach (string ip in Ips)
                                {
                                    i++;
                                    CommentIPs.Add(ip);
                                }
                            }

                            foreach (HtmlNode node5 in node.SelectNodes(".//div[@class='comment-author']"))
                            {
                                string date_ = node5.InnerHtml;
                                string Name = "";
                                Name = date_;
                                Name = Name.Replace("\r\n", string.Empty);
                                Name = Name.Replace("\n", string.Empty);
                                Name = Name.Replace("\t", string.Empty);
                                Name = Name.Replace("<br>", string.Empty);
                                CommentTitles.Add(Name);
                            }

                            foreach (HtmlNode node4 in node.SelectNodes(".//div[@class='comment-content']"))
                            {
                                string context = node4.InnerHtml;
                                var quotes = @"""";
                                List<string> commentcontents = new List<string>();
                                commentcontents = getParser(context, "<div class=" + quotes + "comment-content-inner" + quotes + ">", "</div>");
                                foreach (string content in commentcontents)
                                {
                                    k++;
                                    CommentTexts.Add(content);
                                }
                            }
                        }
                        }
                        }
                    }
                    }
                }
            }
                //Built Poopins
                int s = 0;
                foreach(string commid in CommentIDs)
                {
                    gip = commid;
                    gurl = ArticleUrl[s];
                    string commurl = ArticleUrl[s];
                    string commip = CommentIPs[s].Replace("IP:", string.Empty);
                    string commtitle = CommentTitles[s];
                    commip = commip.Replace(" ", string.Empty);
                    string commtext = CommentTexts[s].Replace("\r\n", string.Empty);
                    commtext = commtext.Replace("\n", string.Empty);
                    commtext = commtext.Replace("\t", string.Empty);
                    commtext = commtext.Replace("<br>", string.Empty);
                    InsertToSql(commid, commip, commurl, commtitle, commtext, DateTime.Now);
                    Console.WriteLine(@"Poopin ID: " + commid + "\nArticle Url: " + commurl + "\nPoopin Title:" + commtitle + "\nPoopin Date: " + CommentDates[s] + "\nPoopin IP: " + commip + "\nPoopin Text: " + commtext);
                    s++;
                }

                //Write to Database
            }
        }
        #region Helpers
        private void InsertToSql(string PoopinID,string IP,string ArticleURL,string Title, string Text, DateTime Created)
        {
            string cmdString = "IF NOT EXISTS(SELECT 1 FROM CommentObjects WHERE PoopinID = @val1) INSERT INTO CommentObjects (PoopinID,IP,ArticleURL,Title, Text, Created) VALUES (@val1, @val2, @val3, @val4, @val5, @val6)";
            string connString = "Data Source=TLT-SPS-08V\\SQLEXPRESS;" +
                "Trusted_Connection=True;"+
"Initial Catalog=Poopin;" +
"User id=PoopinAdmin;" +
"Password=abcd.1234;";
            using (SqlConnection conn = new SqlConnection(connString))
            {
                using (SqlCommand comm = new SqlCommand())
                {
                    comm.Connection = conn;
                    comm.CommandText = cmdString;
                    comm.Parameters.AddWithValue("@val1", PoopinID);
                    comm.Parameters.AddWithValue("@val2", IP);
                    comm.Parameters.AddWithValue("@val3", ArticleURL);
                    comm.Parameters.AddWithValue("@val4", Title);
                    comm.Parameters.AddWithValue("@val5", Text);
                    comm.Parameters.AddWithValue("@val6", Created);
                    conn.Open();
                    comm.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }
        private static List<string> getParser(string text, string startString, string endString)
        {
            List<string> matched = new List<string>();
            int indexStart = 0, indexEnd = 0;
            bool exit = false;
            while (!exit)
            {
                indexStart = text.IndexOf(startString);
                indexEnd = text.IndexOf(endString);
                if (indexStart != -1 && indexEnd != -1)
                {
                    matched.Add(text.Substring(indexStart + startString.Length,
                        indexEnd - indexStart - startString.Length));
                    text = text.Substring(indexEnd + endString.Length);
                }
                else
                    exit = true;
            }
            return matched;
        }
        #endregion
    }

}
