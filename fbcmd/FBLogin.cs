using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Web;

namespace fbcmd
{
    public partial class FBLogin : Form
    {
        private const string APP_ID = "190916760957195";
        private const string FB_LOGIN_URL = "https://www.facebook.com/dialog/oauth?response_type=token&client_id=" + APP_ID + "&redirect_uri=https://www.facebook.com/connect/login_success.html&scope=user_photo_video_tags,friends_photo_video_tags";

        public string Email { get; set; }
        public string Password { get; set; }
        public string Token { get; set; }

        private WebBrowserDocumentCompletedEventHandler DocumentCompleted;
        private bool allowed = false;
        
        public FBLogin()
        {
            ShowInTaskbar = false;
            WindowState = FormWindowState.Minimized;
            Load += new EventHandler(Window_Load);
        }

        void Window_Load(object sender, EventArgs e)
        {
            WebBrowser wb = new WebBrowser();
            wb.DocumentCompleted += new WebBrowserDocumentCompletedEventHandler(wb_DocumentCompleted);
            wb.Navigate(new Uri(FB_LOGIN_URL));
        }

        void wb_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            WebBrowser wb = sender as WebBrowser;
            if (wb.Document.Url.ToString().Contains(FB_LOGIN_URL))
            {
                var email = wb.Document.GetElementById("email");
                var password = wb.Document.GetElementById("pass");
                var submit = wb.Document.GetElementsByTagName("input").Cast<HtmlElement>().Where(x => x.GetAttribute("type") == "submit").FirstOrDefault();

                email.SetAttribute("value", Email);
                password.SetAttribute("value", Password);
                submit.InvokeMember("click");
            }
            else if (wb.Document.Url.ToString().StartsWith("https://www.facebook.com/login.php"))
            {
                var email = wb.Document.GetElementById("email");
                var password = wb.Document.GetElementById("pass");
                var submit = wb.Document.GetElementsByTagName("input").Cast<HtmlElement>().Where(x => x.GetAttribute("type") == "submit").FirstOrDefault();

                email.SetAttribute("value", Email);
                password.SetAttribute("value", Password);
                submit.InvokeMember("click");
            }
            else if (wb.Document.Url.ToString().StartsWith("https://www.facebook.com/connect/uiserver.php"))
            {
                if (!allowed)
                {
                    var allow = wb.Document.GetElementsByTagName("input").Cast<HtmlElement>().Where(x => x.GetAttribute("value") == "Allow" && x.GetAttribute("type") == "submit").FirstOrDefault();
                    allow.InvokeMember("click");
                    allowed = true;
                }
            }
            else if (wb.Document.Url.ToString().Contains("login_success.html#access_token"))
            {
                Token = HttpUtility.UrlDecode(wb.Document.Url.ToString()
                    .Split(new string[] { "#access_token=" }, StringSplitOptions.RemoveEmptyEntries)[1]
                    .Split('&')[0]);
                Application.Exit();
            }
            else throw new Exception("Unexpected URL!");
        }
    }

}
