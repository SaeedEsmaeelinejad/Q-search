using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Common;
using Common.Enum;
using DevComponents.DotNetBar;
using DevComponents.DotNetBar.Controls;
using IFilterTextReader;
using Services;

namespace SearchMe
{
    public partial class Search : DevComponents.DotNetBar.Metro.MetroForm
    {
        private string filePath = "";
        private int IndexDocCount = 0;
        private int IndexMp3Count = 0;
        private int IndexMovieCount = 0;
        List<DocumentType> DocumentFormat = new List<DocumentType>();
        List<SoundType> SoundsFormat = new List<SoundType>();
        List<VideoType> VideosFormat = new List<VideoType>();

        public Search()
        {
            InitializeComponent();
        }

        private void Search_Load(object sender, EventArgs e)
        {
            LoadDocumentCheckListItem();
            LoadSoundCheckedListItem();
            LoadVideoCheckedListItem();
        }

        private void LoadSoundCheckedListItem()
        {
            var list =
                Enum.GetValues(typeof(SoundType))
                    .Cast<SoundType>()
                    .Select(v => new SelectListItem() { Value = ((int)v), Text = v.GetDescription() })
                    .ToList();
            list.Insert(0, new SelectListItem() { Text = "Select all", Value = 100 });
            list.RemoveAt(list.Count - 1);
            SoundCheckedList.DataSource = list;
            SoundCheckedList.ValueMember = "value";
            SoundCheckedList.DisplayMember = "text";
        }

        private void LoadVideoCheckedListItem()
        {
            var list =
                Enum.GetValues(typeof(VideoType))
                    .Cast<VideoType>()
                    .Select(v => new SelectListItem() { Value = ((int)v), Text = v.GetDescription() })
                    .ToList();
            list.Insert(0, new SelectListItem() { Text = "Select all", Value = 100 });
            list.RemoveAt(list.Count - 1);
            VideoCheckedList.DataSource = list;
            VideoCheckedList.ValueMember = "value";
            VideoCheckedList.DisplayMember = "text";
        }

        private void LoadDocumentCheckListItem()
        {
            var list =
                Enum.GetValues(typeof(DocumentType))
                    .Cast<DocumentType>()
                    .Select(v => new SelectListItem() { Value = ((int)v), Text = v.GetDescription() })
                    .ToList();
            list.Insert(0, new SelectListItem() { Text = "Select all", Value = 100 });
            list.RemoveAt(list.Count - 1);
            DocumentCheckedList.DataSource = list;
            DocumentCheckedList.ValueMember = "value";
            DocumentCheckedList.DisplayMember = "text";
        }

        private void browsBtn_Click(object sender, EventArgs e)
        {
            if (FolderBrowseDialog.ShowDialog() == DialogResult.OK)
            {
                filePathTxt.Text = FolderBrowseDialog.SelectedPath;
            }
        }

        private void CheckedLIstBoxCheckedEvent(object sender, ItemCheckEventArgs e)
        {
            var checkedListBox = (CheckedListBox)sender;
            if (e.NewValue == CheckState.Checked && e.Index == 0)
                for (int i = 1; i < checkedListBox.Items.Count; i++)
                    checkedListBox.SetItemChecked(i, true);
            else if (e.NewValue == CheckState.Unchecked && e.Index == 0)
                for (int i = 1; i < checkedListBox.Items.Count; i++)
                    checkedListBox.SetItemChecked(i, false);
        }

        private void indexStartBtn_Click(object sender, EventArgs e)
        {
            IndexDocCount = 0;
            if (filePath == "")
                MessageBox.Show("لطفا یک مسیر را انتخاب کنید");
            else
            {
                //label10.Visible = true;
                //label11.Visible = true;
                LuceneBussines.DeleteXDirectory(0);
                GetSelectedCheckboxList();
                //var t = new Thread(GO);
                //t.Start();

                Utility.CopyDirectory(".\\xpdf", "C:\\SearchMe");

                ProcessDirectory(filePathTxt.Text);

            }
        }

