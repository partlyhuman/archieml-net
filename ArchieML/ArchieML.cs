using System;
using System.Linq;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

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
        protected static Regex OBJECT_SCOPE_PATTERN = new Regex(@"^\s* { \s* (?<key> [a-zA-Z0-9_\-\.]+ )? \s* }", OPTIONS_CX);
        
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
            JToken multilineBufferDestination = null;
            JObject root = new JObject();
            JContainer context = root;
            ContextType contextType = ContextType.Object;
            string arrayContextFirstKey = null;
            bool isSkipping = false;

            while ((line = reader.ReadLine()) != null) {
                bool isLineHandled = false;

                // Handle KEY : VALUE lines
                Match keyValueMatch = KEY_VALUE_PATTERN.Match(line);
                if (!isLineHandled && contextType == ContextType.Object && keyValueMatch.Success) {
                    FlushBuffer(multilineBuffer);

                    string keyString = keyValueMatch.Groups["key"].Value;
                    string valueString = keyValueMatch.Groups["value"].Value;
                    JValue value = new JValue(valueString.TrimEnd());
                    multilineBuffer.Append(valueString);

                    JObject target = (JObject)context;
                    TraverseOrCreateIntermediateKeyPath(ref keyString, ref target, includingFinal: false);

                    JProperty keyValue = target.Property(keyString);
                    if (keyValue == null) {
                        keyValue = new JProperty(keyString, value);
                        target.Add(keyValue);
                    }
                    else {
                        keyValue.Value = value;
                    }
                    multilineBufferDestination = keyValue.Value;
                    isLineHandled = true;
                }

                // Handle {SCOPE} commands
                Match objectScopeMatch = OBJECT_SCOPE_PATTERN.Match(line);
                if (!isLineHandled && objectScopeMatch.Success) {
                    FlushBuffer(multilineBuffer, multilineBufferDestination);
                    multilineBufferDestination = null;

                    bool isObjectScopeEnd = !objectScopeMatch.Groups["key"].Success;
                    if (isObjectScopeEnd) {
                        //{} scope ending, pop context back to root
                        context = root;
                    }
                    else {
                        //{scope} scope starting, create whole key path
                        var keyPathString = objectScopeMatch.Groups["key"].Value;
                        var target = root;
                        TraverseOrCreateIntermediateKeyPath(ref keyPathString, ref target, true);
                        context = target;
                    }
                    isLineHandled = true;
                }

                if (!isLineHandled) {
                    multilineBuffer.Append(line);
                }
            }

            return root;
        }

        protected void FlushBuffer(StringBuilder buffer, JToken destination = null) {
            if (destination as JValue != null) {
                ((JValue)destination).Value = buffer.ToString();
            }
            buffer.Length = 0;
        }

        protected bool TraverseOrCreateIntermediateKeyPath(ref string keyPathString, ref JObject target, bool includingFinal) {
            //TODO validate keypath
            List<string> keyPath = keyPathString.Split('.').ToList();
            int limit = includingFinal ? 0 : 1;
            while (keyPath.Count > limit) {
                string pathFragment = keyPath.First();
                keyPath.RemoveAt(0);
                JToken child = target[pathFragment];
                if (child == null || child.Type != JTokenType.Object) {
                    //create path fragment or set to object (if it was a simple value before)
                    target[pathFragment] = new JObject();
                }
                target = (JObject)target[pathFragment];
            }
            keyPathString = includingFinal? null : keyPath[0];
            return true;
        }
    }
}
