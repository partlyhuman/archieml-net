using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;

namespace ArchieML {

    [Flags]
    public enum ParserOptions {
        None = 0,
        Comments,
        CaseInsensitive,
    }

    internal enum ContextType {
        Object,
        UnknownArray,
        ObjectArray,
        StringArray,
        FreeformArray,
    }

    internal static class ContextTypeExtensions {
        internal static bool IsAnyOf(this ContextType self, params ContextType[] others) {
            foreach (ContextType other in others) {
                if (self == other)
                    return true;
            }
            return false;
        }
        internal static bool IsObjectLike(this ContextType self) {
            return (self == ContextType.Object || self == ContextType.ObjectArray);
        }
    }

    /// <summary>
    /// Parser implementing the ArchieML Parser 1.0 Spec
    /// @see http://archieml.org/spec/1.0/CR-20150509.html
    /// </summary>
    internal class Parser {

        protected static RegexOptions OPTIONS_CX = RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace;
        protected static Regex KEY_VALUE_PATTERN = new Regex(@"\A\s* (?<key> [a-zA-Z0-9_\-\.]+ ) \s* : \s* (?<value> .+ )$", OPTIONS_CX);
        protected static Regex OBJECT_SCOPE_PATTERN = new Regex(@"\A\s* { \s* (?<key> [a-zA-Z0-9_\-\.]+ )? \s* }", OPTIONS_CX);
        protected static Regex SKIP_COMMAND_PATTERN = new Regex(@"\A\s* : (?: (?<start> skip )|(?<end> endskip) )", OPTIONS_CX | RegexOptions.IgnoreCase);
        protected static Regex IGNORE_COMMAND_PATTERN = new Regex(@"\A\s* :ignore", OPTIONS_CX | RegexOptions.IgnoreCase);
        protected static Regex END_MULTILINE_COMMAND_PATTERN = new Regex(@"\A\s* :end", OPTIONS_CX | RegexOptions.IgnoreCase);
        //protected static Regex MULTILINE_ESCAPES_PATTERN = new Regex(@"^ \\ (?= [ \{ \[ \* \: \\ ] )", OPTIONS_CX);
        protected static Regex ARRAY_PATTERN = new Regex(@"\A\s* \[ \s* (?<freeform> \+\s* )? (?<subarray> \.+ )? (?<key> [a-zA-Z0-9_\-\.]+ )? \s* ]", OPTIONS_CX);
        protected static Regex SIMPLE_ARRAY_VALUE_PATTERN = new Regex(@"\A\s* \* \s* (?<value> .+ )", OPTIONS_CX);

        /// <summary>
        /// Parsing options
        /// </summary>
        protected ParserOptions _options;

        /// <summary>
        /// Current line being parsed
        /// </summary>
        protected string _line;

        /// <summary>
        /// Collects lines of text until an :end is encountered or a new value begins
        /// </summary>
        protected StringBuilder _multilineBuffer;

        /// <summary>
        /// The last assigned value, where collected lines of text will all end up if an :end is encountered
        /// </summary>
        protected JValue _multilineBufferDestination;

        /// <summary>
        /// The root of the data being parsed. Archie can return at any point and this will contain all data parsed up to that point.
        /// </summary>
        protected readonly JObject _root;

        /// <summary>
        /// The current context, where new data will be added as it is encountered, much like a "with" block in Ecmascript.
        /// </summary>
        protected JContainer _context;

        /// <summary>
        /// The type of context we are in currently, which changes how we may interpret a line. See enum ContextType for possible values.
        /// </summary>
        protected ContextType _contextType;

        /// <summary>
        /// Only used when inside an object ("complex") array, the first key encountered is memoized. Whenever it reoccurs, a new object is added to the array and becomes the context.
        /// </summary>
        protected string _arrayContextFirstKey;

        /// <summary>
        /// Whether we are between a :skip and :endskip command. Lines are ignored until :endskip is encountered.
        /// </summary>
        protected bool _isSkipping;

