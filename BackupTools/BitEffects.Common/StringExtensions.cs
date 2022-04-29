using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace BitEffects
{
    static public class StringExtensions
    {
        static public T IfEmpty<T>(this T obj, Func<T> defaultValue)
        {
            if (obj.IsEmpty())
            {
                return defaultValue();
            }
            return obj;
        }

        static public bool IsEmpty<T>(this T obj)
        {
            return EqualityComparer<T>.Default.Equals(obj, default(T));
        }

        static public bool IsEmpty(this string str)
        {
            return string.IsNullOrEmpty(str);
        }

        static public int ToInt(this string str)
        {
            return int.TryParse(str ?? string.Empty, out int res)
                ? res
                : 0;
        }

        static public long ToLong(this string str)
        {
            return long.TryParse(str ?? string.Empty, out long res)
                ? res
                : 0;
        }

        static HashSet<string> BOOL_TRUE_VALUES = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "1", "TRUE", "Y", "YES"
        };
        static HashSet<string> BOOL_FALSE_VALUES = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "0", "FALSE", "N", "NO"
        };
        static public bool ToBool(this string str, bool defaultValue = true)
        {
            if (str.IsEmpty())
                return defaultValue;
            else if (BOOL_TRUE_VALUES.Contains(str))
                return true;
            else if (BOOL_FALSE_VALUES.Contains(str))
                return false;
            else
                return defaultValue;
        }

        static public int ToByteSize(this string str)
        {
            int res = 0;

            if (!str.IsEmpty())
            {
                int multiplier = 1;
                switch (str.Last())
                {
                    case 'g':
                    case 'G':
                        multiplier = 1024 * 1024 * 1024;
                        break;
                    case 'm':
                    case 'M':
                        multiplier = 1024 * 1024;
                        break;
                    case 'k':
                    case 'K':
                        multiplier = 1024;
                        break;
                }

                str = Regex.Replace(str, @"[^0-9]*$", "");
                res = str.ToInt() * multiplier;
            }

            return res;
        }

        /// <summary>
        /// Perform a case insensitive ordinal match
        /// </summary>
        /// <param name="str"></param>
        /// <param name="other"></param>
        /// <returns></returns>
        static public bool EqualsCI(this string str, string other)
        {
            return true == str?.Equals(other ?? string.Empty, StringComparison.OrdinalIgnoreCase);
        }

        static public string SplitFirst(this string str, string delim, out string remaining)
        {
            int idx = str.IndexOf(delim);
            remaining = string.Empty;

            if (idx < 0)
            {
                return str;
            }

            remaining = str.Substring(idx + delim.Length);
            return str.Substring(0, idx);
        }

        static public string CamelCased(this string str)
        {
            return str.ToLower().First() + Regex.Replace(str.Substring(1), @"[_-]([a-z])", (match) =>
            {
                return match.Groups[1].Value.ToUpper();
            });
        }

        static public string PascalCased(this string str)
        {
            return str.ToUpper().First() + Regex.Replace(str.Substring(1), @"[_-]([a-z])", (match) =>
            {
                return match.Groups[1].Value.ToUpper();
            });
        }

        static public string CamelCaseToDashed(this string str)
        {
            return Regex.Replace(str, @"([A-Z]+)", (match) =>
            {
                string value = match.Groups[1].Value.ToLower();

                if (value.Length == 1)
                {
                    return "-" + value;
                }
                else
                {
                    return "-" + value.Substring(0, value.Length - 1)
                        + "-" + value.Last();
                }
            })
            .TrimStart('-');
        }

        class DateTimeConverter : JsonConverter<DateTime>
        {
            public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                return DateTime.Parse(reader.GetString()!, CultureInfo.InvariantCulture, DateTimeStyles.None).ToUniversalTime();
                //return DateTime.ParseExact(reader.GetString()!, "yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture).ToUniversalTime();
            }

            public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
            {
                writer.WriteStringValue(value.ToUniversalTime().ToString("o"));
            }
        }

        readonly static JsonSerializerOptions serOptions;
        readonly static JsonSerializerOptions serOptionsFormatted;

        static StringExtensions()
        {
            serOptions = new System.Text.Json.JsonSerializerOptions()
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };

            serOptions.Converters.Add(new DateTimeConverter());

            serOptionsFormatted = new System.Text.Json.JsonSerializerOptions()
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase
            };

            serOptionsFormatted.Converters.Add(new DateTimeConverter());

        }

        static public string Serialize<T>(this T obj, bool formatted = false)
        {
            string res = string.Empty;

            try
            {
                if (formatted)
                {
                    res = System.Text.Json.JsonSerializer.Serialize(obj, serOptionsFormatted);
                }
                else
                {
                    res = System.Text.Json.JsonSerializer.Serialize(obj, serOptions);
                }
            }
            catch { }

            return res;
        }

        static public T Deserialize<T>(this string json)
        {
            T res = default(T);

            try
            {
                res = System.Text.Json.JsonSerializer.Deserialize<T>(json, serOptions);
            }
            catch (Exception ex) { }

            return res;
        }
    }

    static public class EnumerableExtensions
    {
        static public IEnumerable<TSource> DistinctBy<TSource, TKey>(this IEnumerable<TSource> coll, Func<TSource, TKey> keySelector)
        {
            return coll.GroupBy(keySelector).Select(g => g.First());
        }
    }
}
