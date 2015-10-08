using System;
using ArchieML;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace ArchieML_Tests {
    [TestClass]
    public class ArrayTests {
        [TestMethod]
        public void Arrays01ArrayCommandCreatesEmptyArray() {
            var result = Archie.Load(@"[array]");
            var expected = JObject.Parse(@"{'array': []}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Arrays02IgnoresSpacesAroundArrayCommand() {
            var result = Archie.Load(@"  [array]  ");
            var expected = JObject.Parse(@"{'array': []}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Arrays03IgnoresTabsAroundArrayCommand() {
            var result = Archie.Load(@"     [array]     ");
            var expected = JObject.Parse(@"{'array': []}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Arrays04IgnoresSpacesAroundArrayName() {
            var result = Archie.Load(@"[  array  ]");
            var expected = JObject.Parse(@"{'array': []}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Arrays05IgnoresTabsAroundArrayName() {
            var result = Archie.Load(@"[        array     ]");
            var expected = JObject.Parse(@"{'array': []}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Arrays06IgnoresTextAfterArrayCommand() {
            var result = Archie.Load(@"[array]a");
            var expected = JObject.Parse(@"{'array': []}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Arrays07ArraysCanBeNestedUsingDotNotation() {
            var result = Archie.Load(@"[scope.array]");
            var expected = JObject.Parse(@"{'scope': {'array': []}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Arrays08ArrayValuesCanBeNestedUsingDotNotation() {
            var result = Archie.Load(@"
[array]
scope.key: value
scope.key: value
");
            var expected = JObject.Parse(@"{'array': [{'scope': {'key': 'value'}}, {'scope': {'key': 'value'}}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Arrays09EndArrayCommandResetsToGlobalScope() {
            var result = Archie.Load(@"
[array]
[]
key:value
");
            var expected = JObject.Parse(@"{'array': [], 'key': 'value'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Arrays10IgnoreSpacesInsideEndArrayCommand() {
            var result = Archie.Load(@"
[array]
[  ]
key:value
");
            var expected = JObject.Parse(@"{'array': [], 'key': 'value'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Arrays11IgnoreTabsInsideEndArrayCommand() {
            var result = Archie.Load(@"
[array]
[       ]
key:value
");
            var expected = JObject.Parse(@"{'array': [], 'key': 'value'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Arrays12IgnoreSpacesAroundEndArrayCommand() {
            var result = Archie.Load(@"
[array]
  []  
key:value
");
            var expected = JObject.Parse(@"{'array': [], 'key': 'value'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Arrays13IgnoreTabsAroundEndArrayCommand() {
            var result = Archie.Load(@"
[array]
		[]		
key:value
");
            var expected = JObject.Parse(@"{'array': [], 'key': 'value'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Arrays14EmptyObjectClosesArray() {
            var result = Archie.Load(@"
[array]
{}
topkey: value
");
            var expected = JObject.Parse(@"{'array': [], 'topkey': 'value'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }

        [TestMethod]
        public void ArraysSimpleKeyValueProcessing() {
            var result = Archie.Load(@"
[array]
a: 1
b: 2
c: 3
a: 10
b: 20
c: 30
");
            var expected = JObject.Parse(@"{'array': [{'a': '1', 'b': '2', 'c': '3'}, {'a': '10', 'b': '20', 'c': '30'}]}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
    }
}