        private void GetSelectedCheckboxList()
        {
            DocumentFormat.Clear();
            foreach (var item in DocumentCheckedList.CheckedItems)
            {
                if (((SelectListItem)item).Value != 100)
                    DocumentFormat.Add(((SelectListItem)item).Text.ConvertToEnum<DocumentType>());
            }
            foreach (var item in SoundCheckedList.CheckedItems)
            {
                if (((SelectListItem)item).Value != 100)
                    SoundsFormat.Add(((SelectListItem)item).Text.ConvertToEnum<SoundType>());
            }
            foreach (var item in VideoCheckedList.CheckedItems)
            {
                if (((SelectListItem)item).Value != 100)
                    VideosFormat.Add(((SelectListItem)item).Text.ConvertToEnum<VideoType>());
            }

        }

        private void filePathTxt_TextChanged(object sender, EventArgs e)
        {
            filePath = filePathTxt.Text;
        }

        private void ProcessDirectory(string paths)
        {

            var timeoutOption = FilterReaderTimeout.NoTimeout;
            foreach (string fileName in Directory.GetFiles(paths))
            {

                if (Path.GetExtension(fileName).Trim('.').ToLower() == DocumentType.pdf.ToString().ToLower() &&
                    DocumentFormat.Any(x => x == DocumentType.pdf))
                {
                    AddPdfToIndexe(fileName);
                }
                else if (
                    DocumentFormat.Any(
                        x => x.ToString().ToLower() == Path.GetExtension(fileName).Trim('.').ToLower()))
                {
                    AddDocumentToIndexed(fileName, timeoutOption);

                }
                else if (
                    SoundsFormat.Any(
                        x => x.ToString().ToLower() == Path.GetExtension(fileName).Trim('.').ToLower()))
                {
                    AddSoundToIndexed(fileName);

                }

                else if (
                    VideosFormat.Any(
                        x => x.ToString().ToLower() == Path.GetExtension(fileName).Trim('.').ToLower()))
                {
                    AddVideoToIndexed(fileName);
                }
            }

            // Recurse into subdirectories of this directory.
            string[] subdirectoryEntries = Directory.GetDirectories(paths);
            foreach (string subdirectory in subdirectoryEntries)
            {
                try
                {
                    ProcessDirectory(subdirectory);

                }
                catch (Exception ex)
                {
                    //ignore directory       
                }
            }
        }

        private void AddVideoToIndexed(string fn)
        {
            Task t = new Task(() =>
            {
                DataForIndex dfi = Utility.CreateVideoIndexDoucment(fn);
                LuceneBussines lucene = new LuceneBussines();
                lucene.CreateIndex(dfi);
                IndexMovieCount++;
                LableTextChange(Videolblcount, IndexMovieCount.ToString());
            });
            t.Start();
        }

        private void AddSoundToIndexed(string fn)
        {
            Task t = new Task(() =>
            {
                DataForIndex dfi = Utility.CreateSoundIndexDoucment(fn);
                LuceneBussines lucene = new LuceneBussines();
                lucene.CreateIndex(dfi);
                IndexMp3Count++;
                LableTextChange(CountFileIndexedLabel, IndexMp3Count.ToString());
            });
            t.Start();
        }

        public delegate void IndexCountlbl(Label lbl, string num);

        public void LableTextChange(Label lbl, string num)
        {
            LuceneBussines lb = new LuceneBussines();
            if (lbl.InvokeRequired)
            {
                IndexCountlbl IC = new IndexCountlbl(LableTextChange);
                lbl.Invoke(IC, new object[] { lbl, num });
            }
            else
            {
                lbl.Text = num;
            }
        }

        static Semaphore sem = new Semaphore(1, 1);

        private void AddDocumentToIndexed(string fileName, FilterReaderTimeout timeoutOption)
        {
            Task t = new Task(() =>
            {
                try
                {
                    var reader = new FilterReader(fileName,
                        string.Empty,
                        disableEmbeddedContent: false,
                        includeProperties: false,
                        readIntoMemory: false,
                        filterReaderTimeout: timeoutOption,
                        timeout: -1);
                    DataForIndex dfi = Utility.CreateDocumentIndex(reader, fileName);
                    LuceneBussines lucene = new LuceneBussines();
                    lucene.CreateIndex(dfi);
                    IndexDocCount++;
                    LableTextChange(DocumentCountLable, IndexDocCount.ToString());
                }
                catch (Exception ex)
                {
                    try
                    {
                        var reader = new FilterReader(fileName,
                            string.Empty,
                            disableEmbeddedContent: false,
                            includeProperties: false,
                            readIntoMemory: true,
                            filterReaderTimeout: FilterReaderTimeout.TimeoutWithException,
                            timeout: 5000);
                        if (reader == null)
                            return;
                        ;
                        DataForIndex dfi = Utility.CreateDocumentIndex(reader, fileName);

                        LuceneBussines lucene = new LuceneBussines();
                        lucene.CreateIndex(dfi);
                        IndexDocCount++;
                        LableTextChange(DocumentCountLable, IndexDocCount.ToString());
                    }
                    catch (Exception)
                    {
                    }
                }
            });
            t.Start();
        }

