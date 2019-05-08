namespace Common
{
   public class DataForIndex
    {
        public int ID { get; set; }
        public string Label { get; set; }
        public string FileExtension { get; set; }
        public string FileName { get; set; }
        public string SearchWord { get; set; }

        public bool IsFarsiArabic { get; set; }
        public string HarfArabic { get; set; }
        public string HarfFarsi { get; set; }
        public string AudioBitrate { get; set; }
        public string AudioDuration { get; set; }
        public string AudioGenre { get; set; }
        public string AudioAlbum { get; set; }
        public string Body { get; set; }
        public float Score { get; set; }
        public string ResultText { get; set; }

    }
   
}
