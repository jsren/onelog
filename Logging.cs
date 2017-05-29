/* Logging.cs - (c) 2017 James S Renwick
 * -------------------------------------
 * Authors: James S Renwick
 * 
 * Contains logic and representations for customisable
 * logging.
 */
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Xml.Linq;


namespace onelog
{
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
}
