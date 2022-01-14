using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;

namespace BadmintonCourtAutoBooker
{
    internal class BookingBot
    {
        private string baseUrl { get; set; }

        private string moduleName { get; set; }

        private RestClient restClient { get; set; }

        private CookieContainer cookieContainer { get; set; }

        public BookingBot(string baseUrl, string moduleName)
        {
            this.baseUrl = baseUrl;
            this.moduleName = moduleName;
            InitRestClient();
        }

        public LoginStatus Login(string username, string password)
        {
            try
            {
                RestRequest restRequest = new RestRequest(moduleName);
                restRequest.AddQueryParameter("module", "login_page");
                restRequest.AddQueryParameter("files", "login");

                IRestResponse restResponse = restClient.Get(restRequest);
                if (!(restResponse.Content.Contains("會員註冊/登入") || restResponse.Content.Contains("修改會員資料")))
                {
                    return LoginStatus.ServiceTime;
                }

                string base64Str = "";
                byte[] captchaBytes = restClient.DownloadData(new RestRequest("NewCaptcha.aspx"));
                using (MemoryStream memoryStream = new MemoryStream())
                {
                    memoryStream.Write(captchaBytes, 0, captchaBytes.Length);
                    Bitmap bitmap = GifToBitmap(memoryStream);
                    base64Str = BitmapToBase64(bitmap);
                }

                RestClient ocrClient = new RestClient("https://ocr.holey.cc");
                RestRequest ocrRequest = new RestRequest("aspx_5c");
                ocrRequest.AddQueryParameter("base64_str", base64Str);

                string ocrRespContent = ocrClient.Get(ocrRequest).Content;
                string captchaCode = "";
                JObject jObject = (JObject)JsonConvert.DeserializeObject(ocrRespContent);
                if (jObject["status"].ToString() == "success")
                {
                    captchaCode = jObject["data"].ToString();
                }
                else
                {
                    return LoginStatus.CaptchaOcrError;
                }

                restRequest = new RestRequest(moduleName);
                restRequest.AddQueryParameter("module", "login_page");
                restRequest.AddQueryParameter("files", "login");
                restRequest.AddParameter("loginid", username);
                restRequest.AddParameter("loginpw", password);
                restRequest.AddParameter("Captcha_text", captchaCode);

                restResponse = restClient.Post(restRequest);
                string respContent = restResponse.Content;
                if (respContent.Contains("修改會員資料"))
                {
                    return LoginStatus.Success;
                }
                else
                {
                    string[] firstLine = respContent.Split('\n')[0].Trim('\r').Split(',');
                    if (firstLine[0] == "1")
                    {
                        return LoginStatus.IncorrectUsernameOrPassword;
                    }
                    else if (firstLine[0] == "2" && firstLine[1] == "驗證碼錯誤")
                    {
                        return LoginStatus.CaptchaCodeError;
                    }
                    else
                    {
                        return LoginStatus.Others;
                    }
                }
            }
            catch (Exception)
            {
                return LoginStatus.Others;
            }
        }

        public void Logout()
        {
            RestRequest restRequest = new RestRequest(moduleName);
            restRequest.AddQueryParameter("module", "ind");
            restRequest.AddQueryParameter("files", "ind");
            restRequest.AddQueryParameter("tFlag", "5");
            restClient.Get(restRequest);

            InitRestClient();
        }

        public bool CheckServiceTime()
        {
            RestRequest restRequest = new RestRequest(moduleName);
            restRequest.AddQueryParameter("module", "ind");
            restRequest.AddQueryParameter("files", "ind");
            return restClient.Get(restRequest).Content.Contains("系統維護中");
        }

        public bool CheckDataStatus(DateTime destDate)
        {
            RestRequest restRequest = new RestRequest(moduleName);
            restRequest.AddQueryParameter("module", "net_booking");
            restRequest.AddQueryParameter("files", "booking_place");
            restRequest.AddQueryParameter("StepFlag", "2");
            restRequest.AddQueryParameter("PT", "1");
            restRequest.AddQueryParameter("D", destDate.ToString("yyyy/MM/dd"));

            string respContent = restClient.Get(restRequest).Content.Replace("\r", "").Replace("\n", "");
            HtmlDocument htmlDocument = new HtmlDocument();
            htmlDocument.LoadHtml(respContent);

            return htmlDocument.DocumentNode.SelectNodes("//*[@id='ContentPlaceHolder1_Date_Lab']") == null;
        }

