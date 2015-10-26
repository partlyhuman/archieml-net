using System;
using ArchieML;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace ArchieML_Tests {
    [TestClass]
    public class NestedArrayTests {
        [TestMethod]
        public void ArraysNested01ArraysBeginningWithDotsCreateComplexSubarrays() {
            var result = Archie.Load(@"
[array]
[.subarray]
key: value
");
            var expected = JObject.Parse(@"{'array': [{'subarray': [{'key': 'value'}]}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysNested02ArrayKeysBeginningWithDotsCreateSimpleSubarrays() {
            var result = Archie.Load(@"
[array]
[.subarray]
* Value 1
* Value 2
");
            var expected = JObject.Parse(@"{'array': [{'subarray': ['Value 1', 'Value 2']}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysNested03SubarraysCanContainMultipleComplexValues() {
            var result = Archie.Load(@"
[array]
[.subarray]
key: value
key: value
");
            var expected = JObject.Parse(@"{'array': [{'subarray': [{'key': 'value'}, {'key': 'value'}]}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysNested04SubarraysCanContainObjectsWithMultipleKeys() {
            var result = Archie.Load(@"
[array]
[.subarray]
key1: value
key2: value
key1: value
key2: value
");
            var expected = JObject.Parse(@"{'array': [{'subarray': [{'key1': 'value', 'key2': 'value'}, {'key1': 'value', 'key2': 'value'}]}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysNested05SubarraysCanBeClosedToReturnToParentScope() {
            var result = Archie.Load(@"
[array]
[.subarray]
subkey: value
[]
parentkey: value
");
            var expected = JObject.Parse(@"{'array': [{'parentkey': 'value', 'subarray': [{'subkey': 'value'}]}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysNested06SubarraysCanCoexistWithObjectArrayKeys() {
            var result = Archie.Load(@"
[array]
parentkey: value
[.subarray]
subkey: value");
            var expected = JObject.Parse(@"{'array': [{'parentkey': 'value', 'subarray': [{'subkey': 'value'}]}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysNested07SubarraysDoNotAffectParentTrackingArrayItemDelimiterKey() {
            var result = Archie.Load(@"
[array]
key: value
[.subarray]
subkey: value
[]

key: value
");
            var expected = JObject.Parse(@"{'array': [{'key': 'value', 'subarray': [{'subkey': 'value'}]}, {'key': 'value'}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysNested08SubarraysDoNotAffectParentTrackingArrayItemDelimiterKey() {
            var result = Archie.Load(@"
[array]
key: value
[.subarray]
subkey: value
[]
key: value
");
            var expected = JObject.Parse(@"{'array': [{'key': 'value', 'subarray': [{'subkey': 'value'}]}, {'key': 'value'}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysNested09SubarraysCanServeAsItemDelimiterKey() {
            var result = Archie.Load(@"
[array]
[.subarray]
[]
[.subarray]
[]
");
            var expected = JObject.Parse(@"{'array': [{'subarray': []}, {'subarray': []}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysNested10SubarraysCanContainComplexSubarrays() {
            var result = Archie.Load(@"
[array]
[.subarray]
[.subsubarray]
key1: Value 1
key2: Value 2
[]
[]
");
            var expected = JObject.Parse(@"{'array': [{'subarray': [{'subsubarray': [{'key1': 'Value 1', 'key2': 'Value 2'}]}]}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysNested11SubarraysCanContainSimpleSubarrays() {
            var result = Archie.Load(@"
[array]
[.subarray]
[.subsubarray]
* Value 1
* Value 2
[]
[]");
            var expected = JObject.Parse(@"{'array': [{'subarray': [{'subsubarray': ['Value 1', 'Value 2']}]}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void ArraysNested12SubarraysActAsTopLevelArraysWhenNotAlreadyInsideArray() {
            var result = Archie.Load(@"
[.subarray]
key: value
[]");
            var expected = JObject.Parse(@"{'subarray': [{'key': 'value'}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
    }
}
