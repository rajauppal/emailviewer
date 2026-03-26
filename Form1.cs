using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Xml;
using static System.Windows.Forms.Design.AxImporter;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;
using UniqueId = MailKit.UniqueId;

namespace WinFormsApp3
{
    public partial class Form1 : Form
    {
        private CancellationTokenSource _cts;

        int listBox2SelectedIndex = 0;
        int listBox1SelectedIndex = 0;

        string printQueuePath = "";   

        public Form1()
        {
            InitializeComponent();
        }

        private void fileSystemWatcher1_Created(object sender, FileSystemEventArgs e)
        {
            try
            {
                System.Threading.Thread.Sleep(1000);

                FileInfo fileInfo = new FileInfo(e.FullPath);

                LoadFolderInformation(new DirectoryInfo(e.FullPath));
            }
            catch (Exception)
            {

            }

        }

        private async Task ProcessEmails()
        {
            using var client = new ImapClient();

            await client.ConnectAsync("imap.gmail.com", 993, true);
            await client.AuthenticateAsync("ups5284@gmail.com", "wypvmgojcjoujdyn");

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite);

            var since = DateTime.UtcNow.AddHours(-10);

            uint minDir = 0;


  
            var directories = Directory.GetDirectories(printQueuePath);
            foreach (var item in directories)
            {
                if (int.TryParse(Path.GetFileName(item), out int id))
                {
                    if (id > minDir)
                    {
                        minDir = (uint)id;
                    }
                }
            }

            UniqueIdRange range;

            if (minDir == 0)
            {
                var uids = await inbox.SearchAsync(SearchQuery.All);

                range = new UniqueIdRange(uids[uids.Count - 10], uids.Last());
            }
            else
            {
                var min = new UniqueId(minDir);

                var max = UniqueId.MaxValue;
                range = new UniqueIdRange(min, max);
            }


            var summaries = await inbox.FetchAsync(
                range,
                MessageSummaryItems.Envelope
            );