        /// <summary>
        /// Construct a new parser with the specified options. Sets up the instance variables to get ready to parse.
        /// </summary>
        /// <param name="options">A bitmask of any options that control parsing behavior. CURRENTLY DOES NOTHING.</param>
        public Parser(ParserOptions options = ParserOptions.None) {
            _options = options;
            _multilineBuffer = new StringBuilder(1024);
            _multilineBufferDestination = null;
            _root = new JObject();
            _context = _root;
            _contextType = ContextType.Object;
            _arrayContextFirstKey = null;
            _isSkipping = false;
        }

        /// <summary>
        /// Perform the actual parsing.
        /// NOTE it is currently illegal to call Parse() more than once on a parser instance.
        /// </summary>
        /// <param name="reader">A stream of ArchieML text to parse.</param>
        /// <returns>A LINQ-to-Json Object token from JSON.Net, populated with our interpretation of the stream's contents.</returns>
        public JObject Parse(TextReader reader) {
            while ((_line = reader.ReadLine()) != null) {
                // No parsing errors are permitted in Archie. The worst outcome must be that a line is skipped.
                try {
                    if (IGNORE_COMMAND_PATTERN.IsMatch(_line)) {
                        // :ignore = We're done here, stop parsing completely
                        break;
                    }
                    if (ParseSkipCommands()) {
                        continue;
                    }
                    if (_isSkipping) {
                        // Skip the line before we start parsing anything other than potential :endskip
                        continue;
                    }
                    if (END_MULTILINE_COMMAND_PATTERN.IsMatch(_line)) {
                        CommitMultilineBuffer();
                        continue;
                    }
                    if (ParseArrayCommands()) {
                        continue;
                    }
                    if (ParseSimpleArrayBullets()) {
                        continue;
                    }
                    if (ParseKeyValues()) {
                        continue;
                    }
                    if (ParseScopeCommands()) {
                        continue;
                    }
                    if (_contextType == ContextType.FreeformArray) {
                        //Plain or otherwise unhandled text in a freeform array gets added as a text line
                        ParsePlainTextInFreeformArray();
                        continue;
                    }

                    // Otherwise, escape line and add to multiline buffer.
                    if (_line.StartsWith("\\")) {
                        _line = _line.Remove(0, 1);
                    }
                    _multilineBuffer.Append(_line);
                    _multilineBuffer.Append('\n');
                } //per-line try block
                catch (Exception e) {
                    Debug.WriteLine("Exception swallowed while parsing ArchieML: " + e.Message);
                    Debug.WriteLine(" Original line: " + _line);
                }
            } //line loop
            return _root;
        }

        private void ParsePlainTextInFreeformArray() {
            string lineTrimmed = _line.Trim();
            if (!string.IsNullOrEmpty(lineTrimmed)) {
                JObject item = new JObject();
                item["type"] = "text";
                item["value"] = lineTrimmed;
                _context.Add(item);
            }
        }

