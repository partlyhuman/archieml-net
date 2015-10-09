using System;
using ArchieML;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace ArchieML_Tests {
    [TestClass]
    public class FreeformArrayTests {
        [TestMethod]
        public void Freeform01TextLinesAreConvertedIntoObjectsWithTypeText() {
            var result = Archie.Load(@"
[+freeform]
Value
[]
");
            var expected = JObject.Parse(@"{'freeform': [{'type': 'text', 'value': 'Value'}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Freeform02KeyValueLinesAreConvertedIntoObjects() {
            var result = Archie.Load(@"
[+freeform]
name: value
[]
");
            var expected = JObject.Parse(@"{'freeform': [{'type': 'name', 'value': 'value'}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Freeform03TextAndKeyValueLinesMayBeInterspersed() {
            var result = Archie.Load(@"
[+freeform]
Value
name: value
Value
name: value
[]
");
            var expected = JObject.Parse(@"{'freeform': [{'type': 'text', 'value': 'Value'}, {'type': 'name', 'value': 'value'}, {'type': 'text', 'value': 'Value'}, {'type': 'name', 'value': 'value'}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Freeform04TextAndKeyValueLinesCanBeRepeatedOrderIsPreserved() {
            var result = Archie.Load(@"
[+freeform]
Value
Value
name: value
name: value
[]
");
            var expected = JObject.Parse(@"{'freeform': [{'type': 'text', 'value': 'Value'}, {'type': 'text', 'value': 'Value'}, {'type': 'name', 'value': 'value'}, {'type': 'name', 'value': 'value'}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Freeform05ObjectsNestedInFreeformArrays() {
            var result = Archie.Load(@"
[+freeform]
type: value
Text
{.image}
name: map.jpg
credit: Photographer
{}
[]
");
            var expected = JObject.Parse(@"{'freeform': [{'type': 'type', 'value': 'value'}, {'type': 'text', 'value': 'Text'}, {'type': 'image', 'value': {'name': 'map.jpg', 'credit': 'Photographer'}}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Freeform06SimpleArraysNestedInFreeformArrays() {
            var result = Archie.Load(@"
[+freeform]
name: value
Text
[.array]
* Value 1
* Value 2
[]
[]
");
            var expected = JObject.Parse(@"{'freeform': [{'type': 'name', 'value': 'value'}, {'type': 'text', 'value': 'Text'}, {'type': 'array', 'value': ['Value 1', 'Value 2']}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Freeform07FreeformArraysNestedInObjectArrays() {
            var result = Archie.Load(@"
[array]
name: value
[.+freeform]
name: value
Text
[]
[]
");
            var expected = JObject.Parse(@"{'array': [{'name': 'value', 'freeform': [{'type': 'name', 'value': 'value'}, {'type': 'text', 'value': 'Text'}]}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }


        [TestMethod]
        public void Freeform08WhitespaceLinesAreIgnored() {
            var result = Archie.Load(@"
[+freeform]
one
  
two
[]
");
            var expected = JObject.Parse(@"{'freeform': [{'type': 'text', 'value': 'one'}, {'type': 'text', 'value': 'two'}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Freeform09WhitespaceIsStrippedFromPlainTextLines() {
            var result = Archie.Load(@"
[+freeform]
  one  
  two  
[]
");
            var expected = JObject.Parse(@"{'freeform': [{'type': 'text', 'value': 'one'}, {'type': 'text', 'value': 'two'}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Freeform10FreeformArraysNestedInScopes() {
            var result = Archie.Load(@"
{scope}
[.+freeform]
Value
[]
{}
");
            var expected = JObject.Parse(@"{'scope': {'freeform': [{'type': 'text', 'value': 'Value'}]}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Freeform11FreeformArraysNestedInFreeformArrays() {
            var result = Archie.Load(@"
[+freeform]
[.+freeform]
Text
[]
[]
");
            var expected = JObject.Parse(@"{'freeform': [{'type': 'freeform', 'value': [{'type': 'text', 'value': 'Text'}]}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
    }
}
