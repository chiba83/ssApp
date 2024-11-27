using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssAppCommon.Extensions
{
   // UTF-8対応のStringWriter
   public class Utf8StringWriter : StringWriter
   {
      public override Encoding Encoding => Encoding.UTF8;
   }
}
