using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public enum DocumentType :int
    {
        pdf = 1,
        docx,
        doc,
        ppt,
        pptx,
        xls,
        xlsx,
        txt,
        html,
        htm,
        Other=200
    }
}
