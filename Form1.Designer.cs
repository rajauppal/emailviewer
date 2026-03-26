namespace WinFormsApp3
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            fileSystemWatcher1 = new FileSystemWatcher();
            listBox2 = new ListBox();
            folderBrowserDialog1 = new FolderBrowserDialog();
            webView21 = new Microsoft.Web.WebView2.WinForms.WebView2();
            listView1 = new ListView();
            ((System.ComponentModel.ISupportInitialize)fileSystemWatcher1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)webView21).BeginInit();
            SuspendLayout();
            // 
            // fileSystemWatcher1
            // 
            fileSystemWatcher1.EnableRaisingEvents = true;
            fileSystemWatcher1.IncludeSubdirectories = true;
            fileSystemWatcher1.NotifyFilter = NotifyFilters.DirectoryName;
            fileSystemWatcher1.Path = "C:\\";
            fileSystemWatcher1.SynchronizingObject = this;
            fileSystemWatcher1.Created += fileSystemWatcher1_Created;
            // 
            // listBox2
            // 
            listBox2.FormattingEnabled = true;
            listBox2.ItemHeight = 15;
            listBox2.Location = new Point(12, 220);
            listBox2.Name = "listBox2";
            listBox2.Size = new Size(367, 184);
            listBox2.TabIndex = 0;
            listBox2.SelectedIndexChanged += listBox2_SelectedIndexChanged;
            // 
            // webView21
            // 
            webView21.AllowExternalDrop = true;
            webView21.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            webView21.CreationProperties = null;
            webView21.DefaultBackgroundColor = Color.White;
            webView21.Location = new Point(485, 30);
            webView21.Name = "webView21";
            webView21.Size = new Size(303, 330);
            webView21.TabIndex = 1;
            webView21.ZoomFactor = 1D;
            // 
            // listView1
            // 
            listView1.Location = new Point(12, 30);
            listView1.Name = "listView1";
            listView1.Size = new Size(367, 173);
            listView1.TabIndex = 2;
            listView1.UseCompatibleStateImageBehavior = false;
            listView1.SelectedIndexChanged += listView1_SelectedIndexChanged;
            listView1.KeyDown += listView1_KeyDown;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(listView1);
            Controls.Add(webView21);
            Controls.Add(listBox2);
            Name = "Form1";
            Text = "Email Viewer";
            WindowState = FormWindowState.Maximized;
            Load += Form1_Load;
            ((System.ComponentModel.ISupportInitialize)fileSystemWatcher1).EndInit();
            ((System.ComponentModel.ISupportInitialize)webView21).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private FileSystemWatcher fileSystemWatcher1;
        private ListBox listBox2;
        private Microsoft.Web.WebView2.WinForms.WebView2 webView21;
        private FolderBrowserDialog folderBrowserDialog1;
        private ListView listView1;
    }
}