        public Dictionary<int, List<Court>> CheckCourtStatus(DateTime destDate, List<int> timepartCodes)
        {
            var courtStatusDict = new Dictionary<int, List<Court>>();
            for (int i = 1; i <= 3; i++)
            {
                courtStatusDict.Add(i, new List<Court>());
            }

            foreach (int timepartCode in timepartCodes)
            {
                RestRequest restRequest = new RestRequest(moduleName);
                restRequest.AddQueryParameter("module", "net_booking");
                restRequest.AddQueryParameter("files", "booking_place");
                restRequest.AddQueryParameter("StepFlag", "2");
                restRequest.AddQueryParameter("PT", "1");
                restRequest.AddQueryParameter("D", destDate.ToString("yyyy/MM/dd"));
                restRequest.AddQueryParameter("D2", timepartCode.ToString());

                string respContent = restClient.Get(restRequest).Content.Replace("\r", "").Replace("\n", "");
                HtmlDocument htmlDocument = new HtmlDocument();
                htmlDocument.LoadHtml(respContent);

                HtmlNode statusTable = htmlDocument.DocumentNode.SelectSingleNode("//*[@id='ContentPlaceHolder1_Step2_data']").SelectSingleNode(".//table");
                if (statusTable != null)
                {
                    HtmlNodeCollection trNodes = statusTable.ChildNodes;
                    foreach (HtmlNode trNode in trNodes)
                    {
                        if (trNode.ChildNodes.Count == 0)
                        {
                            continue;
                        }

                        if (trNode.SelectSingleNode("./td[4]/img") == null)
                        {
                            continue;
                        }

                        bool courtStatus = trNode.SelectSingleNode("./td[4]/img").Attributes["src"].Value == "img/sche01.png";
                        string courtCode = "";
                        string timeCode = "";

                        if (courtStatus)
                        {
                            string onclickContent = trNode.SelectSingleNode("./td[4]/img").Attributes["onclick"].Value;
                            Match match = new Regex(@"^.+Step3Action\((\d+),(\d+)\).+$", RegexOptions.Compiled | RegexOptions.IgnoreCase).Match(onclickContent);
                            if (match.Success)
                            {
                                courtCode = match.Groups[1].Value;
                                timeCode = match.Groups[2].Value;
                            }
                        }

                        courtStatusDict[timepartCode].Add(new Court()
                        {
                            Date = destDate,
                            Time = trNode.SelectSingleNode("./td[1]").InnerText.Replace(" ", ""),
                            Name = trNode.SelectSingleNode("./td[2]").InnerText.Replace(" ", ""),
                            Rent = int.Parse(trNode.SelectSingleNode("./td[3]").InnerText.Replace(" ", "")),
                            Available = courtStatus,
                            Id = courtCode == "" ? -1 : int.Parse(courtCode),
                            TimeCode = timeCode == "" ? -1 : int.Parse(timeCode)
                        });
                    }
                }
            }

            return courtStatusDict;
        }

        public bool BookCourt(Court court)
        {
            RestRequest restRequest = new RestRequest(moduleName);
            restRequest.AddQueryParameter("module", "net_booking");
            restRequest.AddQueryParameter("files", "booking_place");
            restRequest.AddQueryParameter("StepFlag", "25");
            restRequest.AddQueryParameter("QPid", court.Id.ToString());
            restRequest.AddQueryParameter("QTime", court.TimeCode.ToString());
            restRequest.AddQueryParameter("PT", "1");
            restRequest.AddQueryParameter("D", court.Date.ToString("yyyy/MM/dd"));

            string respContent = restClient.Get(restRequest).Content.Replace("\r", "").Replace("\n", "");
            Match match = new Regex(@"<script>(.+?)<\/script>", RegexOptions.Compiled | RegexOptions.IgnoreCase).Match(respContent);
            if (match.Success)
            {
                respContent = match.Groups[1].Value.Replace(" ", "");
                match = new Regex(@"window\.location\.href=\'(\.\.\/)*.+\?(.+)\'", RegexOptions.Compiled | RegexOptions.IgnoreCase).Match(respContent);
                if (match.Success)
                {
                    restRequest = new RestRequest(moduleName);
                    foreach (string keyValuePair in match.Groups[2].Value.Split('&'))
                    {
                        string[] keyValue = keyValuePair.Split('=');
                        restRequest.AddQueryParameter(keyValue[0], keyValue[1]);
                    }

                    return restClient.Get(restRequest).Content.Contains("預約成功");
                }
            }

            return false;
        }