            foreach (var uid in summaries.Select(x => x.UniqueId))
            {
                var dir = new DirectoryInfo(printQueuePath + $@"\{uid}");

                if (Directory.Exists(dir.FullName) && dir.GetFiles().Length > 0)
                {
                    continue;
                }

                //if (processed.Contains(uid.Id))
                //    continue;


                var message = await inbox.GetMessageAsync(uid);

                if (message.Date.UtcDateTime < since)
                    continue;

                // Extract sender mailbox details
                var mailbox = message.From.Mailboxes.FirstOrDefault();
                var senderEmail = mailbox?.Address ?? "unknown";
                var senderName = mailbox?.Name;

                if (string.IsNullOrWhiteSpace(senderName))
                {
                    try
                    {
                        senderName = senderEmail.Split('@', 2)[0];
                    }
                    catch
                    {
                        senderName = "Unknown";
                    }
                }

                Console.WriteLine($"Processing: {message.Subject} from {senderName} <{senderEmail}> ({message.Date})");

                var baseDir = printQueuePath + $@"\{uid.Id}";
                Directory.CreateDirectory(baseDir);

                string emailMetadata = JsonSerializer.Serialize(new
                {
                    Subject = message.Subject,
                    FromName = senderName,
                    FromEmail = senderEmail,
                    Date = message.Date,
                    Id = uid.Id,
                }, new JsonSerializerOptions { WriteIndented = true });

                File.WriteAllText(Path.Combine(baseDir, "metadata.json"), emailMetadata);

                foreach (var part in message.BodyParts)
                {
                    if (part is not MimePart mimePart)
                        continue;

                    var mime = mimePart.ContentType.MimeType.ToLower();

                    var allowedMimeTypes = new[]
                    {
                    "application/pdf",
                    "image/jpeg",
                    "image/png",
                    "image/jpg",
                    "application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                };

                    if (!allowedMimeTypes.Contains(mime))
                        continue;

                    var isInline = mimePart.ContentDisposition?.Disposition?.Equals("inline", StringComparison.OrdinalIgnoreCase) == true;
                    var size = mimePart.ContentDisposition?.Size ?? 0;

                    // Skip tiny inline junk only
                    if (isInline && size > 0 && size < 15_000)
                        continue;

                    var fileName =
                        mimePart.ContentDisposition?.FileName
                        ?? mimePart.ContentType.Name
                        ?? mimePart.FileName;

                    string ext = mime switch
                    {
                        "image/jpeg" => ".jpg",
                        "image/png" => ".png",
                        "application/pdf" => ".pdf",
                        "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
                        _ => null
                    };

                    if (ext == null)
                        continue;

                    if (string.IsNullOrWhiteSpace(fileName))
                        fileName = $"{Guid.NewGuid()}{ext}";

                    Directory.CreateDirectory(baseDir);
                    fileName = SanitizeFileName(fileName);

                    var filePath = Path.Combine(baseDir, $"{Guid.NewGuid()}_{fileName}");


                    try
                    {
                        using var stream = File.Create(filePath);
                        await mimePart.Content.DecodeToAsync(stream);
                        await stream.FlushAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                        throw;
                    }


                    Console.WriteLine($"Saved: {filePath} ({new FileInfo(filePath).Length} bytes)");
                }

                var html = message.HtmlBody;
                var matches = Regex.Matches(html ?? "", "<img[^>]+src=\"([^\"]+)\"");

                using var http = new HttpClient();

                foreach (Match match in matches)
                {
                    var url = match.Groups[1].Value;

                    if (!url.StartsWith("http"))
                        continue;

                    byte[] bytes;

                    try
                    {
                        bytes = await http.GetByteArrayAsync(url);
                    }
                    catch
                    {
                        continue;
                    }

                    // 🔥 SKIP SMALL IMAGES (THIS IS WHAT YOU WANT)
                    if (bytes.Length < 20_000) // <20KB
                        continue;

                    var ext = Path.GetExtension(url);

                    if (string.IsNullOrWhiteSpace(ext))
                        ext = ".jpg";

                    var filePath = printQueuePath + $@"\{uid.Id}\img_{Guid.NewGuid()}{ext}";

                    await File.WriteAllBytesAsync(filePath, bytes);

                    Console.WriteLine($"Saved image: {filePath} ({bytes.Length} bytes)");
                }

                matches = Regex.Matches(html ?? "", "href\\s*=\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase);

                int i = 0;
                foreach (Match match in matches)
                {

                    var url = match.Groups[1].Value;

                    if (url.Contains("amazon.com", StringComparison.OrdinalIgnoreCase) && url.Contains("label", StringComparison.OrdinalIgnoreCase))
                    {
                        i++;

                        Console.WriteLine("Found Amazon SPR link: " + url);

                        var filePath = printQueuePath + $@"\{uid.Id}\Link-{i}.txt";

                        await File.WriteAllTextAsync(filePath, url);

                        // 👉 do whatever you want here
                        // e.g. mark email as printable / special handling
                    }
                    if (url.Contains("docs.google.com", StringComparison.OrdinalIgnoreCase))
                    {
                        i++;

                        Console.WriteLine("Google Drive link: " + url);

                        var filePath = printQueuePath + $@"\{uid.Id}\Link-{i}.txt";

                        await File.WriteAllTextAsync(filePath, url);

                        // 👉 do whatever you want here
                        // e.g. mark email as printable / special handling
                    }
                }

               
                await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true);
            }

            await client.DisconnectAsync(true);
        }

        public static string SanitizeFileName(string fileName, string defaultName = "file")
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return defaultName;

            var invalidChars = Path.GetInvalidFileNameChars();

            var cleaned = new string(fileName
                .Select(c => invalidChars.Contains(c) ? '_' : c)
                .ToArray())
                .Trim();

            return string.IsNullOrWhiteSpace(cleaned) ? defaultName : cleaned;
        }