        private void AddPdfToIndexe(string fileName)
        {
            Task t = new Task(() =>
            {
                var Pdfdirectory = "C:\\SearchMe";
                var pathPdf = Pdfdirectory + "\\" + Guid.NewGuid().ToString() + ".txt";
                if (!Directory.Exists(Pdfdirectory))
                {
                    Directory.CreateDirectory(Pdfdirectory);
                }
                //should change to application startup path 
                if (!File.Exists(pathPdf))
                {

                    File.Create(pathPdf).Dispose();
                }
                Utility.WritePdfContentToTxtFile(Pdfdirectory, fileName, pathPdf);
                DataForIndex dfi = Utility.CreatePdfIndexedDocument(fileName, pathPdf);
                LuceneBussines lucene = new LuceneBussines();
                lucene.CreateIndex(dfi);
                IndexDocCount++;
                LableTextChange(DocumentCountLable, IndexDocCount.ToString());

            });
            t.Start();
        }


        List<DataForIndex> dfiForSearch = new List<DataForIndex>();

        private void SearchButn_Click(object sender, EventArgs e)
        {
            RichtxtResult.Clear();
            if (Searchtxtx.Text != "")
            {
                
                string input = Utility.RemoveAarab(Searchtxtx.Text.Trim().Replace("لا","ال"));
                Stopwatch sw = new Stopwatch();
                sw.Start();
                dfiForSearch = LuceneBussines.SearchIndex(input);
                sw.Stop();
                GridResult.DataSource = dfiForSearch.ToList();
                Resultlbl.Text = dfiForSearch.Count().ToString();
                SearchTimelbl.Text = sw.Elapsed.TotalSeconds.ToString();
                label5.Visible = true;
                label4.Visible = true;
                GridResult.Visible = true;
                GridResult.Columns[0].Visible = false;
                GridResult.Columns[1].Visible = false;
                GridResult.Columns[8].Visible = false;
                GridResult.Columns[9].Visible = false;
                GridResult.Columns[12].Visible = false;
                GridResult.Columns[13].Visible = false;
                GridResult.Columns[14].Visible = false;
                ShowFilebtn.Visible = true;
                RichtxtResult.Visible = true;

            }
            else
                MessageBox.Show("یک مقدار را برای جستجو وارد کنید");
        }

        private void ShowFile_Click(object sender, EventArgs e)
        {
            Process.Start(GridResult.CurrentRow.Cells[3].Value.ToString());
        }

        private void GridResult_MouseClick(object sender, MouseEventArgs e)
        {
            if (GridResult.Rows != null && GridResult.Rows.Count > 0)
            {
                //var searchvalue = Searchtxtx.Text.Trim().Replace("لا","ال");
                var searchvalue = GridResult.CurrentRow.Cells[4].Value.ToString();
                var searchvaluelist = searchvalue.Split(new[] {" + "}, StringSplitOptions.None);
                bool flag = false;
                RichtxtResult.Clear();
                var finddfi =
                    dfiForSearch.FirstOrDefault(
                        x => x.ID == Convert.ToInt32(GridResult.CurrentRow.Cells[0].Value.ToString()));
                foreach (var item in finddfi.ResultText.Split('#'))
                {
                    var hasval = ShowResultinRichtxt(finddfi, item, searchvalue);
                    if (!hasval && searchvaluelist.Any())
                    {
                        for (int j = 0; j < searchvaluelist.Count(); j++)
                        {
                            if (!ShowResultinRichtxt(finddfi, item, searchvaluelist[j]))
                            {
                                try
                                {
                                    int s = j + 1;
                                    hasval = ShowResultinRichtxt(finddfi, item,
                                        searchvaluelist[j] + searchvaluelist[s]);
                                    if (hasval)
                                    {
                                        flag = true;
                                    }
                                }
                                catch
                                {
                                }
                            }
                            else
                            {
                                flag = true;
                            }
                        }

                    }
                    else
                    {
                        flag = true;
                    }
                }
                if (!flag)
                {
                    RichtxtResult.Text = "Sorry Can't Load Text." + Environment.NewLine + GridResult.CurrentRow.Cells[3].Value.ToString();
                }
            }

        }

