using System;
using System.Linq;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace ArchieML {

    /// <summary>
    /// Public facade to the ArchieML parser
    /// </summary>
    public static class Archie {
        public static JObject Load(TextReader reader, ParserOptions options = ParserOptions.None) {
            return (new Parser(options)).Parse(reader);
        }

        public static JObject Load(string input, ParserOptions options = ParserOptions.None) {
            return Load(new StringReader(input), options);
        }

        public static JObject LoadFile(string filename, ParserOptions options = ParserOptions.None) {
            return Load(File.OpenText(filename), options);
        }

        public static JObject Test() {
            return new JObject(new JProperty("key", "value"));
        }
    }

    [Flags]
    public enum ParserOptions {
        None = 0,
        Comments,
        CaseInsensitive,
    }

    public class Parser {

        internal enum ContextType {
            Object,
            ObjectArray,
            StringArray,
            FreeformArray,
        }

        protected static RegexOptions OPTIONS_CX = RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace;
        protected static Regex KEY_VALUE_PATTERN = new Regex(@"^\s* (?<key> [a-zA-Z0-9_\-\.]+ ) \s* : \s* (?<value> .+ )$", OPTIONS_CX);
        protected static Regex OBJECT_SCOPE_PATTERN = new Regex(@"^\s* {{ \s* (?<key> [a-zA-Z0-9_\-\.]+ )? \s* }} \s*$", OPTIONS_CX);
        
        protected ParserOptions _options;

        public Parser(ParserOptions options = ParserOptions.None) {
            this._options = options;
            //RegexOptions ignoreCase = RegexOptions.None;
            //if ((options & ParserOptions.CaseInsensitive) > 0) {
            //    ignoreCase = RegexOptions.IgnoreCase;
            //}
        }

        public JObject Parse(TextReader reader) {
            string line;
            StringBuilder multilineBuffer = new StringBuilder(1024);
            JValue multilineBufferDestination = null;
            JObject root = new JObject();
            JContainer context = root;
            ContextType contextType = ContextType.Object;
            string arrayContextFirstKey = null;
            bool isSkipping = false;

            while ((line = reader.ReadLine()) != null) {
                bool isLineHandled = false;

                Match keyValueMatch = KEY_VALUE_PATTERN.Match(line);
                if (!isLineHandled && contextType == ContextType.Object && keyValueMatch.Success) {
                    FlushBuffer(multilineBuffer);
                    string keyString = keyValueMatch.Groups["key"].Value;
                    string valueString = keyValueMatch.Groups["value"].Value;
                    JValue value = new JValue(valueString.TrimEnd());
                    multilineBuffer.Append(valueString);

                    //TODO set multilineBufferDestination

                    var keyPath = keyString.Split('.').ToList();
                    JObject target = (JObject)context;
                    while (keyPath.Count > 1) {
                        string pathFragment = keyPath.First();
                        keyPath.RemoveAt(0);
                        JToken child = target[pathFragment];
                        if (child == null || child.Type != JTokenType.Object) {
                            target[pathFragment] = new JObject();
                        }
                        target = (JObject)target[pathFragment];
                    }
                    string key = keyPath[0];
                    JProperty keyValue = target.Property(key);
                    if (keyValue == null) {
                        keyValue = new JProperty(key, value);
                        target.Add(keyValue);
                    }
                    else {
                        keyValue.Value = value;
                    }
                    isLineHandled = true;
                }

                if (!isLineHandled) {
                    multilineBuffer.Append(line);
                }
            }

            return root;
        }

        protected void FlushBuffer(StringBuilder buffer, JValue destination = null) {
            if (destination != null) {
                destination.Value = buffer.ToString();
            }
            buffer.Length = 0;
        }
    }
}