        private async Task StartEmailChecker(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    await ProcessEmails();

                    await Task.Delay(TimeSpan.FromSeconds(10), token);
                }
                catch (TaskCanceledException) { }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
        }
        private void SetupPrintQueueFolder()
        {
            string exePath = Application.ExecutablePath;
            FileInfo exeFile = new FileInfo(exePath);
            string directory = exeFile.Directory.FullName + @"\PrintQueue";

            System.IO.Directory.CreateDirectory(directory);

            printQueuePath = directory;

            fileSystemWatcher1.Path = directory;
        }

        private async void Form1_Load(object sender, EventArgs e)
        {
            SetupPrintQueueFolder();

            _cts = new CancellationTokenSource();
            StartEmailChecker(_cts.Token);

            Panel rightPanel = new Panel();
            rightPanel.Dock = DockStyle.Right;
            rightPanel.Width = (int)(this.Width / 1.25); // half width

            Panel leftPanel = new Panel();
            leftPanel.Dock = DockStyle.Left;
            leftPanel.Width = this.Width - rightPanel.Width; // remaining width

            this.Controls.Add(leftPanel);
            this.Controls.Add(rightPanel);

            // WebView2 inside panel
            webView21.Dock = DockStyle.Fill;
            rightPanel.Controls.Add(webView21);

            //listView1.Dock = DockStyle.Top;
            listView1.Height = (int)(this.Height / 2); // half height
            leftPanel.Controls.Add(listView1);

            //listBox2.Dock = DockStyle.Bottom;
            listBox2.Height = (int)(this.Height / 2); // half height
            listBox2.Top = listView1.Bottom; // position below listView1
            leftPanel.Controls.Add(listBox2);

            listView1.View = View.Details;
            listView1.FullRowSelect = true;
            listView1.GridLines = true;

            // Columns
            listView1.Columns.Add("Time", 80);
            listView1.Columns.Add("Name", 120);
            listView1.Columns.Add("Email", 200);


            listBox2SelectedIndex = listBox2.SelectedIndex;


            var sortedDirectories = new DirectoryInfo(printQueuePath).GetDirectories()
                                              .OrderBy(d => d.CreationTime)
                                              .ToArray();

            foreach (var item in sortedDirectories)
            {
                LoadFolderInformation(item);
            }

            await webView21.EnsureCoreWebView2Async();

        }

        private void LoadFolderInformation(DirectoryInfo item)
        {
            string metaData = File.ReadAllText(item + @"\metadata.json");

            JsonSerializerOptions options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var printMetaData = JsonSerializer.Deserialize<PrintMetadata>(metaData, options);


            try
            {
                listBox2.SelectedIndex = listBox2SelectedIndex;
            }
            catch (Exception)
            {

            }




            var itemz = new ListViewItem(printMetaData.Date.ToString("hh:mm tt"));
            itemz.SubItems.Add(printMetaData.FromName);
            itemz.SubItems.Add(printMetaData.FromEmail);

            // 🔥 THIS IS KEY
            itemz.Tag = printMetaData;

            listView1.Items.Add(itemz);





        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0)
                return;

            var selectedItem = listView1.SelectedItems[0];

            var printMetaData = (PrintMetadata)selectedItem.Tag;

            listBox2.Items.Clear();

            string folderPath = printQueuePath + @"\" + printMetaData.Id.ToString();

            if (!Directory.Exists(folderPath))
                return;

            foreach (var file in Directory.GetFiles(folderPath))
            {
                if (file.EndsWith("metadata.json", StringComparison.OrdinalIgnoreCase))
                    continue;

                FileInfo fileInfo = new FileInfo(file);

                // Remove GUID prefix
                string nameOnly = fileInfo.Name.Substring(fileInfo.Name.IndexOf("_") + 1);

                listBox2.Items.Add(new 
                {
                    Name = nameOnly.Replace(".txt", ""),
                    FullName = fileInfo.FullName
                });
            }

            listBox2.DisplayMember = "Name";
            webView21.NavigateToString("<html></html>");

            Console.WriteLine(printMetaData.Id);
        }
        

        private async void listBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBox2.SelectedItem == null)
            {
                return;
            }

            await webView21.EnsureCoreWebView2Async();

            var file = listBox2.SelectedItem.GetType().GetProperty("FullName")?.GetValue(listBox2.SelectedItem)?.ToString();

            if (file.EndsWith("AmazonLink.txt"))
            {
                webView21.Source = new Uri(File.ReadAllText(file));
            }
            else if (file.Contains("Link-") && file.EndsWith(".txt"))
            {
                webView21.Source = new Uri(File.ReadAllText(file));
            }
            else
            {
                webView21.Source = new Uri(file);
            }

            //webView21.Source = new Uri("https://www.amazon.com/spr/returns/label/64cd1d03-e7a7-40c9-9a25-fe937a022e23?linkGeneratedOnTimeStamp=1774473088427&ref_=pe_139016810_1160122130_TC_01_01_BT_01_spr_share_label_post&printerFriendly=1&src=old&encoded=1&token=6e2a927d-fcd3-42f5-8115-bd36b6404293");
        }

        private void listView1_KeyDown(object sender, KeyEventArgs e)
        {

        }
    }
}
