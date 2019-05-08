using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace Services
{
    public class LuceneBussines
    {
        static readonly Lucene.Net.Util.Version _version = Lucene.Net.Util.Version.LUCENE_29;
        static Semaphore sem = new Semaphore(1, 1);

        public static HashSet<string> GetStopWords()
        {

            var result = new HashSet<string>();
            StreamReader sr = new StreamReader("stopwords.txt");
            string[] st = new string[1000];
            int j = 0;
            foreach (var item in sr.ReadToEnd().Split('\n'))
            {
                st.SetValue(item.Replace("\r", ""), j);
                j++;
            }

            foreach (var item in st)
                result.Add(item); ;

            return result;
        }

        public static void DeleteXDirectory(int i)
        {
            try
            {
                FSDirectory directory;
                switch (i)
                {
                    case 0:
                        {
                            directory = FSDirectory.Open(new DirectoryInfo(Environment.CurrentDirectory + "\\LuceneIndex"));
                            break;
                        }
                    case 1:
                        {
                            directory = FSDirectory.Open(new DirectoryInfo(Environment.CurrentDirectory + "\\LuceneIndexSite"));
                            break;
                        }
                    default:
                        {
                            directory = FSDirectory.Open(new DirectoryInfo(Environment.CurrentDirectory + "\\StatisticsIndex"));
                            break;
                        }
                }
                
                StandardAnalyzer analyzer = new StandardAnalyzer(_version);
                var writer = new IndexWriter(directory, analyzer, false, IndexWriter.MaxFieldLength.UNLIMITED);
                writer.DeleteAll();
                writer.Dispose();

            }
            catch
            {
            }

        }

        static StandardAnalyzer _analyzer;

        public StandardAnalyzer Analyzer
        {
            get
            {
                if (_analyzer == null)
                {
                    var StopWordLocation = "stopwords.txt";
                    FileInfo stopwords = new FileInfo(StopWordLocation);
                    _analyzer = new StandardAnalyzer(_version, stopwords);
                }
                return _analyzer;
            }
        }
        static FSDirectory _directory;
        public FSDirectory Directory
        {
            get
            {
                if (_directory == null)
                    _directory = FSDirectory.Open(new DirectoryInfo(LuceneDirectory));
                if (IndexWriter.IsLocked(_directory))
                    IndexWriter.Unlock(_directory);

                try
                {
                    var lockFilePath = Path.Combine(LuceneDirectory, "write.lock");
                    if (File.Exists(lockFilePath))
                        File.Delete(lockFilePath);
                }
                catch { }
                return _directory;
            }
        }

        public static string _luceneDIrectory { get; set; }
        public static string LuceneDirectory
        {
            get
            {
                if (string.IsNullOrEmpty(_luceneDIrectory))
                {
                    _luceneDIrectory = Environment.CurrentDirectory + "\\LuceneIndex";
                    return
                        _luceneDIrectory;
                }
                return
                    _luceneDIrectory;
            }


        }

        public static int CountIndex = 0;

        /// <summary>
        /// add document to lucene
        /// </summary>
        /// <param name="dfi"></param>
        public void CreateIndex(DataForIndex dfi)
        {
            sem.WaitOne();
            using (
                var writer = new IndexWriter(Directory, Analyzer, create: false,
                    mfl: IndexWriter.MaxFieldLength.UNLIMITED))
            {
                lock (writer)
                {

                    Document doc = new Document();
                    doc.Add(new Field("ID", dfi.ID.ToString(), Field.Store.YES, Field.Index.NO, Field.TermVector.NO));
                    doc.Add(new Field("FileName", dfi.FileName ?? "-", Field.Store.YES, Field.Index.ANALYZED,
                        Field.TermVector.NO));
                    doc.Add(new Field("Body", dfi.Body ?? "-", Field.Store.YES, Field.Index.ANALYZED,
                        Field.TermVector.WITH_POSITIONS_OFFSETS));
                    doc.Add(new Field("FileExtension", dfi.FileExtension ?? "-", Field.Store.YES, Field.Index.ANALYZED,
                        Field.TermVector.YES));
                    doc.Add(new Field("AudioGenre", dfi.AudioGenre ?? "-", Field.Store.YES, Field.Index.ANALYZED,
                        Field.TermVector.YES));
                    doc.Add(new Field("Label", dfi.Label ?? "-", Field.Store.YES, Field.Index.ANALYZED,
                        Field.TermVector.YES));
                    doc.Add(new Field("AudioAlbum", dfi.AudioAlbum ?? "-", Field.Store.YES, Field.Index.ANALYZED,
                        Field.TermVector.YES));
                    doc.Add(new Field("AudioBitrate", dfi.AudioBitrate ?? "-", Field.Store.YES, Field.Index.ANALYZED,
                        Field.TermVector.YES));
                    doc.Add(new Field("AudioDuration", dfi.AudioDuration ?? "-", Field.Store.YES, Field.Index.ANALYZED,
                        Field.TermVector.YES));
                    //sem.WaitOne();
                    writer.AddDocument(doc);
                    CountIndex++;
                    //sem.Release();
                    //writer.Optimize();
                    writer.Commit();
                    writer.Dispose();
                }
            }
            sem.Release();


        }

        public int ReturnCountIndex()
        {
            return CountIndex;
        }

        public static void CreateIndexForStatistics(DataForIndex dfi)
        {
            FileInfo file = new FileInfo("stopwords.txt");
            var directory = FSDirectory.Open(new DirectoryInfo(Environment.CurrentDirectory + "\\StatisticsIndex"));
            var analyzer = new StandardAnalyzer(_version, file);

            using (var writer = new IndexWriter(directory, analyzer, create: false, mfl: IndexWriter.MaxFieldLength.UNLIMITED))
            {
                var section = string.Empty;
                foreach (var line in File.ReadAllLines(dfi.FileName))
                {
                    Document postDocument = new Document();
                    postDocument.Add(new Field("Id", new Random().Next().ToString(), Field.Store.YES, Field.Index.NOT_ANALYZED));
                    postDocument.Add(new Field("Body", line, Field.Store.YES, Field.Index.ANALYZED, Field.TermVector.WITH_POSITIONS_OFFSETS));
                    writer.AddDocument(postDocument);
                    section = string.Empty;

                }
                writer.Optimize();
                writer.Commit();
                writer.Dispose();

            }
        }

        public static bool isFarsiArabic = false;

        public static string HarfArabic = "";

        public static string HarfFarsi = "";

        public static List<DataForIndex> SearchIndex(string term)
        {
            FileInfo stopword = new FileInfo("stopwords.txt");
            var directory = FSDirectory.Open(new DirectoryInfo(Environment.CurrentDirectory + "\\LuceneIndex"));
            StandardAnalyzer analyzer = new StandardAnalyzer(_version, stopword);
            List<DataForIndex> dfi = new List<DataForIndex>();

            using (var searcher = new IndexSearcher(directory, readOnly: true))
            {
                var parser = new MultiFieldQueryParser(_version, new[] { "Body" }, analyzer);
                if (term.Contains(" ") || term.Contains("\t"))
                {
                    parser.DefaultOperator = QueryParser.Operator.AND;
                    GoforResult(parser, term, searcher, dfi);
                    var termlist = term.Split(' ', '\t');
                    var len = termlist.Length;
                    while (len > 1)
                    {
                        try
                        {
                            string term2 = string.Empty;
                            for (int i = 0; i < len - 1; i++)
                            {
                                term2 += termlist[i] + " ";
                            }
                            if (term2 != string.Empty)
                            {
                                GoforResult(parser, term2, searcher, dfi);
                            }
                            string term3 = string.Empty;
                            for (int i = len - 1; i > 0; i--)
                            {
                                term3 = termlist[i] + " " + term3;
                            }
                            if (term3 != string.Empty)
                            {
                                GoforResult(parser, term3, searcher, dfi);
                            }
                        }
                        catch
                        {
                        }

                        len--;
                    }
                    parser.DefaultOperator = QueryParser.Operator.OR;
                    foreach (var word in term.Split(' ', '\t'))
                    {
                        GoforResult(parser, term, searcher, dfi);
                    }

                }
                else
                {
                    GoforResult(parser, term, searcher, dfi);
                }
                isFarsiArabic = false;
                searcher.Dispose();
                directory.Dispose();

            }
            //dfi = dfi.OrderByDescending(z => z.Score).ToList();
            return dfi;

        }

        private static void GoforResult(MultiFieldQueryParser parser, string term, IndexSearcher searcher, List<DataForIndex> dfi)
        {
            var query = parseQuery(term, parser);
            var hits = searcher.Search(query, 10000).ScoreDocs;
            dfi = getResult(dfi, hits, term, searcher, query, parser);
            if (term.Contains("ي") && !term.Contains("\u0643"))
            {
                isFarsiArabic = true;
                HarfArabic = "ي";
                HarfFarsi = "ی";
                term = term.Replace(HarfArabic, HarfFarsi);
                query = parseQuery(term, parser);
                hits = searcher.Search(query, 10000).ScoreDocs;
                dfi = getResult(dfi, hits, term, searcher, query, parser);
            }
            isFarsiArabic = false;
            if (!term.Contains("ي") && term.Contains("\u0643"))
            {

                isFarsiArabic = true;
                HarfArabic = "\u0643";
                HarfFarsi = "ک";
                term = term.Replace(HarfArabic, HarfFarsi);
                query = parseQuery(term, parser);
                hits = searcher.Search(query, 10000).ScoreDocs;
                dfi = getResult(dfi, hits, term, searcher, query, parser);
            }
            isFarsiArabic = false;
            if (term.Contains("ي") && term.Contains("\u0643"))
            {
                isFarsiArabic = true;
                term = term.Replace("\u0643", "ک").Replace("ي", "ی");
                HarfArabic = "\u0643#ي";
                HarfFarsi = "ک#ی";
                query = parseQuery(term, parser);
                hits = searcher.Search(query, 10000).ScoreDocs;
                dfi = getResult(dfi, hits, term, searcher, query, parser);
            }
        }

        private static Query parseQuery(string searchQuery, QueryParser parser)
        {
            Query query;
            try
            {
                query = parser.Parse(searchQuery.Trim());
            }
            catch (ParseException)
            {
                query = parser.Parse(QueryParser.Escape(searchQuery.Trim()));
            }
            return query;
        }

        private static string searchByPartialWords(string bodyTerm)
        {
            bodyTerm = bodyTerm.Replace("*", "").Replace("?", "");
            var terms = bodyTerm.Trim().Replace("-", " ").Split(' ')
                                     .Where(x => !string.IsNullOrEmpty(x))
                                     .Select(x => x.Trim() + "*");
            bodyTerm = string.Join(" ", terms);
            return bodyTerm;
        }

        private static List<DataForIndex> getResult(List<DataForIndex> dfi, ScoreDoc[] hits, string term, IndexSearcher searcher, Query query, MultiFieldQueryParser parser)
        {

            if (hits.Length == 0)
            {
                term = searchByPartialWords(term);
                query = parseQuery(term, parser);
                hits = searcher.Search(query, 100).ScoreDocs;
            }
            foreach (var scoreDoc in hits)
            {
                var doc = searcher.Doc(scoreDoc.Doc);
                var score = scoreDoc.Score;
                DataForIndex listdata = new DataForIndex();
                listdata.ID = int.Parse(doc.Get("ID"));
                listdata.FileName = doc.Get("FileName");
                listdata.SearchWord = term;
                listdata.FileExtension = doc.Get("FileExtension");
                listdata.AudioGenre = doc.Get("AudioGenre");
                listdata.AudioAlbum = doc.Get("AudioAlbum");
                listdata.AudioBitrate = doc.Get("AudioBitrate");
                listdata.AudioDuration = doc.Get("AudioDuration");
                listdata.Label = doc.Get("Label");
                if (isFarsiArabic)
                {
                    listdata.IsFarsiArabic = true;
                    listdata.HarfArabic = HarfArabic;
                    listdata.HarfFarsi = HarfFarsi;
                }
                listdata.Score = score;
                TermPositionVector obj_vector =
                (TermPositionVector)searcher.IndexReader.GetTermFreqVector(scoreDoc.Doc, "Body");
                int int_phraseIndex = obj_vector.IndexOf(term.Split(' ').FirstOrDefault());
                TermVectorOffsetInfo[] obj_offsetInfo = obj_vector.GetOffsets(int_phraseIndex);
                StringBuilder text = new StringBuilder();
                for (int i = 0; i < obj_offsetInfo.Length; i++)
                {

                    string body = doc.Get("Body");
                    int start = obj_offsetInfo[i].StartOffset;
                    int end = body.Length;
                    int count = 100;
                    if (start + count <= end)
                    {
                        end = start + count;
                    }

                    if (start > count)
                    {
                        start = start - count;
                    }
                    else
                    {
                        start = 0;
                    }
                    text.Append(body.Substring(start, end - start) + " # ");
                }
                listdata.ResultText = text.ToString();
                if (dfi.FirstOrDefault(x => x.FileName == listdata.FileName) == null)
                {
                    dfi.Add(listdata);
                }
                else
                {
                    var del = dfi.FirstOrDefault(x => x.FileName == listdata.FileName);
                    dfi.Remove(del);
                    del.SearchWord = del.SearchWord + " + " + listdata.SearchWord;
                    del.ResultText = del.ResultText + " " + listdata.ResultText;
                    dfi.Add(del);
                }
            }
            return dfi;
        }

        #region const
        public const char YEH = '\u064A';

        public const char FARSI_YEH = '\u06CC';

        public const char YEH_BARREE = '\u06D2';

        public const char KEHEH = '\u06A9';

        public const char KAF = '\u0643';

        public const char HAMZA_ABOVE = '\u0654';

        public const char HEH_YEH = '\u06C0';

        public const char HEH_GOAL = '\u06C1';

        public const char HEH = '\u0647';
        #endregion

        public void CreateIndexSite(DataForIndex dfi)
        {
            sem.WaitOne();
            var directory = FSDirectory.Open(new DirectoryInfo(Environment.CurrentDirectory + "\\LuceneIndexSite"));
            using (
                var writer = new IndexWriter(directory, Analyzer, create: false,
                    mfl: IndexWriter.MaxFieldLength.UNLIMITED))
            {
                lock (writer)
                {

                    Document doc = new Document();
                    doc.Add(new Field("ID", dfi.ID.ToString(), Field.Store.YES, Field.Index.NO, Field.TermVector.NO));
                    doc.Add(new Field("FileName", dfi.FileName ?? "-", Field.Store.YES, Field.Index.ANALYZED,
                        Field.TermVector.NO));
                    doc.Add(new Field("Body", dfi.Body ?? "-", Field.Store.YES, Field.Index.ANALYZED,
                        Field.TermVector.WITH_POSITIONS_OFFSETS));
                    doc.Add(new Field("FileExtension", dfi.FileExtension ?? "-", Field.Store.YES, Field.Index.ANALYZED,
                        Field.TermVector.YES));
                    doc.Add(new Field("AudioGenre", dfi.AudioGenre ?? "-", Field.Store.YES, Field.Index.ANALYZED,
                        Field.TermVector.YES));
                    doc.Add(new Field("Label", dfi.Label ?? "-", Field.Store.YES, Field.Index.ANALYZED,
                        Field.TermVector.YES));
                    doc.Add(new Field("AudioAlbum", dfi.AudioAlbum ?? "-", Field.Store.YES, Field.Index.ANALYZED,
                        Field.TermVector.YES));
                    doc.Add(new Field("AudioBitrate", dfi.AudioBitrate ?? "-", Field.Store.YES, Field.Index.ANALYZED,
                        Field.TermVector.YES));
                    doc.Add(new Field("AudioDuration", dfi.AudioDuration ?? "-", Field.Store.YES, Field.Index.ANALYZED,
                        Field.TermVector.YES));
                    //sem.WaitOne();
                    writer.AddDocument(doc);
                    CountIndex++;
                    //sem.Release();
                    //writer.Optimize();
                    writer.Commit();
                    writer.Dispose();
                }
            }
            sem.Release();
        }
        public static IList<TagForStatistic> Create(int threshold = 50)
        {
            var path = Environment.CurrentDirectory + "\\StatisticsIndex";

            var results = new List<TagForStatistic>();
            var field = "Body";

            IndexReader indexReader = IndexReader.Open(FSDirectory.Open(path), true);
            try
            {

                var termFrequency = indexReader.Terms();
                while (termFrequency.Next())
                {
                    if (termFrequency.DocFreq() >= threshold && termFrequency.Term.Field == field)
                    {
                        results.Add(new TagForStatistic { Text = termFrequency.Term.Text, Frequency = termFrequency.DocFreq() });
                    }
                }
            }
            catch (Exception ex)
            {

                //System.Windows.Forms.MessageBox.Show(ex.Message);
            }

            return results.OrderByDescending(x => x.Frequency).ToList();
        }
        public static List<DataForIndex> SearchIndexStite(string term)
        {
            FileInfo stopword = new FileInfo("stopwords.txt");
            var directory = FSDirectory.Open(new DirectoryInfo(Environment.CurrentDirectory + "\\LuceneIndexSite"));
            StandardAnalyzer analyzer = new StandardAnalyzer(_version, stopword);
            List<DataForIndex> dfi = new List<DataForIndex>();

            using (var searcher = new IndexSearcher(directory, readOnly: true))
            {
                var parser = new MultiFieldQueryParser(_version, new[] { "Body" }, analyzer);
                if (term.Contains(" ") || term.Contains("\t"))
                {
                    parser.DefaultOperator = QueryParser.Operator.AND;
                    GoforResult(parser, term, searcher, dfi);
                    var termlist = term.Split(' ', '\t');
                    var len = termlist.Length;
                    while (len > 1)
                    {
                        try
                        {
                            string term2 = string.Empty;
                            for (int i = 0; i < len - 1; i++)
                            {
                                term2 += termlist[i] + " ";
                            }
                            if (term2 != string.Empty)
                            {
                                GoforResult(parser, term2, searcher, dfi);
                            }
                            string term3 = string.Empty;
                            for (int i = len - 1; i > 0; i--)
                            {
                                term3 = termlist[i] + " " + term3;
                            }
                            if (term3 != string.Empty)
                            {
                                GoforResult(parser, term3, searcher, dfi);
                            }
                        }
                        catch
                        {
                        }

                        len--;
                    }
                    parser.DefaultOperator = QueryParser.Operator.OR;
                    foreach (var word in term.Split(' ', '\t'))
                    {
                        GoforResult(parser, term, searcher, dfi);
                    }

                }
                else
                {
                    GoforResult(parser, term, searcher, dfi);
                }
                isFarsiArabic = false;
                searcher.Dispose();
                directory.Dispose();

            }
            return dfi;

        }
    }
}
