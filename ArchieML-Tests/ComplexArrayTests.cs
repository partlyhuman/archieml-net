using System;
using ArchieML;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace ArchieML_Tests {
    [TestClass]
    public class ComplexArrayTests {
        [TestMethod]
        public void ArraysComplex01KeysAfterArrayCommandAreIncludedAsItemsInArray() {
            var result = Archie.Load(@"
[array]
key:value
");
            var expected = JObject.Parse(@"{'array': [{'key': 'value'}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysComplex02ArrayItemsCanHaveMultipleKeys() {
            var result = Archie.Load(@"
[array]
key:value
second:value
");
            var expected = JObject.Parse(@"{'array': [{'key': 'value', 'second': 'value'}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysComplex03NewItemsStartedWhenFirstKeyEncounteredAgain() {
            var result = Archie.Load(@"
[array]
key:value
second:value
key:value
");
            var expected = JObject.Parse(@"{'array': [{'key': 'value', 'second': 'value'}, {'key': 'value'}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysComplex04NewItemsStartedWhenDuplicateKeysEncountered() {
            var result = Archie.Load(@"
[array]
key:first
key:second
");
            var expected = JObject.Parse(@"{'array': [{'key': 'first'}, {'key': 'second'}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysComplex05NewItemsStartedWhenDuplicateKeysEncounteredWithDotPaths() {
            var result = Archie.Load(@"
[array]
scope.key:first
scope.key:second
");
            var expected = JObject.Parse(@"{'array': [{'scope': {'key': 'first'}}, {'scope': {'key': 'second'}}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysComplex06DuplicateKeysMustMatchFullDotPathToStartNewItems() {
            var result = Archie.Load(@"
[array]
key:value
scope.key:value
");
            var expected = JObject.Parse(@"{'array': [{'key': 'value', 'scope': {'key': 'value'}}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysComplex07DuplicateKeysMustMatchFullDotPathToStartNewItems() {
            var result = Archie.Load(@"
[array]
scope.key:value
key:value
otherscope.key:value
");
            var expected = JObject.Parse(@"{'array': [{'scope': {'key': 'value'}, 'key': 'value', 'otherscope': {'key': 'value'}}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysComplex08ArraysBreakUpMultiLineValues() {
            var result = Archie.Load(@"
[array1]
key:value
[array2]
more text
:end
");
            var expected = JObject.Parse(@"{'array1': [{'key': 'value'}], 'array2': []}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysComplex09ObjectsWithinMultiLineValueBreakUpValue() {
            var result = Archie.Load(@"
[array]
key:value
{scope}
more text
:end
");
            var expected = JObject.Parse(@"{'array': [{'key': 'value'}], 'scope': {}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysComplex10MultiLineValuesCanBeAddedToArrayItems() {
            var result = Archie.Load(@"
[array]
key:value
other: value
more text
:end
");
            var expected = JObject.Parse(@"{'array': [{'key': 'value', 'other': 'value\nmore text'}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysComplex11BulletsIgnoredInObjectArrays() {
            var result = Archie.Load(@"
[array]
key:value
* value
more text
:end
");
            var expected = JObject.Parse(@"{'array': [{'key': 'value\n* value\nmore text'}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysComplex12SkipsBreakUpMultiLineValuesInArrays() {
            var result = Archie.Load(@"
[array]
key:value
:skip
:endskip
more text
:end
");
            var expected = JObject.Parse(@"{'array': [{'key': 'value'}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysComplex13RedefiningArrayReplacesExistingArray() {
            var result = Archie.Load(@"
[array]
key:value 1
[]
[array]
key:value 2
");
            var expected = JObject.Parse(@"{'array': [{'key': 'value 2'}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysComplex14ComplexArraysCanBeRedefinedAsSimpleArrays() {
            var result = Archie.Load(@"
[array]
key:value
[]
[array]
*Value
");
            var expected = JObject.Parse(@"{'array': ['Value']}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysComplex15ComplexArraysOverwriteExistingKeys() {
            var result = Archie.Load(@"
a.b:complex value
[a.b]
key:value
");
            var expected = JObject.Parse(@"{'a': {'b': [{'key': 'value'}]}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
    }
}
