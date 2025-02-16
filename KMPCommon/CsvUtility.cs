using System;
using System.Collections.Generic;
using System.IO;

namespace KMPCommon
{
    public static class CsvUtility
    {
        public enum BreakLineStatus
        {
            Normal,
            InQuote,
            ExitingQuote,
            NormalAfterQuote
        }

        /// <summary>
        ///  Return the fields of a complete CSV row which may contains multi-line fields
        /// </summary>
        /// <param name="sr">The stream to read the CSV from.</param>
        /// <param name="trim">Whether to trim the loaded fields.</param>
        /// <returns>All the loaded fields of the row.</returns>
        public static IEnumerable<string> GetAndBreakRow(this StreamReader sr, bool trim = false)
        {
            var done = false;
            string? lastVal = null;
            var status = BreakLineStatus.Normal;
            while (!done)
            {
                var line = sr.ReadLine();
                if (line == null)
                {
                    break;
                }

                var fieldVals = line.BreakLine(trim, status);
                var first = true;
                done = true;
                foreach (var val in fieldVals)
                {
                    if (first)
                    {
                        if (lastVal != null)
                        {
                            lastVal = lastVal + '\n' + val;
                        }
                        else
                        {
                            lastVal = val;
                        }

                        first = false;
                    }
                    else
                    {
                        if (val == null)
                        {
                            done = false;
                            status = BreakLineStatus.InQuote;
                            break;
                        }
                        else
                        {
                            yield return lastVal!;
                            lastVal = val;
                        }
                    }
                }
            }

            if (lastVal != null)
            {
                yield return lastVal;
            }
        }

        /// <summary>
        ///  Read a CSV line and returns the fields.
        /// </summary>
        /// <param name="line">The line</param>
        /// <param name="trim">Whether to trim the loaded fields</param>
        /// <param name="initialStatus">If the line starts with the continuation of a  multiline field</param>
        /// <returns>All the fields from the line. If the last item is an incomplete multiline item then an additional null is yielded.</returns>
        public static IEnumerable<string?> BreakLine(this string line, bool trim = false,
            BreakLineStatus initialStatus = BreakLineStatus.Normal)
        {
            BreakLineStatus status = initialStatus;
            string lastItem = "";
            foreach (var ch in line)
            {
                if (status == BreakLineStatus.Normal || status == BreakLineStatus.NormalAfterQuote)
                {
                    if (ch == ',')
                    {
                        yield return trim ? lastItem.Trim() : lastItem;
                        lastItem = "";
                        status = BreakLineStatus.Normal;
                    }
                    else if (ch == '"' && lastItem == "")
                    {
                        status = BreakLineStatus.InQuote;
                    }
                    else
                    {
                        lastItem += ch;
                    }
                }
                else if (status == BreakLineStatus.InQuote)
                {
                    if (ch == '"')
                    {
                        status = BreakLineStatus.ExitingQuote;
                    }
                    else
                    {
                        lastItem += ch;
                    }
                }
                else // status == BreakLineStatus.ExitingQuote
                {
                    if (ch == '"')
                    {
                        lastItem += '"';
                        status = BreakLineStatus.InQuote;
                    }
                    else if (ch == ',')
                    {
                        yield return trim ? lastItem.Trim() : lastItem;
                        lastItem = "";
                        status = BreakLineStatus.Normal;
                    }
                    else
                    {
                        lastItem += ch;
                        status = BreakLineStatus.NormalAfterQuote;
                    }
                }
            }

            if (lastItem != "")
            {
                yield return trim ? lastItem.Trim() : lastItem;
            }

            if (status == BreakLineStatus.InQuote)
            {
                yield return null;
            }
        }

        public static DateTime ParseDateTime(string fieldValue)
        {
            // TODO implement
            return DateTime.Parse(fieldValue);
        }

        public static string ToShortDateOnlyString(this DateTime date)
            => date.Date.ToShortDateString();

        public static DateTime ParseTimestamp(string timestamp)
        {
            var yearStr = timestamp[..4];
            var monthStr = timestamp[4..6];
            var dateStr = timestamp[6..8];
            var hrStr = timestamp[8..10];
            var minStr = timestamp[10..12];
            var secStr = timestamp[12..14];

            var year = int.Parse(yearStr);
            var month = int.Parse(monthStr);
            var date = int.Parse(dateStr);
            var hr = int.Parse(hrStr);
            var min = int.Parse(minStr);
            var sec = int.Parse(secStr);

            return new DateTime(year, month, date, hr, min, sec);
        }

        public static string TimestampToString(DateTime dateTime)
        {
            return
                $"{dateTime.Year:0000}{dateTime.Month:00}{dateTime.Day:00}{dateTime.Hour:00}{dateTime.Minute:00}{dateTime.Second:00}";
        }

        public static bool TimestampsAreEqual(DateTime dateTime1, DateTime dateTime2)
        {
            return dateTime1.Year.Equals(dateTime2.Year) && dateTime1.Month.Equals(dateTime2.Month) &&
                   dateTime1.Day.Equals(dateTime2.Day) && dateTime1.Hour.Equals(dateTime2.Hour) &&
                   dateTime1.Minute.Equals(dateTime2.Minute) && dateTime1.Second.Equals(dateTime2.Second);
        }

        public static string StringToCsvField(this string field)
        {
            if (field.Contains(',') || field.StartsWith('"'))
            {
                return "\"" + field.Replace("\"", "\\\"") + "\"";
            }
            else
            {
                return field;
            }
        }

        public static decimal? ParseDecimalValue(string str)
        {
            if (str.Contains('$') || str.Contains(','))
            {
                str = str.Replace("$", "").Replace(",", "").Replace("\"", "");
            }

            if (decimal.TryParse(str, out var val))
            {
                return val;
            }

            return null;
        }
    }
}