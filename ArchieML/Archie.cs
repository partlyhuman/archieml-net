using System;
using System.IO;
using System.Net;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace ArchieML {

    /// <summary>
    /// Public facade to the ArchieML parser
    /// </summary>
    public static partial class Archie {
        public static JObject Load(TextReader reader, ParserOptions options = ParserOptions.None) {
            return (new Parser(options)).Parse(reader);
        }

        public static JObject Load(string input, ParserOptions options = ParserOptions.None) {
            return Load(new StringReader(input), options);
        }

        public static JObject LoadFile(string filename, ParserOptions options = ParserOptions.None) {
            return Load(File.OpenText(filename), options);
        }

        public static JObject LoadUrl(string url, ParserOptions options = ParserOptions.None) {
            using (var client = new WebClient()) {
                return Load(client.DownloadString(url));
            }
        }

        public static JObject LoadPublicGoogleDoc(string docIdOrUrl, ParserOptions options = ParserOptions.None) {
            const string GDRIVE_DOWNLOAD = @"https://docs.google.com/feeds/download/documents/export/Export?id={0}&exportFormat=txt";
            // extract document ID from url
            var match = Regex.Match(docIdOrUrl, @"[-\w]{25,}");
            var docId = (match.Success) ? match.Value : docIdOrUrl;
            return LoadUrl(string.Format(GDRIVE_DOWNLOAD, docId));
        }
    }
}