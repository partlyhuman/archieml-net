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
    public class Parser {
        protected static RegexOptions OPTIONS_CX = RegexOptions.Compiled | RegexOptions.IgnorePatternWhitespace;
        protected static Regex KEY_VALUE_PATTERN = new Regex(@"\A\s* (?<key> [a-zA-Z0-9_\-\.]+ ) \s* : \s* (?<value> .+ )$", OPTIONS_CX);
        protected static Regex OBJECT_SCOPE_PATTERN = new Regex(@"\A\s* { \s* (?<key> [a-zA-Z0-9_\-\.]+ )? \s* }", OPTIONS_CX);
        protected static Regex SKIP_COMMAND_PATTERN = new Regex(@"\A\s* : (?: (?<start> skip )|(?<end> endskip) )", OPTIONS_CX | RegexOptions.IgnoreCase);
        protected static Regex IGNORE_COMMAND_PATTERN = new Regex(@"\A\s* :ignore", OPTIONS_CX | RegexOptions.IgnoreCase);
        protected static Regex END_MULTILINE_COMMAND_PATTERN = new Regex(@"\A\s* :end", OPTIONS_CX | RegexOptions.IgnoreCase);
        //protected static Regex MULTILINE_ESCAPES_PATTERN = new Regex(@"^ \\ (?= [ \{ \[ \* \: \\ ] )", OPTIONS_CX);
        protected static Regex ARRAY_PATTERN = new Regex(@"\A\s* \[ \s* (?<freeform> \+\s* )? (?<key> [a-zA-Z0-9_\-\.]+ )? \s* ]", OPTIONS_CX);
        protected static Regex SIMPLE_ARRAY_VALUE_PATTERN = new Regex(@"\A\s* \* \s* (?<value> .+ )", OPTIONS_CX);

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
                // No parsing errors are permitted in Archie. The worst outcome must be that a line is skipped.
                try {
                    bool isLineHandled = false;

                    // Handle :IGNORE
                    if (IGNORE_COMMAND_PATTERN.IsMatch(line)) {
                        // :ignore = We're done here, stop parsing completely
                        break;
                    }

                    // Do all matching up-front because results may be used several times. Groups & captures are matched lazily so this isn't so bad.
                    Match skipCommandMatch = SKIP_COMMAND_PATTERN.Match(line);
                    Match arrayMatch = ARRAY_PATTERN.Match(line);
                    Match simpleArrayValueMatch = SIMPLE_ARRAY_VALUE_PATTERN.Match(line);
                    Match keyValueMatch = KEY_VALUE_PATTERN.Match(line);
                    Match objectScopeMatch = OBJECT_SCOPE_PATTERN.Match(line);


                    // Handle :SKIP/ENDSKIP
                    if (!isLineHandled && skipCommandMatch.Success) {
                        if (skipCommandMatch.Groups["start"].Success) {
                            isSkipping = true;
                            ClearMultilineBuffer(multilineBuffer, ref multilineBufferDestination);
                        }
                        if (skipCommandMatch.Groups["end"].Success) {
                            isSkipping = false;
                        }
                        isLineHandled = true;
                    }

                    // Skip the line before we start parsing anything other than potential :endskip
                    if (isSkipping) {
                        continue;
                    }

                    // Handle :END
                    if (!isLineHandled && END_MULTILINE_COMMAND_PATTERN.IsMatch(line)) {
                        FlushBuffer(multilineBuffer, multilineBufferDestination);
                        isLineHandled = true;
                    }

                    // Handle [ARRAY]
                    if (!isLineHandled && arrayMatch.Success) {
                        ClearMultilineBuffer(multilineBuffer, ref multilineBufferDestination);
                        // TODO this may not be correct. Acting as if any [array] tag starts a new one at root.
                        context = root;
                        if (arrayMatch.Groups["key"].Success) {
                            // [arrayname] - create a new array in the context
                            string keyString = arrayMatch.Groups["key"].Value;

                            JObject target = (JObject)context;
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

                            context = new JArray();
                            target[keyString] = context;

                            contextType = ContextType.UnknownArray;
                            arrayContextFirstKey = null;

                            if (arrayMatch.Groups["freeform"].Success) {
                                contextType = ContextType.FreeformArray;
                            }
                        }
                        else {
                            // [] - close array and go back to global context
                            context = root;
                            contextType = ContextType.Object;
                        }
                        isLineHandled = true;
                    }


                    // Special handling inside freeform, of both plain text and key:value
                    if (!isLineHandled && contextType == ContextType.FreeformArray) {
                        // TODO what about multiline handling?
                        // Do nothing with empty lines
                        string lineTrimmed = line.Trim();
                        if (!string.IsNullOrEmpty(lineTrimmed)) {
                            JObject item = new JObject();
                            if (keyValueMatch.Success) {
                                // Key : Value
                                item["type"] = keyValueMatch.Groups["key"].Value;
                                item["value"] = keyValueMatch.Groups["value"].Value.TrimEnd();
                            }
                            else {
                                item["type"] = "text";
                                item["value"] = lineTrimmed;
                            }
                            context.Add(item);
                        }
                        isLineHandled = true;
                    }

                    // Handle * VALUE
                    if (!isLineHandled && contextType.IsAnyOf(ContextType.StringArray, ContextType.UnknownArray) && simpleArrayValueMatch.Groups["value"].Success) {
                        contextType = ContextType.StringArray;
                        ClearMultilineBuffer(multilineBuffer, ref multilineBufferDestination);
                        string valueString = simpleArrayValueMatch.Groups["value"].Value;
                        JValue value = new JValue(valueString.TrimEnd());
                        multilineBuffer.Append(valueString);
                        multilineBuffer.Append('\n');
                        multilineBufferDestination = value;

                        JArray targetArray = (JArray)context;
                        targetArray.Add(value);

                        isLineHandled = true;
                        // Handle line? Or not - add to multiline?
                    }

                    // Handle KEY : VALUE
                    if (!isLineHandled && keyValueMatch.Success && contextType != ContextType.StringArray) {
                        ClearMultilineBuffer(multilineBuffer, ref multilineBufferDestination);

                        string keyString = keyValueMatch.Groups["key"].Value;
                        string valueString = keyValueMatch.Groups["value"].Value;
                        JValue value = new JValue(valueString.TrimEnd());
                        multilineBuffer.Append(valueString);
                        multilineBuffer.Append('\n');

                        JObject targetObject = context as JObject;
                        if (contextType.IsAnyOf(ContextType.UnknownArray, ContextType.ObjectArray)) {
                            //NOTE: in the Object Array context, the context variable can be either the parent array (when totally empty), or the currently open object array instance.
                            contextType = ContextType.ObjectArray;
                            if (arrayContextFirstKey == null) {
                                arrayContextFirstKey = keyString;
                            }
                            if (arrayContextFirstKey == keyString) {
                                // first key or repeat of first key to appear, create new object
                                targetObject = new JObject();
                                if (context.Type == JTokenType.Object) {
                                    context = context.Parent; //go up to array
                                }
                                // add new object to array
                                context.Add(targetObject);
                                context = targetObject;
                            }
                            else {
                                // add to existing object on array
                                targetObject = (JObject)context;
                            }
                        }

                        TraverseOrCreateIntermediateKeyPath(ref keyString, ref targetObject, includingFinal: false);

                        JProperty keyValue = targetObject.Property(keyString);
                        if (keyValue == null) {
                            keyValue = new JProperty(keyString, value);
                            targetObject.Add(keyValue);
                        }
                        else {
                            keyValue.Value = value;
                        }
                        multilineBufferDestination = keyValue.Value;
                        isLineHandled = true;
                    }

                    // Handle {SCOPE}
                    if (!isLineHandled && objectScopeMatch.Success) {
                        ClearMultilineBuffer(multilineBuffer, ref multilineBufferDestination);

                        bool isObjectScopeEnd = !objectScopeMatch.Groups["key"].Success;
                        if (isObjectScopeEnd) {
                            //{} scope ending, pop context back to root
                            context = root;
                            contextType = ContextType.Object;
                        }
                        else {
                            //{scope} scope starting, create whole key path
                            var keyPathString = objectScopeMatch.Groups["key"].Value;
                            var target = root;
                            TraverseOrCreateIntermediateKeyPath(ref keyPathString, ref target, true);
                            context = target;
                            contextType = ContextType.Object;
                        }
                        isLineHandled = true;
                    }

                    if (!isLineHandled) {
                        // NOTE: spec below may be incorrect.
                        // SPEC: To avoid as much processing as possible, leading backslashes should be removed only when the backslash 
                        // is the first character of a line (but not a value's first line), and when the second character is any of the
                        // following: {, [, *, : or \.
                        //string escapeTrimmedLine = MULTILINE_ESCAPES_PATTERN.Replace(line, "");

                        // NOTE: alternatively, just trim initial \. Tests indicate this is proper behavior.
                        if (line.StartsWith("\\")) {
                            line = line.Remove(0, 1);
                        }

                        multilineBuffer.Append(line);
                        multilineBuffer.Append('\n');
                    }

                } //per-line try block
                catch (Exception e) {
                    Debug.WriteLine("Exception swallowed while parsing ArchieML: " + e.Message);
                    Debug.WriteLine(" Original line: " + line);
                }
            } //line loop
            return root;
        }

        protected void ClearMultilineBuffer(StringBuilder buffer, ref JToken destination) {
            destination = null;
            buffer.Length = 0;
        }

        protected void FlushBuffer(StringBuilder buffer, JToken destination = null) {
            if (destination as JValue != null) {
                // Leading and trailing whitespace is trimmed, all inner whitespace and newlines are preserved.
                ((JValue)destination).Value = buffer.ToString().Trim(' ', '\t', '\n');
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
            keyPathString = includingFinal ? null : keyPath[0];
            return true;
        }
    }
}
