using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Lucene.Net.Index;
using Lucene.Net.Store;

namespace Services
{
    //[DebuggerDisplay("{Frequency}, {Text}")]
    //public class TagForStatistics
    //{
    //    public string Text { set; get; }

    //    /// <summary>
    //    /// The frequency of a term is defined as the number of 
    //    /// documents in which a specific term appears.
    //    /// </summary>
    //    public int Frequency { set; get; }
    //}
    //public static class WordCount
    //{
    //    public static IList<TagForStatistics> Create(int threshold =50)
    //    {
    //        var path = Environment.CurrentDirectory + "\\StatisticsIndex";

    //        var results = new List<TagForStatistics>();
    //        var field = "Body";

    //        IndexReader indexReader = IndexReader.Open(FSDirectory.Open(path), true);
    //        try
    //        {
                
    //            var termFrequency = indexReader.Terms();
    //            while (termFrequency.Next())
    //            {
    //                if (termFrequency.DocFreq() >= threshold && termFrequency.Term.Field == field)
    //                {
    //                    results.Add(new TagForStatistics { Text = termFrequency.Term.Text, Frequency = termFrequency.DocFreq() });
    //                }
    //            }
    //        }
    //        catch (Exception ex)
    //        {

    //            //System.Windows.Forms.MessageBox.Show(ex.Message);
    //        }
            
    //        return results.OrderByDescending(x => x.Frequency).ToList();
    //    }
    //}
}
