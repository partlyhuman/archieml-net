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

    /// <summary>
    /// Possible states that the parser can be in depending on its current context. Changes the way lines are interpreted.
    /// </summary>
    internal enum ArchieContext {
        /// <summary>
        /// The context is an Object {}. Key/values, objects, and arrays are allowed. Root context is always an object.
        /// </summary>
        Object,
        /// <summary>
        /// Right after an Array has been created, but before lines have been parsed which hint at the intended contents of the array.
        /// At this point, the array may become an ObjectArray ("complex") array, or a StringArray ("simple") array.
        /// </summary>
        UnknownArray,
        /// <summary>
        /// We're creating an array of objects, or "complex" array. The actual context may be the currently open object,
        /// or the array itself.
        /// </summary>
        ObjectArray,
        /// <summary>
        /// The context is a "simple" Array [] of strings. Only bulleted text * is allowed.
        /// </summary>
        StringArray,
        /// <summary>
        /// The context is a "freeform" Array [], in which each line becomes an object with two properties, a "type" and "value".
        /// </summary>
        FreeformArray,
    }

    internal class Context {
        public ArchieContext Type;
        public JContainer Target;
        public string ArrayContextFirstKey;
        public Context() { }
        public Context(ArchieContext type, JContainer target) : this() {
            Type = type;
            Target = target;
        }
        /// <summary>
        /// Checks if the context is in the list of passed, valid contexts.
        /// </summary>
        public bool IsAnyOf(params ArchieContext[] others) {
            foreach (ArchieContext other in others) {
                if (Type == other)
                    return true;
            }
            return false;
        }
    }


    /// <summary>
    /// Parser implementing the ArchieML Parser 1.0 Spec.
    /// </summary>
    /// <see cref="http://archieml.org/spec/1.0/CR-20150509.html"/>
    internal class Parser {

        protected static RegexOptions OPTIONS_CX = RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace;
        protected static Regex KEY_VALUE_PATTERN = new Regex(@"\A\s* (?<key> [a-zA-Z0-9_\-\.]+ ) \s* : \s* (?<value> .+ )$", OPTIONS_CX);
        protected static Regex OBJECT_SCOPE_PATTERN = new Regex(@"\A\s* { \s* (?<subobject> \.+ )? (?<key> [a-zA-Z0-9_\-\.]+ )? \s* }", OPTIONS_CX);
        protected static Regex SKIP_COMMAND_PATTERN = new Regex(@"\A\s* : (?: (?<start> skip )|(?<end> endskip) )", OPTIONS_CX | RegexOptions.IgnoreCase);
        protected static Regex IGNORE_COMMAND_PATTERN = new Regex(@"\A\s* :ignore", OPTIONS_CX | RegexOptions.IgnoreCase);
        protected static Regex END_MULTILINE_COMMAND_PATTERN = new Regex(@"\A\s* :end", OPTIONS_CX | RegexOptions.IgnoreCase);
        //protected static Regex MULTILINE_ESCAPES_PATTERN = new Regex(@"^ \\ (?= [ \{ \[ \* \: \\ ] )", OPTIONS_CX);
        protected static Regex ARRAY_PATTERN = new Regex(@"\A\s* \[ \s* (?<subarray> \.+ )? (?<freeform> \+\s* )? (?<key> [a-zA-Z0-9_\-\.]+ )? \s* ]", OPTIONS_CX);
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
        /// Shortcut to get to the current context without using the context stack.
        /// </summary>
        protected Context CurrentContext {
            get {
                return _contextStack.Peek();
            }
        }

        protected Stack<Context> _contextStack;


        /// <summary>
        /// Only used when inside an object ("complex") array, the first key encountered is memoized. Whenever it reoccurs, a new object is added to the array and becomes the context.
        /// </summary>
        protected string _arrayContextFirstKey {
            get {
                return CurrentContext.ArrayContextFirstKey;
            }
            set {
                CurrentContext.ArrayContextFirstKey = value;
            }
        }

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
            _contextStack = new Stack<Context>();
            _isSkipping = false;
            _contextStack.Push(new Context(ArchieContext.Object, _root));
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
                    if (ParsePlainTextInFreeformArray()) {
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

        /// <summary>
        /// Interpret the line as plain text if the context is a freeform array. This adds an object of type "text" to the array.
        /// </summary>
        /// <example>
        /// [+freeform]
        /// plain text
        /// is added
        /// line by line
        /// []
        /// </example>
        /// <returns></returns>
        protected bool ParsePlainTextInFreeformArray() {
            if (CurrentContext.Type == ArchieContext.FreeformArray) {
                string lineTrimmed = _line.Trim();
                if (!string.IsNullOrEmpty(lineTrimmed)) {
                    JObject item = new JObject();
                    item["type"] = "text";
                    item["value"] = lineTrimmed;
                    CurrentContext.Target.Add(item);
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Interpret the line if it contains a {SCOPE} command, including {} (end scope).
        /// Changes the context.
        /// </summary>
        /// <example>
        /// {location}
        /// shortcut: Home
        /// url.scheme: http
        /// url.domain: archieml.org
        /// {}
        /// </example>
        /// <returns>Whether the line was handled</returns>
        protected bool ParseScopeCommands() {
            Match objectScopeMatch = OBJECT_SCOPE_PATTERN.Match(_line);
            if (objectScopeMatch.Success) {
                ClearMultilineBuffer();

                bool isObjectScopeEnd = !objectScopeMatch.Groups["key"].Success;
                if (isObjectScopeEnd) {
                    //{} scope ending, pop context back to root
                    PopContext();
                }
                else {
                    var keyPathString = objectScopeMatch.Groups["key"].Value;
                    JObject target;
                    if (CurrentContext.Type == ArchieContext.FreeformArray) {
                        var item = new JObject();
                        target = new JObject();
                        item["type"] = keyPathString;
                        item["value"] = target;
                        CurrentContext.Target.Add(item);
                    } else {
                        PopContext(); //TODO this can't be right
                        target = NearestEnclosingJObject(CurrentContext.Target);
                        TraverseOrCreateIntermediateKeyPath(ref keyPathString, ref target, true);
                    }
                    _contextStack.Push(new Context(ArchieContext.Object, target));
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
        protected bool ParseKeyValues() {
            // Handle KEY : VALUE
            Match keyValueMatch = KEY_VALUE_PATTERN.Match(_line);
            if (keyValueMatch.Success && CurrentContext.Type != ArchieContext.StringArray) {
                ClearMultilineBuffer();

                string keyString = keyValueMatch.Groups["key"].Value;
                string valueString = keyValueMatch.Groups["value"].Value;
                JValue value = new JValue(valueString.TrimEnd());
                _multilineBuffer.Append(valueString);
                _multilineBuffer.Append('\n');

                JObject targetObject = NearestEnclosingJObject(CurrentContext.Target);

                // If we're in an Object array
                if (CurrentContext.IsAnyOf(ArchieContext.UnknownArray, ArchieContext.ObjectArray)) {
                    //NOTE: in the Object Array context, the context variable can be either the parent array (when totally empty), or the currently open object array instance.
                    CurrentContext.Type = ArchieContext.ObjectArray;
                    if (_arrayContextFirstKey == null) {
                        _arrayContextFirstKey = keyString;
                    }
                    if (_arrayContextFirstKey == keyString) {
                        // first key or repeat of first key to appear, create new object
                        targetObject = new JObject();
                        if (CurrentContext.Target.Type == JTokenType.Object) {
                            CurrentContext.Target = CurrentContext.Target.Parent; //go up to array
                        }
                        // add new object to array
                        CurrentContext.Target.Add(targetObject);
                        CurrentContext.Target = targetObject;
                    }
                }

                // If we're in a Freeform array
                if (CurrentContext.Type == ArchieContext.FreeformArray) {
                    //Freeform arrays don't use dot navigation in key names
                    //Freeform arrays don't do multiline text, they make new entries for each line
                    JObject item = new JObject();
                    item["type"] = keyValueMatch.Groups["key"].Value;
                    item["value"] = keyValueMatch.Groups["value"].Value.TrimEnd();
                    CurrentContext.Target.Add(item);
                }
                else {
                    // In an object, or object array, add the key and value to the context object, including key path.
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
        protected bool ParseSimpleArrayBullets() {
            Match simpleArrayValueMatch = SIMPLE_ARRAY_VALUE_PATTERN.Match(_line);
            if (CurrentContext.IsAnyOf(ArchieContext.StringArray, ArchieContext.UnknownArray) && simpleArrayValueMatch.Groups["value"].Success) {
                CurrentContext.Type = ArchieContext.StringArray;
                ClearMultilineBuffer();
                string valueString = simpleArrayValueMatch.Groups["value"].Value;
                JValue value = new JValue(valueString.TrimEnd());
                _multilineBuffer.Append(valueString);
                _multilineBuffer.Append('\n');
                _multilineBufferDestination = value;

                JArray targetArray = (JArray)CurrentContext.Target;
                targetArray.Add(value);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Interpret the line if it has an [ARRAY] command, including [.NestedArrays], [+FreeformArrays], and [] (End arrays).
        /// Changes the context.
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
        protected bool ParseArrayCommands() {
            Match arrayMatch = ARRAY_PATTERN.Match(_line);
            if (arrayMatch.Success) {
                ClearMultilineBuffer();
                // TODO this may not be correct. Acting as if any [array] tag starts a new one at root.
                if (arrayMatch.Groups["key"].Success) {
                    // [arrayname] - create a new array in the context
                    string keyString = arrayMatch.Groups["key"].Value;
                    bool isSubarray = arrayMatch.Groups["subarray"].Success;

                    JObject target = NearestEnclosingJObject(CurrentContext.Target);
                    if (isSubarray) {
                        switch (CurrentContext.Type) {
                            case ArchieContext.UnknownArray:
                                target = new JObject();
                                CurrentContext.Target.Add(target);
                                //NOTE this is not an error, we upgrade current context to Object array first.
                                CurrentContext.Type = ArchieContext.ObjectArray;
                                CurrentContext.Target = target;
                                CurrentContext.ArrayContextFirstKey = keyString;
                                break;
                            case ArchieContext.FreeformArray:
                                target = new JObject(new JProperty("type", keyString));
                                keyString = "value";
                                CurrentContext.Target.Add(target);
                                break;
                            case ArchieContext.ObjectArray:
                                if (keyString == CurrentContext.ArrayContextFirstKey) {
                                    target = new JObject();
                                    CurrentContext.Target.Parent.Add(target);
                                    CurrentContext.Target = target;
                                }
                                break;
                        }
                    }
                    else {
                        PopContext();
                        target = NearestEnclosingJObject(CurrentContext.Target);
                    }

                    if (CurrentContext.Type != ArchieContext.FreeformArray) {
                        TraverseOrCreateIntermediateKeyPath(ref keyString, ref target, includingFinal: false);
                    }
                    var newArray = new JArray();
                    target[keyString] = newArray;

                    var newContext = new Context(ArchieContext.UnknownArray, newArray);
                    if (arrayMatch.Groups["freeform"].Success) {
                        newContext.Type = ArchieContext.FreeformArray;
                    }
                    _contextStack.Push(newContext);
                }
                else {
                    // no key, empty [] - close array and pop to previous context
                    PopContext();
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
        protected bool ParseSkipCommands() {
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

        protected JObject NearestEnclosingJObject(JToken token) {
            while ((token.Type != JTokenType.Object) && (token.Parent != null)) {
                token = token.Parent;
            }
            return (JObject)token;
        }

        protected void PopContext() {
            if (_contextStack.Count > 1) {
                _contextStack.Pop();
            }
        }

        protected void PopContextToRoot() {
            while (_contextStack.Count > 1) {
                _contextStack.Pop();
            }
        }
    }
}

