using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Enum;
using IFilterTextReader;

namespace Services
{
    public static class Utility
    {
        public static string GetDescription(this Enum en)
        {
            Type type = en.GetType();

            MemberInfo[] memInfo = type.GetMember(en.ToString());

            if (memInfo != null && memInfo.Length > 0)
            {
                object[] attrs = memInfo[0].GetCustomAttributes(typeof(DescriptionAttribute), false);

                if (attrs != null && attrs.Length > 0)
                {
                    return ((DescriptionAttribute)attrs[0]).Description;
                }
            }

            return en.ToString();
        }

        public static TType ConvertToEnum<TType>(this string value)
        {
            try
            {
                if (value.Length == 2)
                    return (TType)Enum.Parse(typeof(TType), "Other");
                else
                    return (TType)Enum.Parse(typeof(TType), value.Trim('.').ToLower());
            }
            catch (Exception)
            {
                return (TType)Enum.Parse(typeof(TType), "Other");
            }
        }

        public static void CopyDirectory(string sourcedir, string desdir)
        {
            DirectoryInfo dir = new DirectoryInfo(sourcedir);
            if (!Directory.Exists(desdir))
            {
                Directory.CreateDirectory(desdir);
            }
            FileInfo[] files = dir.GetFiles();
            foreach (var item in files)
            {
                string temppath = Path.Combine(desdir, item.Name);
                item.CopyTo(temppath, true);
            }
        }
        static StreamWriter sw = new StreamWriter(@"D:\logsmp3.txt", true);
        static object locki;
        public static DataForIndex CreateSoundIndexDoucment(string fn)
        {
            DataForIndex dfi = new DataForIndex();

            try
            {
                var file = TagLib.File.Create(fn);
                dfi.ID = new Random().Next(int.MaxValue);
                dfi.Label = "Music";
                dfi.FileExtension = Path.GetExtension(fn);
                dfi.AudioDuration = file.Properties.Duration.ToString();
                dfi.AudioBitrate = file.Properties.AudioBitrate.ToString();
                foreach (var item in file.Tag.Genres)
                {
                    dfi.AudioGenre += item + " , ";
                }

                dfi.AudioAlbum = file.Tag.Album;
                dfi.FileName = fn;
                dfi.Body = fn + " , " + dfi.AudioGenre + " , " + dfi.AudioDuration + " , " + dfi.AudioAlbum;

            }
            catch (Exception ex)
            {
                FileInfo fi = new FileInfo(fn);
                fi.GetType();
            }
            return dfi;
        }

        public static DataForIndex CreateVideoIndexDoucment(string fn)
        {
            DataForIndex dfi = new DataForIndex();

            try
            {
                var file =new FileInfo(fn);
                dfi.ID = new Random().Next(int.MaxValue);
                dfi.Label = "Music";
                dfi.FileExtension = Path.GetExtension(fn);
                dfi.FileName = fn;
                dfi.Body = fn + " , " + dfi.FileExtension + " , " + dfi.Label;

            }
            catch
            {
                
            }
            return dfi;
        }

        public static DataForIndex CreateDocumentIndex(FilterReader reader, string fileName)
        {
            var dfi = new DataForIndex();
            dfi.ID = new Random().Next(int.MaxValue);
            dfi.Label = "Docs";
            dfi.FileExtension = Path.GetExtension(fileName);
            dfi.FileName = fileName;
            dfi.Body = reader.ReadToEnd();
            return dfi;
        }

        public static void WritePdfContentToTxtFile(string pdfdirectory, string fileName, string pathPdf)
        {
            var cmdexe = new Process { StartInfo = { FileName = "cmd.exe" } };
            var command = string.Format(pdfdirectory + "\\pdftotext.exe -enc UTF-8 \"{0}\" {1}", fileName, pathPdf);
            cmdexe.StartInfo.Arguments = @"/c " + command;
            cmdexe.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            cmdexe.Start();
            cmdexe.WaitForExit();
        }

        public static DataForIndex CreatePdfIndexedDocument(string fileName, string pathPdf)
        {
            var dfi = new DataForIndex();
            dfi.ID = new Random().Next(int.MaxValue);
            dfi.Label = "Docs";
            dfi.FileName = fileName;
            dfi.FileExtension = Path.GetExtension(fileName);
            StreamReader sr = new StreamReader(pathPdf);
            dfi.Body = sr.ReadToEnd();
            sr.Close();
            sr.Dispose();
            File.Delete(pathPdf);
            return dfi;
        }
        public static string RemoveHtmlTags(string text)
        {
            return string.IsNullOrEmpty(text) ? string.Empty : Regex.Replace(text, @"<(.|\n)*?>", " ");
        }
        public static string RemoveAarab(string text)
        {
            var normalizedString = text.Normalize(NormalizationForm.FormD);
            var stringBuilder = new StringBuilder();

            foreach (var c in normalizedString)
            {
                var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
        }

        public static List<string> GetKeyWords(string Keys)
        {
            if (!string.IsNullOrEmpty(Keys))
                return Keys.Split('-', ' ', '\n').ToList();
            else
                return null;
        }
       

    }
}
