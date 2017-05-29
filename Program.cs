using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace logz
{
    public static class RegexExtensions
    {
        private static string stringPattern1 = "(?:\"(?:[^\\\\\"]|(?:\\\\.))*\")";
        private static string stringPattern2 = "(?:'(?:[^\\\\']|(?:\\\\.))*')";

        private static Regex stringRegex1 = new Regex(
            "((?:[^\\\\\"]|(?:\\\\[^\"]))*)\\\\\"", RegexOptions.Compiled);
        private static Regex stringRegex2 = new Regex(
            "((?:[^\\\\']|(?:\\\\[^']))*)\\\\'", RegexOptions.Compiled);

        public static string EnableStringExtension(string pattern)
        {
            pattern = stringRegex1.Replace(pattern, "$1"+stringPattern1);
            return stringRegex2.Replace(pattern, "$1"+stringPattern2);
        }
    }

    public class ItemBase
    {
        public string Timestamp { get; private set; }
        public string Level { get; private set; }
        public string System { get; private set; }

        public IReadOnlyCollection<string> Filters { get; private set; }

        protected ItemBase(string timestamp, string level, string system,
            IReadOnlyCollection<string> filters)
        {
            Timestamp = timestamp;
            Level = level;
            System = system;
            Filters = filters;
        }
    }

    public class EventItem : ItemBase
    {
        public string Message { get; private set; }

        public EventItem(string timestamp, string level, string system,
            IReadOnlyCollection<string> filters, string message) 
            : base(timestamp, level, system, filters)
        {
            Message = message;
        }
    }

    public class StatusItem : ItemBase
    {
        public string ID { get; private set; }
        public IReadOnlyDictionary<String, String> Assignments { get; private set; }

        public StatusItem(string timestamp, string level, string system,
            IReadOnlyCollection<string> filters, string id,
            IReadOnlyDictionary<String, String> assignments)
            : base(timestamp, level, system, filters)
        {
            ID = id;
            Assignments = assignments;
        }
    }

    public class OtherItem
    {
        public string Message { get; private set; }

        public OtherItem(string message)
        {
            Message = message;
        }
    }


    public struct Any<T1, T2, T3>
    {
        private bool t1;
        private bool t2;
        private bool t3;
        private readonly T1 valueAsT1;
        private readonly T2 valueAsT2;
        private readonly T3 valueAsT3;

        public T1 ValueAsT1
        {
            get
            {
                if (!t1) throw new Exception("Value not present for T1");
                else return valueAsT1;
            }
        }
        public T2 ValueAsT2
        {
            get
            {
                if (!t2) throw new Exception("Value not present for T2");
                else return valueAsT2;
            }
        }
        public T3 ValueAsT3
        {
            get
            {
                if (!t3) throw new Exception("Value not present for T3");
                else return valueAsT3;
            }
        }

        public bool IsT1 { get { return t1; } }
        public bool IsT2 { get { return t2; } }
        public bool IsT3 { get { return t3; } }


        public Any(T1 value)
        {
            t1 = true;
            t2 = false;
            t3 = false;
            valueAsT1 = value;
            valueAsT2 = default(T2);
            valueAsT3 = default(T3);
        }
        public Any(T2 value)
        {
            t1 = false;
            t2 = true;
            t3 = false;
            valueAsT1 = default(T1);
            valueAsT2 = value;
            valueAsT3 = default(T3);
        }
        public Any(T3 value)
        {
            t1 = false;
            t2 = false;
            t3 = true;
            valueAsT1 = default(T1);
            valueAsT2 = default(T2);
            valueAsT3 = value;
        }
    }


    /// <summary>
    /// Object defining a specific log format with which incoming log messages will be parsed.
    /// </summary>
    public class LogFormat
    {
        public Regex Filter { get; private set; }
        public Regex Event { get; private set; }
        public Regex Status { get; private set; }

        /// <summary>
        /// Creates a new log format with the specified regular expressions.
        /// </summary>
        /// <param name="filter"></param>
        /// <param name="event"></param>
        /// <param name="status"></param>
        public LogFormat(string filter, string @event, string status)
        {
            filter = RegexExtensions.EnableStringExtension(filter);
            @event = RegexExtensions.EnableStringExtension(@event);
            status = RegexExtensions.EnableStringExtension(status);

            Filter = new Regex("^\\s*" + filter, RegexOptions.Compiled);
            Event  = new Regex("\\s*(" + @event + ")\\s*$", RegexOptions.Compiled);
            Status = new Regex("\\s*(?:" + status + ")\\s*$", RegexOptions.Compiled);
        }


        public Any<EventItem, StatusItem, OtherItem> ParseMessage(string message)
        {
            // First attempt to parse header
            Match filterMatch = Filter.Match(message);
            if (filterMatch.Success)
            {
                string timestamp = filterMatch.Groups["timestamp"].Value;
                string level = filterMatch.Groups["level"].Value;
                string system = filterMatch.Groups["system"].Value;
                var filters = new List<string>(filterMatch.Groups["filters"].Captures.Count);

                // Add extra filters
                foreach (Capture capture in filterMatch.Groups["filters"].Captures) {
                    filters.Add(capture.Value);
                }

                Match statusMatch = Status.Match(message, filterMatch.Length);
                if (statusMatch.Success)
                {
                    var keyGroup = statusMatch.Groups["keys"];
                    var valueGroup = statusMatch.Groups["values"];
                    var assignments = new Dictionary<string, string>(keyGroup.Captures.Count);

                    // Add extra assignments
                    for (int i = 0; i < keyGroup.Captures.Count; i++) {
                        assignments.Add(keyGroup.Captures[i].Value, valueGroup.Captures[i].Value);
                    }

                    return new Any<EventItem, StatusItem, OtherItem>(new StatusItem(
                        timestamp, level, system, filters, statusMatch.Groups["id"].Value, assignments
                    ));
                }

                Match eventMatch = Event.Match(message, filterMatch.Length);
                if (eventMatch.Success)
                {
                    return new Any<EventItem, StatusItem, OtherItem>(new EventItem(
                        timestamp, level, system, filters, eventMatch.Groups["message"].Value
                    ));
                }
            }
            return new Any<EventItem, StatusItem, OtherItem>(new OtherItem(message));
        }
        

        /// <summary>
        /// Loads a log format from the given XML document stream.
        /// </summary>
        /// <param name="stream">The stream from which to load the XML log format object.</param>
        /// <returns>The loaded LogFormat.</returns>
        public static LogFormat FromXML(System.IO.Stream stream)
        {
            string filter = null, @event = null, status = null;

            var doc = XDocument.Load(stream);

            foreach (var element in doc.Root.Elements())
            {
                switch(element.Name.LocalName)
                {
                    case "filter": filter = element.Value; break;
                    case "event" : @event = element.Value; break;
                    case "status": status = element.Value; break;
                }
            }
            return new LogFormat(filter, @event, status);
        }

        /// <summary>
        /// Gets the default LogFormat.
        /// </summary>
        private /*TODO*/ LogFormat Default { get; set; }
    }

    class Program
    {
        static LogFormat LoadFormat(string filename)
        {
            using (var stream = System.IO.File.OpenRead(filename))
            {
                return LogFormat.FromXML(stream);
            }
        }


        static void Main(string[] args)
        {
            LogFormat format = LoadFormat("default.xml");

            var evt = format.ParseMessage("[INFO] [test] [a] [b] [c] This is a test.");
            var stat = format.ParseMessage("[1:2:3] [INFO] [test] [a] [b] [c] myid { a = 1, b='hello this is James'; c= \"\"} ");

            return;
        }
    }
}