        private bool ShowResultinRichtxt(DataForIndex finddfi, string item, string searchvalue)
        {
            int index = item.IndexOf(searchvalue, StringComparison.Ordinal);
            if (index!=-1)
            {
                if (finddfi.IsFarsiArabic == true)
                {
                    string term = searchvalue;
                    if (!finddfi.HarfArabic.Contains("#"))
                    {
                        term = term.Replace(finddfi.HarfArabic, finddfi.HarfFarsi);
                        index = item.IndexOf(term);
                    }
                }
                if (index >= 0)
                {
                    RichtxtResult.AppendText(item.Substring(0, index));
                    RichtxtResult.SelectionBackColor = Color.Yellow;
                    Font fonttext = RichtxtResult.SelectionFont;
                    RichtxtResult.SelectionFont = new Font("Tahoma", 12, FontStyle.Bold);
                    RichtxtResult.AppendText(item.Substring(index, searchvalue.Length));
                    RichtxtResult.SelectionBackColor = Color.White;
                    RichtxtResult.SelectionFont = new Font(fonttext, FontStyle.Regular);
                    RichtxtResult.AppendText(
                        item.Substring(index + searchvalue.Length, item.Length - (index + searchvalue.Length)) +
                        Environment.NewLine + "//////////////////" + Environment.NewLine);
                    return true;
                }

            }
            else
            {
                return false;
            }
            return false;
        }

        private void sideNavItem4_Click(object sender, EventArgs e)
        {

        }
        static Dictionary<string, string> content = new Dictionary<string, string>();
        static List<Task> AllTask = new List<Task>();
        static string BaseUrl = "";
        static List<string> keyWords = new List<string>();
        private static int numberOfKeyWord = 0;
        private void crawlBtn_Click(object sender, EventArgs e)
        {
            if (urlTextBox.Text != "")
            {
                if (!string.IsNullOrEmpty(KeywordCounttxt.Text))
                    numberOfKeyWord = int.Parse(KeywordCounttxt.Text);
                keyWords = Utility.GetKeyWords(keyWordTextBox.Text.Trim());
                //LuceneBussines.DeleteXDirectory(1);

                CrawledSiterichtct.Text = "start" + Environment.NewLine;
                if (urlTextBox.Text.Contains("http://"))
                {
                    BaseUrl = urlTextBox.Text;
                }
                else
                    BaseUrl = "http://" + urlTextBox.Text;
                GetAllString(BaseUrl);
                Task.WhenAll(AllTask.ToArray());
            }
            else
                MessageBox.Show("آدرس سایت را وارد کنید");
        }

        int i = 1;
        public delegate void settext(RichTextBoxEx rich, string txt);
        public void settextforrich(RichTextBoxEx rich, string txt)
        {
            if (CrawledSiterichtct.InvokeRequired)
            {
                settext sx = new settext(settextforrich);
                rich.Invoke(sx, new object[] { rich, txt });
            }
            else
            {
                rich.Text += "\r\n" + txt + "\r\n";
                SiteCountlbl.Text = i.ToString();
                i++;
            }
        }

        public static DataForIndex dfi2 = new DataForIndex();
        public static Semaphore sema = new Semaphore(1, 1);
        public static int itemAdded = 0;
        public delegate void indexsites(DataForIndex LuceneForSite, string url, string sitetxt);
        public void indexsite(DataForIndex LuceneForSite, string url, string sitetxt)
        {

            dfi2.ID = new Random().Next(int.MaxValue);
            dfi2.FileName = url;
            dfi2.Body = Utility.RemoveHtmlTags(sitetxt);
            Thread t = new Thread(GoForIndexSite);
            t.Start();
        }
        public static void GoForIndexSite()
        {
            try
            {
                sem.WaitOne();
                new LuceneBussines().CreateIndexSite(dfi2);
                sem.Release();
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
            }

        }

