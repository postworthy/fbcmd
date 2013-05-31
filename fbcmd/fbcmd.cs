using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Web;
using System.Threading.Tasks;
using Facebook;
using System.Drawing.Imaging;
using System.IO;

namespace fbcmd
{
    public class fbcmd
    {
        private string Email { get; set; }
        private string Password { get; set; }
        private string Token { get; set; }
        private string  FILE_PATH 
        { 
            get
            {
                return ((Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX)
                    ? Environment.GetEnvironmentVariable("HOME") : Environment.ExpandEnvironmentVariables("%HOMEDRIVE%%HOMEPATH%")) + "\\Pictures\\FacebookImages\\";
            } 
        }

        public fbcmd()
        {
            if (!Directory.Exists(FILE_PATH)) Directory.CreateDirectory(FILE_PATH);
            Console.Write("Email: ");
            Email = Console.ReadLine();
            Console.Write("Password: ");
            Password = Console.ReadLine();
            Console.Clear();
            Console.WriteLine("Authenticating...");
            using (var login = new FBLogin())
            {
                login.Email = Email;
                login.Password = Password;
                Application.Run(login);
                if (string.IsNullOrEmpty(login.Token)) throw new Exception("Invalid Facebook Token");
                Token = login.Token;
            }
            Console.WriteLine("Success!");
            Console.Write("[" + Email + " ~]$ ");
        }

        public void Run()
        {
            string cmd = "";
            do
            {
                cmd = Console.ReadLine();
                switch (cmd)
                {
                    case "help":
                        Console.WriteLine("Commands:");
                        Console.WriteLine("help   - This response");
                        Console.WriteLine("logout - Log out from current user");
                        Console.WriteLine("exit   - Exit fbcmd");
                        Console.WriteLine("cls    - Clear console");
                        Console.WriteLine("dl     - Download your images to {0}", FILE_PATH);
                        break;
                    case "logout":
                        Console.Clear();
                        throw new Exception("Logout");
                        break;
                    case "cls":
                        Console.Clear();
                        break;
                    case "dl":
                        Console.WriteLine("Downloading...");
                        DownloadAllFiles().Wait();
                        break;
                }
                Console.Write("[" + Email + " ~]$ ");
            } while (cmd != "exit");
        }

        private Task DownloadAllFiles()
        {
            return Task.Factory.StartNew(new Action(() => Task.WaitAll(new Task[] { GetTaggedPhotos(), GetAlbumPhotos() })));
        }

        private Task GetTaggedPhotos()
        {
            return GetPhotos("me");
        }

        private Task GetAlbumPhotos()
        {
            return Task.Factory.StartNew(new Action(() =>
            {
                ProcessFacebookData("/me/albums", new Func<JSONObject, List<Task>>((data) =>
                {
                    List<Task> tasks = new List<Task>();
                    data.Array.ToList()
                        .ForEach(x => tasks.Add(GetPhotos(x.Dictionary["id"].String)));
                    return tasks;
                }));
            }));
        }

        private void ProcessFacebookData(string path, Func<JSONObject, List<Task>> task)
        {
            List<Task> tasks = new List<Task>();
            var api = new FacebookAPI(Token);
            var args = new Dictionary<string, string>();
            args.Add("limit", "100");
            var results = api.Get(path, args);
            var next = (results.Dictionary.ContainsKey("paging")) &&
                results.Dictionary["paging"].Dictionary.ContainsKey("next")
                ? results.Dictionary["paging"].Dictionary["next"].String : null;
            do
            {
                tasks.AddRange(task(results.Dictionary["data"]));

                if (!string.IsNullOrEmpty(next))
                {
                    args = new Dictionary<string, string>();
                    args.Add("until", next.Split(new string[] { "until=" }, StringSplitOptions.RemoveEmptyEntries)[1]);
                    args.Add("limit", "100");
                    results = api.Get(path, args);
                    if (results.Dictionary.ContainsKey("paging"))
                    {
                        if (next != results.Dictionary["paging"].Dictionary["next"].String)
                            next = results.Dictionary["paging"].Dictionary["next"].String;
                        else { next = null; results = null; }
                    }
                    else { next = null; results = null; }
                }
                else results = null;
            }
            while (results != null);
            Task.WaitAll(tasks.ToArray());
        }

        private Task GetPhotos(string path)
        {
            return Task.Factory.StartNew(new Action(() =>
            {
                ProcessFacebookData("/" + path + "/photos", new Func<JSONObject, List<Task>>((data) =>
                {
                    List<Task> tasks = new List<Task>();
                    data.Array.ToList()
                        .ForEach(x =>
                        tasks.Add(Task.Factory.StartNew(new Action(() =>
                        {
                            var fileName = x.Dictionary["id"].String + ".jpg";
                            if (!File.Exists(FILE_PATH + fileName))
                            {
                                new DownloadImage(x.Dictionary["source"].String)
                                    .Download()
                                    .SaveImage(FILE_PATH + fileName, ImageFormat.Jpeg);
                            }
                        }))));
                    return tasks;
                }));
            }));
        }
    }
}
