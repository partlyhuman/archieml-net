using System;
using ArchieML;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace ArchieML_Tests {
    [TestClass]
    public class KeyTests {
        [TestMethod]
        public void Keys1LettersNumbersDashesUnderscoresAllowed() {
            var result = Archie.Load(@"a-_1: value");
            var expected = JObject.Parse(@"{'a-_1': 'value'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Keys2SpacesNotAllowed() {
            var result = Archie.Load(@"k ey:value");
            var expected = new JObject();
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Keys3SymbolsNotAllowed() {
            var result = Archie.Load(@"k&ey:value");
            var expected = new JObject();
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Keys4CanBeNestedUsingDotNotation() {
            var result = Archie.Load(@"scope.key:value");
            var expected = JObject.Parse(@"{'scope': {'key': 'value'}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Keys5EarlierKeysWithinScopesArentDeleted() {
            var result = Archie.Load(@"
scope.key:value
scope.otherkey:value
            ");
            var expected = JObject.Parse(@"{'scope': {'key': 'value', 'otherkey': 'value'}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Keys6ValuesAreConvertedBetweenObjectsAndStrings() {
            var result = Archie.Load(@"
string_to_object.scope: value
string_to_object.scope.scope: value

object_to_string.scope.scope: value
object_to_string.scope: value
            ");
            var expected = JObject.Parse(@"{'string_to_object': {'scope': {'scope': 'value'}}, 'object_to_string': {'scope': 'value'}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
    }
}
