using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace EasyWorkspace
{
    public static class EWHelper
    {
        public static IOrderedEnumerable<T> OrderByAlphaNumeric<T>(this IEnumerable<T> source, Func<T, string> selector)
        {
            int max = source
                .SelectMany(i => Regex.Matches(selector(i), @"\d+").Select(m => (int?)m.Value.Length))
                .Max() ?? 0;

            return source.OrderBy(i => Regex.Replace(selector(i), @"\d+", m => m.Value.PadLeft(max, '0')));
        }
    
        public static IOrderedEnumerable<T> OrderByAlphaNumericDescending<T>(this IEnumerable<T> source, Func<T, string> selector)
        {
            IEnumerable<T> enumerable = source.ToList();
            int max = enumerable
                .SelectMany(i => Regex.Matches(selector(i), @"\d+").Select(m => (int?)m.Value.Length))
                .Max() ?? 0;

            return enumerable.OrderByDescending(i => Regex.Replace(selector(i), @"\d+", m => m.Value.PadLeft(max, '0')));
        }
    
        public static IOrderedEnumerable<T> ThenByAlphaNumeric<T>(this IOrderedEnumerable<T> source, Func<T, string> selector)
        {
            int max = source
                .SelectMany(i => Regex.Matches(selector(i), @"\d+").Select(m => (int?)m.Value.Length))
                .Max() ?? 0;

            return source.ThenBy(i => Regex.Replace(selector(i), @"\d+", m => m.Value.PadLeft(max, '0')));
        }
    
        public static IOrderedEnumerable<T> ThenByAlphaNumericDescending<T>(this IOrderedEnumerable<T> source, Func<T, string> selector)
        {
            IEnumerable<T> enumerable = source.ToList();
            int max = source
                .SelectMany(i => Regex.Matches(selector(i), @"\d+").Select(m => (int?)m.Value.Length))
                .Max() ?? 0;

            return source.ThenByDescending(i => Regex.Replace(selector(i), @"\d+", m => m.Value.PadLeft(max, '0')));
        }
    }
}