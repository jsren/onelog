/* Program.cs - (c) 2017 James S Renwick
 * -------------------------------------
 * Authors: James S Renwick
 * 
 * Example program for onelog.
 */

namespace onelog
{
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
