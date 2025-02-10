using System;
using System.ComponentModel;
using System.IO;
using OfficeOpenXml;

namespace ssAppCommon.Helpers;

public static class ExcelHelper
{
   // 初期化：EPPlus のライセンス設定
   static ExcelHelper()
   {
      ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
   }

   /// <summary>
   /// アクティブな Excel ファイルの A1 セルにデータをセット
   /// </summary>
   public static byte[] WriteToActiveSheet(byte[] fileData, string text)
   {
      using var stream = new MemoryStream(fileData);
      using var package = new ExcelPackage(stream);
      var sheet = package.Workbook.Worksheets[0]; // アクティブシートを取得
      sheet.Cells["A1"].Value = text; // A1 セルに文字をセット
      return package.GetAsByteArray(); // 更新後のバイナリデータを返す   }
   }
}