        /// <summary>
        /// Interpret the line if it contains a {SCOPE} command, starting or ending a scope.
        /// </summary>
        /// <example>
        /// {location}
        /// shortcut: Home
        /// url.scheme: http
        /// url.domain: archieml.org
        /// {}
        /// </example>
        /// <returns>Whether the line was handled</returns>
        private bool ParseScopeCommands() {
            Match objectScopeMatch = OBJECT_SCOPE_PATTERN.Match(_line);
            if (objectScopeMatch.Success) {
                ClearMultilineBuffer();

                bool isObjectScopeEnd = !objectScopeMatch.Groups["key"].Success;
                if (isObjectScopeEnd) {
                    //{} scope ending, pop context back to root
                    _context = _root;
                    _contextType = ContextType.Object;
                }
                else {
                    //{scope} scope starting, create whole key path
                    var keyPathString = objectScopeMatch.Groups["key"].Value;
                    var target = _root;
                    TraverseOrCreateIntermediateKeyPath(ref keyPathString, ref target, true);
                    _context = target;
                    _contextType = ContextType.Object;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Interpret the line if it is a KEY: VALUE line.
        /// This one can depend on context quite a bit.
        /// </summary>
        /// <example>
        /// key:a really long
        /// multi line
        /// value
        /// :end
        /// application.name: aml
        /// application.copyright: 2015
        /// </example>
        /// <returns>Whether the line was handled</returns>
        private bool ParseKeyValues() {
            // Handle KEY : VALUE
            Match keyValueMatch = KEY_VALUE_PATTERN.Match(_line);
            if (keyValueMatch.Success && _contextType != ContextType.StringArray) {
                ClearMultilineBuffer();

                string keyString = keyValueMatch.Groups["key"].Value;
                string valueString = keyValueMatch.Groups["value"].Value;
                JValue value = new JValue(valueString.TrimEnd());
                _multilineBuffer.Append(valueString);
                _multilineBuffer.Append('\n');

                JObject targetObject = _context as JObject;

                // If we're in an Object array
                if (_contextType.IsAnyOf(ContextType.UnknownArray, ContextType.ObjectArray)) {
                    //NOTE: in the Object Array context, the context variable can be either the parent array (when totally empty), or the currently open object array instance.
                    _contextType = ContextType.ObjectArray;
                    if (_arrayContextFirstKey == null) {
                        _arrayContextFirstKey = keyString;
                    }
                    if (_arrayContextFirstKey == keyString) {
                        // first key or repeat of first key to appear, create new object
                        targetObject = new JObject();
                        if (_context.Type == JTokenType.Object) {
                            _context = _context.Parent; //go up to array
                        }
                        // add new object to array
                        _context.Add(targetObject);
                        _context = targetObject;
                    }
                    else {
                        // add to existing object on array
                        targetObject = (JObject)_context;
                    }
                }

                // If we're in a Freeform array
                if (_contextType == ContextType.FreeformArray) {
                    //Freeform arrays don't use dot navigation in key names
                    //Freeform arrays don't do multiline text, they make new entries for each line
                    JObject item = new JObject();
                    item["type"] = keyValueMatch.Groups["key"].Value;
                    item["value"] = keyValueMatch.Groups["value"].Value.TrimEnd();
                    _context.Add(item);
                }
                else {
                    TraverseOrCreateIntermediateKeyPath(ref keyString, ref targetObject, includingFinal: false);
                    JProperty keyValue = targetObject.Property(keyString);
                    if (keyValue == null) {
                        keyValue = new JProperty(keyString, value);
                        targetObject.Add(keyValue);
                    }
                    else {
                        keyValue.Value = value;
                    }
                    _multilineBufferDestination = value;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Interpret the line if it has a * BULLET and we're in a context that is or could be a simple array.
        /// </summary>
        /// <example>
        /// [languages]
        /// * es
        /// * en
        /// * jp
        /// * de
        /// []
        /// </example>
        /// <returns>Whether the line was handled</returns>
        private bool ParseSimpleArrayBullets() {
            Match simpleArrayValueMatch = SIMPLE_ARRAY_VALUE_PATTERN.Match(_line);
            if (_contextType.IsAnyOf(ContextType.StringArray, ContextType.UnknownArray) && simpleArrayValueMatch.Groups["value"].Success) {
                _contextType = ContextType.StringArray;
                ClearMultilineBuffer();
                string valueString = simpleArrayValueMatch.Groups["value"].Value;
                JValue value = new JValue(valueString.TrimEnd());
                _multilineBuffer.Append(valueString);
                _multilineBuffer.Append('\n');
                _multilineBufferDestination = value;

                JArray targetArray = (JArray)_context;
                targetArray.Add(value);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Interpret the line if it has an [ARRAY] command, including [.NestedArrays], [+FreeformArrays], and [] (End arrays).
        /// </summary>
        /// <example>
        /// [Contacts]
        /// name: roger
        /// [.phones]
        /// * 555-0001
        /// * 555-0002
        /// []
        /// email: roger@partlyhuman.com
        /// twitter: @partlyhuman
        /// name: other
        /// []
        /// </example>
        /// <returns>Whether the line was handled</returns>
        private bool ParseArrayCommands() {
            Match arrayMatch = ARRAY_PATTERN.Match(_line);
            if (arrayMatch.Success) {
                ClearMultilineBuffer();
                // TODO this may not be correct. Acting as if any [array] tag starts a new one at root.
                _context = _root;
                if (arrayMatch.Groups["key"].Success) {
                    // [arrayname] - create a new array in the context
                    string keyString = arrayMatch.Groups["key"].Value;

                    JObject target = (JObject)_context;
                    TraverseOrCreateIntermediateKeyPath(ref keyString, ref target, includingFinal: false);

                    // NOTE redefining an array clears it
                    // @see arrays_complex.13.aml
                    //if (target[keyString] as JArray != null) {
                    //    // array path already exists, set context to it
                    //    context = (JArray)target[keyString];
                    //}
                    //else {
                    //    // no existing array path, create it and set context to it
                    //    context = new JArray();
                    //    target[keyString] = context;
                    //}

                    _context = new JArray();
                    target[keyString] = _context;

                    _contextType = ContextType.UnknownArray;
                    _arrayContextFirstKey = null;

                    if (arrayMatch.Groups["freeform"].Success) {
                        _contextType = ContextType.FreeformArray;
                    }
                }
                else {
                    // [] - close array and go back to global context
                    _context = _root;
                    _contextType = ContextType.Object;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Interpret the line if it has a :SKIP or :ENDSKIP.
        /// </summary>
        /// <example>
        /// :skip
        /// key: values here are all ignored until
        /// :endskip
        /// </example>
        /// <returns>Whether the line was handled</returns>
        private bool ParseSkipCommands() {
            Match skipCommandMatch = SKIP_COMMAND_PATTERN.Match(_line);
            if (skipCommandMatch.Success) {
                if (skipCommandMatch.Groups["start"].Success) {
                    _isSkipping = true;
                    ClearMultilineBuffer();
                }
                if (skipCommandMatch.Groups["end"].Success) {
                    _isSkipping = false;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Abort any pending, potential multi-line text, and reset the buffer and its intended destination.
        /// </summary>
        protected void ClearMultilineBuffer() {
            _multilineBufferDestination = null;
            _multilineBuffer.Length = 0;
        }

        /// <summary>
        /// Commit the multiline buffer's contents into the last encountered value. Called from an :end line.
        /// </summary>
        protected void CommitMultilineBuffer() {
            if (_multilineBufferDestination != null) {
                // Leading and trailing whitespace is trimmed, all inner whitespace and newlines are preserved.
                _multilineBufferDestination.Value = _multilineBuffer.ToString().Trim(' ', '\t', '\n');
            }
            ClearMultilineBuffer();
        }

        /// <summary>
        /// Traverses a dot-notation object path from the current target, using existing keys in the path when
        /// they are Objects, or creating intermediary Objects when that key is not there or isn't an Object type.
        /// This method will modify the target as it traverses, and leave your key path variable as only the last bit,
        /// or nothing, depending on whether you want to consume the whole path.
        /// </summary>
        /// <example>
        /// var context = {a: {b: {c: 10}}};
        /// var path = "a.b.c";
        /// TraverseOrCreateIntermediateKeyPath(ref path, ref context, false);
        /// path == "c";
        /// context == {c: 10};
        /// </example>
        /// <param name="keyPathString">Pass in a reference to the full key path, receive whatever is remaining in the path</param>
        /// <param name="target">Pass in an object reference, which is transformed to point to the path you specify</param>
        /// <param name="includingFinal">When true, the entire path is consumed. When false, consumed up to the last faragment.</param>
        protected void TraverseOrCreateIntermediateKeyPath(ref string keyPathString, ref JObject target, bool includingFinal) {
            List<string> keyPath = keyPathString.Split('.').ToList();
            int limit = includingFinal ? 0 : 1;
            while (keyPath.Count > limit) {
                string pathFragment = keyPath[0];
                keyPath.RemoveAt(0);
                if (string.IsNullOrEmpty(pathFragment)) {
                    continue;
                }
                JToken child = target[pathFragment];
                if (child == null || child.Type != JTokenType.Object) {
                    //create path fragment or set to object (if it was a simple value before)
                    target[pathFragment] = new JObject();
                }
                target = (JObject)target[pathFragment];
            }
            keyPathString = includingFinal ? null : keyPath[0];
        }
    }
}

