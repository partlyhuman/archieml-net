using System;
using ArchieML;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

// NOTE: Take special caution here to preserve and view whitespace in the string literals.

namespace ArchieML_Tests {
    [TestClass]
    public class MultiLineTests {
        [TestMethod]
        public void MultiLine01AddsAdditionalLinesToValue() {
            var result = Archie.Load(@"
key:value
extra
:end
");
            var expected = JObject.Parse("{'key': 'value\nextra'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine02EndIsCaseInsensitive() {
            var result = Archie.Load(@"
key:value
extra
:EnD
");
            var expected = JObject.Parse("{'key': 'value\nextra'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine03PreservesBlankLinesAndWhitespace() {
            var result = Archie.Load(@"
key:value

	 
extra
:end
");
            var expected = JObject.Parse("{'key': 'value\n\n\t \nextra'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine04DoesNotPreserveWhitespaceAtEndOfMultilineValue() {
            var result = Archie.Load(@"
key:value
extra	 
:end
");
            var expected = JObject.Parse("{'key': 'value\nextra'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine05PreservesWhitespaceAtEndOfFirstLine() {
            var result = Archie.Load(@"
key:value	 
extra
:end
");
            var expected = JObject.Parse("{'key': 'value\t \nextra'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine06IgnoresWhitespaceAndNewlinesBeforeEnd() {
            var result = Archie.Load(@"
key:value
extra
 
	
:end
");
            var expected = JObject.Parse("{'key': 'value\nextra'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine07IgnoresWhitespaceSurroundingEndCommand() {
            var result = Archie.Load(@"
key:value
extra
  :end  
");
            var expected = JObject.Parse("{'key': 'value\nextra'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine08IgnoresTabsSurroundingEndCommand() {
            var result = Archie.Load(@"
key:value
extra
		:end		
");
            var expected = JObject.Parse("{'key': 'value\nextra'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine09ParsesEndCommandWithExtraneousCharacters() {
            var result = Archie.Load(@"
key:value
extra
:endthis
");
            var expected = JObject.Parse("{'key': 'value\nextra'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine10DoesNotParseEndSkipCommandAsEnd() {
            var result = Archie.Load(@"
key:value
extra
:endskip
");
            var expected = JObject.Parse("{'key': 'value'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine11OrdinaryTextStartingWithColonIncludedInMultiline() {
            var result = Archie.Load(@"
key:value
:notacommand
:end
");
            var expected = JObject.Parse("{'key': 'value\n:notacommand'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine12ParsesEndCommandWithExtraneousTextAndWhitespace() {
            var result = Archie.Load(@"
key:value
extra
:end this
");
            var expected = JObject.Parse("{'key': 'value\nextra'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine13ParsesEndCommandWithExtraneousTextAndTabs() {
            var result = Archie.Load(@"
key:value
extra
:end	this 
");
            var expected = JObject.Parse("{'key': 'value\nextra'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine14DoesNotEscapeColonsOnFirstLine() {
            var result = Archie.Load(@"
key::value
:end
");
            var expected = JObject.Parse("{'key': ':value'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine15DoesNotEscapeColonsOnFirstLine() {
            var result = Archie.Load(@"
key:\:value
:end
");
            var expected = new JObject(new JProperty("key", "\\:value"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine16DoesNotAllowEscapingColonsInKeys() {
            var result = Archie.Load(@"
key:value
key2\:value
:end
");
            var expected = new JObject(new JProperty("key", "value\nkey2\\:value"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine17AllowsEscapedKeyLinesWithLeadingBackslash() {
            var result = Archie.Load(@"
key:value
\key2:value
:end
");
            var expected = new JObject(new JProperty("key", "value\nkey2:value"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine18AllowsEscapedCommandsAtBeginningOfLines() {
            var result = Archie.Load(@"
key:value
\:end
:end
");
            var expected = new JObject(new JProperty("key", "value\n:end"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine19AllowsEscapedCommandsWithExtraTextAtBeginningOfLines() {
            var result = Archie.Load(@"
key:value
\:endthis
:end
");
            var expected = new JObject(new JProperty("key", "value\n:endthis"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine20AllowsEscapingOfNonCommandsAtBeginningOfLines() {
            var result = Archie.Load(@"
key:value
\:notacommand
:end
");
            var expected = new JObject(new JProperty("key", "value\n:notacommand"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine21AllowsSimpleArrayStyleLines() {
            var result = Archie.Load(@"
key:value
* value
:end
");
            var expected = new JObject(new JProperty("key", "value\n* value"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine22EscapesAsteriskWithinMultiLineValuesWhenNotInSimpleArray() {
            var result = Archie.Load(@"
key:value
\* value
:end
");
            var expected = new JObject(new JProperty("key", "value\n* value"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine23AllowsEscapedScopeCommandsAtBeginningOfLines() {
            var result = Archie.Load(@"
key:value
\{scope}
:end
");
            var expected = new JObject(new JProperty("key", "value\n{scope}"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }

        // NOTE: Don't ask me what happened to tests 24-25

        [TestMethod]
        public void MultiLine26ArraysBreakMultiLineValues() {
            var result = Archie.Load(@"
key:value
text
[array]
more text
:end
");
            var expected = JObject.Parse("{'key': 'value', 'array': []}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine27ObjectsBreakMultiLineValues() {
            var result = Archie.Load(@"
key:value
text
{scope}
more text
:end
");
            var expected = JObject.Parse("{'key': 'value', 'scope': {}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine28BulletsNoNotBreakMultiLineValues() {
            var result = Archie.Load(@"
key:value
text
* value
more text
:end
");
            var expected = new JObject(new JProperty("key", "value\ntext\n* value\nmore text"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine29SkipsBreakMultiLineValues() {
            var result = Archie.Load(@"
key:value
text
:skip
:endskip
more text
:end
");
            var expected = JObject.Parse("{'key': 'value'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine30EscapedInitialBackslashIsIncluded() {
            var result = Archie.Load(@"
key:value
\\:end
:end
");
            var expected = new JObject(new JProperty("key", "value\n\\:end"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine31OnlyFirstBackslashEscaped() {
            var result = Archie.Load(@"
key:value
\\\:end
:end
");
            var expected = new JObject(new JProperty("key", "value\n\\\\:end"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine32MultipleLinesMayBeEscaped() {
            var result = Archie.Load(@"
key:value
\:end
\:ignore
\:endskip
\:skip
:end
");
            var expected = new JObject(new JProperty("key", "value\n:end\n:ignore\n:endskip\n:skip"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void MultiLine33DoesNotEscapeColons() {
            var result = Archie.Load(@"
key:value
Lorem key2\\:value
:end
");
            var expected = new JObject(new JProperty("key", "value\nLorem key2\\\\:value"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }


    }
}
