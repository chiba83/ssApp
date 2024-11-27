using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssAppCommon.Extensions
{
   public static class Guard
   {
      public static void AgainstNullOrWhiteSpace(string value, string paramName)
      {
         if (string.IsNullOrWhiteSpace(value))
            throw new ArgumentException($"{paramName} がNullまたはブランクです。", paramName);
      }

      public static void AgainstNull<T>(T value, string parameterName) where T : class
      {
         if (value is null)
         {
            throw new ArgumentNullException(parameterName, $"{parameterName} がNullです。");
         }
      }

      public static void AgainstNullOrEmpty<T>(IEnumerable<T> collection, string parameterName)
      {
         if (collection is null || !collection.Any())
         {
            throw new ArgumentException($"{parameterName}  がNullです。", parameterName);
         }
      }
   }
}

