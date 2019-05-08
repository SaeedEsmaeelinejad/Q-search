using System.Diagnostics;

namespace Common
{
  public class TagForStatistic
    {
        [DebuggerDisplay("{Frequency}, {Text}")]
            public string Text { set; get; }
            public int Frequency { set; get; }
    }
}
