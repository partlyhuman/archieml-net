using System;
using System.Linq;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace ArchieML {

    /// <summary>
    /// Public facade to the ArchieML parser
    /// </summary>
    public static class Archie {
        public static object Load(TextReader reader, ParserOptions options = ParserOptions.None) {
            return (new Parser(options)).Parse(reader);
        }

        public static object Load(string input, ParserOptions options = ParserOptions.None) {
            return Load(new StringReader(input), options);
        }

        public static object LoadFile(string filename, ParserOptions options = ParserOptions.None) {
            return Load(File.OpenText(filename), options);
        }
    }

    [Flags]
    public enum ParserOptions {
        None = 0,
        Comments,
        CaseInsensitive,
    }

    /// <summary>
    /// A parser for Archie markup language version 1.0.
    /// Interprets a text stream into a mixed-type, nested collection of dictionaries keyed by string and arrays, implemented by .Net CLR generic Dictionary and List types.
    /// Parse errors should not occur; there is not a concept of a malformed document. Extraneous input is simply ignored.
    /// @see http://archieml.org/spec/1.0/CR-20150509.html
    /// </summary>
    public class Parser {
        //protected static readonly Regex nextLine = new Regex(".*((\r|\n)+)");
        protected static Regex startKey = new Regex("^\\s*([A-Za-z0-9-_\\.]+)[ \t\r]*:[ \t\r]*(.*(?:\n|\r|$))", RegexOptions.Compiled);
        protected static Regex commandKey = new Regex("^\\s*:[ \t\r]*(endskip|ignore|skip|end).*?(\n|\r|$)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        protected static Regex arrayElement = new Regex("^\\s*\\*[ \t\r]*(.*(?:\n|\r|$))", RegexOptions.Compiled);
        protected static Regex scopePattern = new Regex("^\\s*(\\[|\\{)[ \t\r]*([\\+\\.]*)[ \t\r]*([A-Za-z0-9-_\\.]*)[ \t\r]*(?:\\]|\\}).*?(\n|\r|$)", RegexOptions.Compiled);
        protected static Regex leadingWhitespace = new Regex("^\\s*", RegexOptions.Compiled);
        protected static Regex trailingWhitespace = new Regex("\\s*$", RegexOptions.Compiled);


        protected Hashtable data;
        protected ParserOptions options;
        protected object scope;
        protected ArrayList stack; //type?
        protected object stackScope; //type? {arrayType: complex|simple, flags: ...?}
        protected object bufferScope; //type? a.b.c -> bufferScope['a']['b']['c']
        protected string bufferKey; //type? string?
        protected StringBuilder buffer;
        protected bool isSkipping;
        protected bool doneParsing;

        public Parser(ParserOptions options = ParserOptions.None) {
            data = new Hashtable();
            scope = data;
            stack = new ArrayList(); Array
            stackScope = null;
            buffer = new StringBuilder(2048);
            bufferScope = null;
            bufferKey = null;
            isSkipping = false;
            doneParsing = false;
            this.options = options;
        }

        public object Parse(TextReader reader) {
            string line;
            Match match;
            while ((line = reader.ReadLine()) != null) {
                if (doneParsing) {
                    return data;
                }

                if (!isSkipping && (match = startKey.Match(line)) != null && match.Groups.Count > 1) { 
                    ParseStartKey(match.Groups[1].Value, (match.Groups.Count > 2) ? match.Groups[2].Value : "");
                }
                else {
                    buffer.AppendLine(line);
                }
            }

            FlushBuffer();
            return data;
        }

        protected void ParseStartKey(string key, string restOfLine) {
            Debug.WriteLine(string.Format("ParseStartKey({0}, {1})", key, restOfLine));
            data[key] = restOfLine;
            // When a new key is encountered, the rest of the line is immediately added as
            // its value, by calling `flushBuffer`.
            FlushBuffer();

            //IncrementArrayElement(key);

            ////if (stackScope && stackScope.flags.indexOf('+') > -1)
            ////    key = 'value';

            bufferKey = key;
            buffer.Append(restOfLine);

            FlushBufferInto(key, shouldReplace: true);
            bufferKey = key;
        }

        protected string FlushBuffer() {
            var result = buffer.ToString();
            buffer.Length = 0;
            bufferKey = null;
            return result;
        }

        protected void FlushBufferInto(object key, bool shouldReplace = false) {
            var existingBufferKey = bufferKey;
            var value = FlushBuffer();

            if (shouldReplace) {
                //TODO
                //value = FormatValue(value, "replace")
                value = leadingWhitespace.Replace(value, "");
                buffer.Append(trailingWhitespace.Match(value).Value);
            }
            else {
                //TODO
                //value = FormatValue(value, "append");
            }


            if (key is IList) {
                if (shouldReplace) {
                    //TODO
                }
            }
            else if (key is string) {
                var keyBits = ((string)key).Split('.');
                ////bufferScope = scope;

                //for (int i = 0; i < keyBits.Length - 1; i++) {
                //    if (bufferScope[keyBits[i]] is string)
                //}

                if (shouldReplace) {
                    //bufferScope[keyBits.Last() as string] = "";
                }
                data[(string)key] = value;
                //data.Add((string)key, value);
            }

        }
    }
}
