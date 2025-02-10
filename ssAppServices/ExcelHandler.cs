using System;
using System.IO;
using System.Runtime.InteropServices;
using OfficeOpenXml;
using ssAppCommon.Helpers;

namespace ssAppServices;

[ComVisible(true)]
[Guid("CA80F97B-1D94-48A6-B87D-1460F13EA3CD")]
[ProgId("ssAppServices.ExcelHandler")]
public class ExcelHandler
{
   public byte[] WriteTextToActiveSheet(byte[] fileData, string text)
   {
      try
      {
         return ExcelHelper.WriteToActiveSheet(fileData, text);
      }
      catch (Exception ex)
      {
         throw new Exception("Excel書き込みエラー: " + ex.Message);
      }
   }
}
