using HtmlAgilityPack;
using NETCommonClass;
using System.Configuration;
using WebCrawler.Model;
using HtmlDocument = HtmlAgilityPack.HtmlDocument;
using System.Linq;
using System.Text.RegularExpressions;

namespace WebCrawler
{
    public partial class Form1 : Form
    {

        Basic objBase = new Basic();

        public Form1()
        {
            InitializeComponent();
        }


        private void Form1_Load(object sender, EventArgs e)
        {
            objBase.db_ConnectionString = "Server=192.168.0.210;Database=ShopWebsite;User ID=sa;Password=Alex0310;Trusted_Connection=True;Integrated Security=False;Encrypt=False;";
        }


        private async void btn_Start_Click(object sender, EventArgs e)
        {
            pictureBox1.Visible = true;

            int page = Convert.ToInt32(txt_Num.Text);

            for (int i = 1; i <= page; i++)
            {
                // 新聞網站的URL
                string url = $"https://technews.tw/category/%e8%83%bd%e6%ba%90%e7%a7%91%e6%8a%80/page/{i}";

                // 使用HttpClient發送GET請求並獲取HTML頁面內容
                HttpClient client = new HttpClient();
                HttpResponseMessage response = await client.GetAsync(url);
                string htmlContent = await response.Content.ReadAsStringAsync();

                // 使用HtmlAgilityPack解析HTML內容
                HtmlDocument htmlDoc = new HtmlDocument();
                htmlDoc.LoadHtml(htmlContent);

                // 解析新聞標題和內容
                var contentNodes = htmlDoc.DocumentNode.SelectNodes("//div[@class='content']");
                foreach (var newsNode in contentNodes)
                {
                    // 文章標題 文章連結 文章描述 圖片連結
                    string title = newsNode.SelectSingleNode(".//h1[@class='entry-title']/a").InnerText;
                    string link = newsNode.SelectSingleNode(".//h1[@class='entry-title']/a")?.GetAttributeValue("href", "");
                    string description = newsNode.SelectSingleNode(".//div[@class='moreinf']/p")?.InnerText;
                    string imgLink = newsNode.SelectSingleNode(".//img[@class='attachment-medium size-medium wp-post-image']")?.GetAttributeValue("src", "");
                    // 選擇新聞標籤元素
                    var tagNodes = newsNode.SelectNodes(".//td/span[@class='body']");
                    // 獲取標籤內容
                    string tagContent = tagNodes[2].InnerText;
                    // 清理標籤內容，移除多餘的空白字符
                    string cleanedTagContent = Regex.Replace(tagContent, @"\s+", " ").Trim();



                    // 下載圖片取得圖片檔名
                    string imgName = await DownloadImage(imgLink);

                    // 取得文章內容
                    string contxt = await GetContxt(link, imgName);

                    News news = new News()
                    {
                        NewsClass = 8,
                        NewsTitle = title,
                        NewsDescription = description,
                        NewsContxt = contxt,
                        NewsImg1 = imgName,
                        NewsPublish = true,
                        NewsPutTime = DateTime.Now.AddDays(-1),
                        Creator = 1,
                        NewsOffTime = new DateTime(2030, 12, 31),
                        Tag = cleanedTagContent
                    };

                    string strSQL = string.Empty;

                    objBase.DB_Connection();

                    strSQL = "INSERT INTO News (NewsClass, NewsTitle, NewsDescription, NewsContxt, NewsImg1, NewsPublish, NewsPutTime, Creator, NewsOffTime, Tag) ";
                    strSQL += $" VALUES ('{news.NewsClass}', '{news.NewsTitle}', '{news.NewsDescription}', '{news.NewsContxt}', '{news.NewsImg1}', '{news.NewsPublish}', '{news.NewsPutTime?.ToString("yyyy-MM-dd HH:mm:ss")}', '{news.Creator}', '{news.NewsOffTime?.ToString("yyyy-MM-dd HH:mm:ss")}', '{news.Tag}')";
                    objBase.SqlExecute(strSQL);

                    objBase.DB_Close();


                    txt_Num.Text += Environment.NewLine;
                }
            }

            pictureBox1.Visible = false;
            MessageBox.Show("完成");
        }


        // 取得文章內容
        private async Task<string> GetContxt(string articleLink, string imgName)
        {
            // 使用HttpClient發送GET請求並獲取HTML頁面內容
            HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(articleLink);
            string htmlContent = await response.Content.ReadAsStringAsync();

            // 使用HtmlAgilityPack解析HTML內容
            HtmlDocument htmlDoc = new HtmlDocument();
            htmlDoc.LoadHtml(htmlContent);

            // 解析新聞文章
            var contentNode = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='entry-content']//div[@class='indent']");

            // 重組文章
            string content = "";
            for (int i = 0; i < contentNode.SelectNodes(".//p").Count; i++)
            {
                if (i == 0)
                {
                    content += string.Format("<p><img src=\"{0}\"></p>", "https://alexweb.ddns.net/uploads/News/" + imgName);
                }
                else
                {
                    content += "<p>" + contentNode.SelectNodes(".//p")[i].InnerText + "</p>";
                }
            }

            return content;
        }


        // 下載圖片
        private async Task<string> DownloadImage(string imageUrl)
        {
            // 取得應用程序的啟動路徑
            string startupPath = AppDomain.CurrentDomain.BaseDirectory;

            // 使用HttpClient下載圖片
            using (HttpClient client = new HttpClient())
            {
                byte[] imageBytes = await client.GetByteArrayAsync(imageUrl);

                // 取得圖片的檔名
                string fileName = Path.GetFileName(new Uri(imageUrl).AbsolutePath);

                // 組合完整的圖片路徑
                string imagePath = Path.Combine(startupPath, "Images", fileName);

                // 寫入圖片檔案
                File.WriteAllBytes(imagePath, imageBytes);

                return fileName;
            }
        }

        
    }
}