        public static DataForIndex data = new DataForIndex();
        int num = 0;
        public delegate void SiteTotxt(string txt, string url);
        public void SiteTotxti(string txt, string url)
        {
            string dir = Directory.GetCurrentDirectory();
            var sw = new StreamWriter(dir + "\\Sites\\" + urlTextBox.Text + "\\" + num + ".txt");
            string sitetxt = Utility.RemoveHtmlTags(txt);
            sw.Write(sitetxt);
            sw.Close();
            data.ID = new Random().Next(int.MaxValue);
            data.Body = sitetxt;
            data.FileName = dir + "\\Sites\\" + urlTextBox.Text + "\\" + num + ".txt";
            Thread t = new Thread(GoForStatisticIndex);
            t.Start();
            num++;
        }

        public static void GoForStatisticIndex()
        {
            try
            {
                sema.WaitOne();
                LuceneBussines.CreateIndexForStatistics(data);
                sema.Release();
            }
            catch (Exception)
            {
                //MessageBox.Show(ex.Message);
            }
        }

        public void GetAllString(string CurrentUrl)
        {
            ServicePointManager.DefaultConnectionLimit = 300;
            if (content.ContainsKey(CurrentUrl))
                return;
            var html = new HtmlAgilityPack.HtmlDocument();

            try
            {
                var request = (HttpWebRequest)WebRequest.Create(CurrentUrl);
                request.Method = "GET";
                using (var response = (HttpWebResponse)request.GetResponse())
                {
                    using (var stream = response.GetResponseStream())
                    {
                        html.Load(stream, Encoding.UTF8);
                    }
                }
            }
            catch
            {
                return;
            }
            //var rootWeekTable = html.DocumentNode;
            var root = html.DocumentNode;
            try
            {
                if (!content.ContainsKey(CurrentUrl))
                {
                    var str = root.InnerText.Trim().Replace("\r", "").Replace("\n", "");
                    content.Add(CurrentUrl, str);
                    if (CheckForKeyWord(str))
                    {
                        indexsite(new DataForIndex(), CurrentUrl, root.InnerText);
                        if (!Directory.Exists(Directory.GetCurrentDirectory() + "\\Sites\\" + urlTextBox.Text))
                        {
                            Directory.CreateDirectory(Directory.GetCurrentDirectory() + "\\Sites\\" + urlTextBox.Text);
                        }
                        SiteTotxti(str, CurrentUrl);
                        itemAdded++;
                    }
                }
                else
                    return;
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message);
                return;
            }
            //add to richtext
            settextforrich(CrawledSiterichtct, CurrentUrl);
            var AllTag = root.SelectNodes("//a[@href]");
            if (AllTag != null)
            {

                foreach (var item in AllTag)
                {
                    string url = item.GetAttributeValue("href", "");
                    if (url.StartsWith("/"))
                        url = BaseUrl + url;
                    url = url.ToLower();
                    if (url.Contains(BaseUrl.Replace("http://", "")) && !content.ContainsKey(url) && !url.Contains(".jpeg") && !url.Contains(".png") && !url.Contains(".jpg") && !url.Contains(".gif") && !url.Contains(".mp4"))
                    {
                        var teststToGetContent = new Task(delegate { GetAllString(url); });
                        AllTask.Add(teststToGetContent);
                        teststToGetContent.Start();
                        //GetAllString(url);
                    }
                }
            }
        }

        private bool CheckForKeyWord(string str)
        {
            if (numberOfKeyWord < 1)
                return true;
            int numberOfMatch = 0;
            foreach (var item in keyWords)
            {
                if (str.Contains(item))
                    numberOfMatch++;
            }
            if (numberOfMatch >= numberOfKeyWord)
            {
                return true;
            }
            return false;
        }

        private void SearchSitebtn_Click(object sender, EventArgs e)
        {
            RichtxtResultSite.Clear();
            if (SearchSitetxt.Text != "")
            {

                string input = Utility.RemoveAarab(SearchSitetxt.Text.Trim().Replace("لا", "ال"));
                Stopwatch sw = new Stopwatch();
                sw.Start();
                dfiForSearch = LuceneBussines.SearchIndexStite(input);
                sw.Stop();
                GridResultSite.DataSource = dfiForSearch.ToList();
                ResultlblSite.Text = dfiForSearch.Count().ToString();
                SearchTimelblSite.Text = sw.Elapsed.TotalSeconds.ToString();
                //label5.Visible = true;
                //label4.Visible = true;
                //GridResultSite.Visible = true;
                GridResultSite.Columns[0].Visible = false;
                GridResultSite.Columns[3].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                GridResultSite.Columns[4].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                GridResultSite.Columns[1].Visible = false;
                GridResultSite.Columns[2].Visible = false;
                //GridResultSite.Columns[7].Visible = false;
                GridResultSite.Columns[8].Visible = false;
                GridResultSite.Columns[9].Visible = false;
                GridResultSite.Columns[10].Visible = false;
                GridResultSite.Columns[11].Visible = false;
                GridResultSite.Columns[12].Visible = false;
                GridResultSite.Columns[13].Visible = false;
                GridResultSite.Columns[14].Visible = false;
                //ShowFilebtn.Visible = true;
                //RichtxtResult.Visible = true;

            }
            else
                MessageBox.Show("یک مقدار را برای جستجو وارد کنید");
        }

