using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ssAppCommon.Extensions
{
   public static class Reflection
   {
      /// <summary>
      /// プロパティの値を設定する。（動的Property対応）
      /// </summary>
      /// <typeparam name="T"></typeparam>
      /// <param name="obj"></param>
      /// <param name="propertyName"></param>
      /// <param name="value"></param>
      public static T SetProperty<T>(T obj, string propertyName, object value)
      {
         var property = obj?.GetType().GetProperty(propertyName);
         if (property == null)
            return obj;

         property.SetValue(obj, value);
         return obj;
      }

      /// <summary>
      /// プロパティの値を取得する。（動的Property対応）
      /// </summary>
      /// <param name="obj"></param>
      /// <param name="propertyName"></param>
      public static object? GetProperty(object obj, string propertyName)
      {
         var property = obj?.GetType().GetProperty(propertyName);
         if (property == null)
            return null;

         return property.GetValue(obj);
      }

      /// <summary>
      /// 引数のvalueを指定のfieldTypeに変換して返す。（valueがnullの場合は型のデフォルト値を返す）
      /// </summary>
      /// <param name="fieldType"></param>
      /// <param name="value"></param>
      public static object CreateInstance(Type? fieldType, string? value)
      {
         if (fieldType == null) return string.Empty;

         if (fieldType == typeof(string))
         {
            return !string.IsNullOrEmpty(value) ? value : string.Empty;
         }
         else
         {
            return !string.IsNullOrEmpty(value)
                ? Convert.ChangeType(value, fieldType)
                : Activator.CreateInstance(fieldType) ?? string.Empty;
         }
      }
   }
}