        public bool BookCourt(int courtCode, int timeCode, DateTime destDate)
        {
            RestRequest restRequest = new RestRequest(moduleName);
            restRequest.AddQueryParameter("module", "net_booking");
            restRequest.AddQueryParameter("files", "booking_place");
            restRequest.AddQueryParameter("StepFlag", "25");
            restRequest.AddQueryParameter("QPid", courtCode.ToString());
            restRequest.AddQueryParameter("QTime", timeCode.ToString());
            restRequest.AddQueryParameter("PT", "1");
            restRequest.AddQueryParameter("D", destDate.ToString("yyyy/MM/dd"));

            string respContent = restClient.Get(restRequest).Content.Replace("\r", "").Replace("\n", "");
            Match match = new Regex(@"<script>(.+?)<\/script>", RegexOptions.Compiled | RegexOptions.IgnoreCase).Match(respContent);
            if (match.Success)
            {
                respContent = match.Groups[1].Value.Replace(" ", "");
                match = new Regex(@"window\.location\.href=\'(\.\.\/)*.+\?(.+)\'", RegexOptions.Compiled | RegexOptions.IgnoreCase).Match(respContent);
                if (match.Success)
                {
                    restRequest = new RestRequest(moduleName);
                    foreach (string keyValuePair in match.Groups[2].Value.Split('&'))
                    {
                        string[] keyValue = keyValuePair.Split('=');
                        restRequest.AddQueryParameter(keyValue[0], keyValue[1]);
                    }

                    return restClient.Get(restRequest).Content.Contains("預約成功");
                }
            }

            return false;
        }

        private void InitRestClient()
        {
            restClient = new RestClient($"{baseUrl.Trim('/')}");
            restClient.AddDefaultHeaders(new Dictionary<string, string>()
            {
                { "Accept-Encoding", "gzip, deflate, br" },
                { "Accept-Language", "en-US,en;q=0.9,zh-TW;q=0.8,zh;q=0.7" },
                { "Connection", "keep-alive" },
                { "User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/95.0.4638.54 Safari/537.36 Edg/95.0.1020.30" },
                { "X-Requested-With", "XMLHttpRequest" }
            });

            cookieContainer = new CookieContainer();
            restClient.CookieContainer = cookieContainer;
        }

        private Bitmap GifToBitmap(Stream imageStream)
        {
            Image gif = Image.FromStream(imageStream);
            FrameDimension dim = new FrameDimension(gif.FrameDimensionsList[0]);
            Bitmap bitmap = new Bitmap(gif.Width, gif.Height);

            gif.SelectActiveFrame(dim, 0);
            Rectangle destRegion = new Rectangle(0, 0, gif.Width, gif.Height);
            Rectangle srcRegion = new Rectangle(0, 0, gif.Width, gif.Height);
            using (Graphics graphics = Graphics.FromImage(bitmap))
            {
                graphics.DrawImage(gif, destRegion, srcRegion, GraphicsUnit.Pixel);
            }

            return bitmap;
        }

        private string BitmapToBase64(Bitmap bitmap)
        {
            using var ms = new MemoryStream();
            bitmap.Save(ms, ImageFormat.Jpeg);
            return Convert.ToBase64String(ms.GetBuffer());
        }

        public enum LoginStatus
        {
            Success,
            IncorrectUsernameOrPassword,
            CaptchaOcrError,
            CaptchaCodeError,
            ServiceTime,
            Others
        }
    }
}