        private void DeleteIndexSite_Click(object sender, EventArgs e)
        {
            LuceneBussines.DeleteXDirectory(1);
            MessageBox.Show("Previous Site Indexes Deleted!");
        }
        private bool ShowResultinRichtxtsites(DataForIndex finddfi, string item, string searchvalue)
        {
            int index = item.IndexOf(searchvalue, StringComparison.Ordinal);
            if (index != -1)
            {
                if (finddfi.IsFarsiArabic == true)
                {
                    string term = searchvalue;
                    if (!finddfi.HarfArabic.Contains("#"))
                    {
                        term = term.Replace(finddfi.HarfArabic, finddfi.HarfFarsi);
                        index = item.IndexOf(term);
                    }
                }
                if (index >= 0)
                {
                    RichtxtResultSite.AppendText(item.Substring(0, index));
                    RichtxtResultSite.SelectionBackColor = Color.Yellow;
                    Font fonttext = RichtxtResult.SelectionFont;
                    RichtxtResultSite.SelectionFont = new Font("Tahoma", 12, FontStyle.Bold);
                    RichtxtResultSite.AppendText(item.Substring(index, searchvalue.Length));
                    RichtxtResultSite.SelectionBackColor = Color.White;
                    RichtxtResultSite.SelectionFont = new Font(fonttext, FontStyle.Regular);
                    RichtxtResultSite.AppendText(
                        item.Substring(index + searchvalue.Length, item.Length - (index + searchvalue.Length)) +
                        Environment.NewLine + "//////////////////" + Environment.NewLine);
                    return true;
                }

            }
            else
            {
                return false;
            }
            return false;
        }
        private void GridResultSite_MouseClick(object sender, MouseEventArgs e)
        {
            if (GridResultSite.Rows != null && GridResultSite.Rows.Count > 0)
            {
                //var searchvalue = Searchtxtx.Text.Trim().Replace("لا","ال");
                var searchvalue = GridResultSite.CurrentRow.Cells[4].Value.ToString();
                var searchvaluelist = searchvalue.Split(new[] { " + " }, StringSplitOptions.None);
                bool flag = false;
                RichtxtResultSite.Clear();
                var finddfi =
                    dfiForSearch.FirstOrDefault(
                        x => x.ID == Convert.ToInt32(GridResultSite.CurrentRow.Cells[0].Value.ToString()));
                foreach (var item in finddfi.ResultText.Split('#'))
                {
                    var hasval = ShowResultinRichtxtsites(finddfi, item, searchvalue);
                    if (!hasval && searchvaluelist.Any())
                    {
                        for (int j = 0; j < searchvaluelist.Count(); j++)
                        {
                            if (!ShowResultinRichtxtsites(finddfi, item, searchvaluelist[j]))
                            {
                                try
                                {
                                    int s = j + 1;
                                    hasval = ShowResultinRichtxtsites(finddfi, item,
                                        searchvaluelist[j] + searchvaluelist[s]);
                                    if (hasval)
                                    {
                                        flag = true;
                                    }
                                }
                                catch
                                {
                                }
                            }
                            else
                            {
                                flag = true;
                            }
                        }

                    }
                    else
                    {
                        flag = true;
                    }
                }
                if (!flag)
                {
                    RichtxtResultSite.Text ="Sorry Can't Load Text."+Environment.NewLine+ GridResultSite.CurrentRow.Cells[3].Value.ToString();
                }
            }

        }

        private void Videolblcount_Click(object sender, EventArgs e)
        {

        }

        private void sideNavItem3_Click(object sender, EventArgs e)
        {

        }
    }
}
