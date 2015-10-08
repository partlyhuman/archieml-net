using System;
using ArchieML;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace ArchieML_Tests {
    [TestClass]
    public class SimpleArrayTests {
        [TestMethod]
        public void ArraysSimple01AsteriskCreatesSimpleArray() {
            var result = Archie.Load(@"
[array]
*Value
");
            var expected = JObject.Parse(@"{'array': ['Value']}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysSimple02IgnoresSpacesSurroundingAsterisk() {
            var result = Archie.Load(@"
[array]
  *  Value
");
            var expected = JObject.Parse(@"{'array': ['Value']}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysSimple03IgnoresTabsSurroundingAsterisk() {
            var result = Archie.Load(@"
[array]
		*		Value
");
            var expected = JObject.Parse(@"{'array': ['Value']}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysSimple04MultipleValues() {
            var result = Archie.Load(@"
[array]
* Value 1
* Value 2
");
            var expected = JObject.Parse(@"{'array': ['Value 1', 'Value 2']}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysSimple05IgnoresPlainTextLinesBetweenArrayItems() {
            var result = Archie.Load(@"
[array]
* Value 1
Non-element
* Value 2
");
            var expected = JObject.Parse(@"{'array': ['Value 1', 'Value 2']}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysSimple06IgnoresKeyValueLinesBetweenArrayItems() {
            var result = Archie.Load(@"
[array]
* Value 1
key:value
* Value 2
");
            var expected = JObject.Parse(@"{'array': ['Value 1', 'Value 2']}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysSimple07ParsesKeyValueLinesAfterEndArray() {
            var result = Archie.Load(@"
[array]
* Value 1
[]
key:value
");
            var expected = JObject.Parse(@"{'array': ['Value 1'], 'key': 'value'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysSimple08MultiLineValuesAllowed() {
            var result = Archie.Load(@"
[array]
* Value 1
extra
:end
");
            var expected = JObject.Parse(@"{'array': ['Value 1\nextra']}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysSimple09EscapedAsterisksAreTreatedAsPlainText() {
            var result = Archie.Load(@"
[array]
* Value 1
\* extra
:end
");
            var expected = JObject.Parse(@"{'array': ['Value 1\n* extra']}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysSimple10EscapedCommandKeysAreTreatedAsPlainText() {
            var result = Archie.Load(@"

[array]
*Value1
\:end
:end
");
            var expected = JObject.Parse(@"{'array': ['Value1\n:end']}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysSimple11EscapedColonsAreNotTreatedAsKeyValueLines() {
            var result = Archie.Load(@"
[array]
*Value1
key\:value
:end
");
            var expected = JObject.Parse(@"{'array': ['Value1\nkey\\:value']}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysSimple12EscapedKeyValueLinesAreTreatedAsPlainText() {
            var result = Archie.Load(@"
[array]
*Value
\key:value
:end
");
            var expected = JObject.Parse(@"{'array': ['Value\nkey:value']}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysSimple13EscapedColonsInNonKeyValueLinesAreTreatedAsPlainText() {
            var result = Archie.Load(@"
[array]
* Value 1
word key\:value
:end
");
            var expected = JObject.Parse(@"{'array': ['Value 1\nword key\\:value']}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysSimple14SimpleArraysBreakMultiLineValues() {
            var result = Archie.Load(@"
[array1]
* value
[array2]
more text
:end
");
            var expected = JObject.Parse(@"{'array1': ['value'], 'array2': []}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysSimple15ScopeCommandsBreakMultiLineValues() {
            var result = Archie.Load(@"
[array]
* value
{scope}
more text
:end
");
            var expected = JObject.Parse(@"{'array': ['value'], 'scope': {}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysSimple16KeyValuesLinesAreIgnoredAndIncludedInMultiLineValues() {
            var result = Archie.Load(@"
[array]
* value
key: value
more text
:end
");
            var expected = JObject.Parse(@"{'array': ['value\nkey: value\nmore text']}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysSimple17BulletsBeginNewMultiLineValues() {
            var result = Archie.Load(@"
[array]
* value
* value
more text
:end
");
            var expected = JObject.Parse(@"{'array': ['value', 'value\nmore text']}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysSimple18SkipsBreakUpMultiLineValues() {
            var result = Archie.Load(@"
[array]
* value
:skip
:endskip
more text
:end
");
            var expected = JObject.Parse(@"{'array': ['value']}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysSimple19RedeclaringSimpleArraysReplaceExistingArrays() {
            var result = Archie.Load(@"
[array]
* Value 1
[]

[array]
* Value 2
[]
");
            var expected = JObject.Parse(@"{'array': ['Value 2']}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysSimple20SimpleArraysCanBeRedeclaredAsComplexArrays() {
            var result = Archie.Load(@"
[array]
*Value
[]
[array]
key:value
");
            var expected = JObject.Parse(@"{'array': [{'key': 'value'}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysSimple21SimpleArraysCanOverwriteExistingValues() {
            var result = Archie.Load(@"
a.b: complex value
[a.b]
* simple value
");
            var expected = JObject.Parse(@"{'a': {'b': ['simple value']}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
    }
}
