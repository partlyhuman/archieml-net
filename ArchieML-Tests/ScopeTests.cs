using System;
using ArchieML;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace ArchieML_Tests {
    [TestClass]
    public partial class ScopeTests {
        [TestMethod]
        public void Scopes01NewScope() {
            var result = Archie.Load(@"
{scope}
");
            var expected = JObject.Parse(@"{'scope': {}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }

        [TestMethod]
        public void Scopes02IgnoresSpaces() {
            var result = Archie.Load(@"  {scope}  ");
            var expected = JObject.Parse(@"{'scope': {}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }

        [TestMethod]
        public void Scopes03IgnoresTabs() {
            var result = Archie.Load(@"         {scope}    ");
            var expected = JObject.Parse(@"{'scope': {}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }

        [TestMethod]
        public void Scopes04IgnoresSpacesInKey() {
            var result = Archie.Load(@"{  scope  }");
            var expected = JObject.Parse(@"{'scope': {}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Scopes05IgnoresTabsInKey() {
            var result = Archie.Load(@"{        scope     }");
            var expected = JObject.Parse(@"{'scope': {}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Scopes06IgnoresTextAfterScopeCommand() {
            var result = Archie.Load(@"{scope}a");
            var expected = JObject.Parse(@"{'scope': {}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Scopes07ItemsBeforeScopeCommandNotNamespaced() {
            var result = Archie.Load(@"
key:value
{scope}
");
            var expected = JObject.Parse(@"{'key': 'value', 'scope': {}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Scopes08ItemsAfterScopeCommandAreNamespaced() {
            var result = Archie.Load(@"
{scope}
key:value
");
            var expected = JObject.Parse(@"{'scope': {'key': 'value'}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Scopes09ScopesCanBeNestedWithDotNotation() {
            var result = Archie.Load(@"
{scope.scope}
key:value
");
            var expected = JObject.Parse(@"{'scope': {'scope': {'key': 'value'}}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Scopes10ScopesCanBeReopened() {
            var result = Archie.Load(@"
{scope}
key:value
{}
{scope}
other:value
");
            var expected = JObject.Parse(@"{'scope': {'key': 'value', 'other': 'value'}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Scopes11ScopesDoNotOverwriteExistingValues() {
            var result = Archie.Load(@"
{scope.scope}
key:value
{scope.otherscope}
key:value
");
            var expected = JObject.Parse(@"{'scope': {'scope': {'key': 'value'}, 'otherscope': {'key': 'value'}}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Scopes12EndScopeResetsToGlobalScope() {
            var result = Archie.Load(@"
{scope}
{}
key:value
");
            var expected = JObject.Parse(@"{'scope': {}, 'key': 'value'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Scopes13IgnoreSpacesInsideEndScopeBrackets() {
            var result = Archie.Load(@"
{scope}
{  }
key:value
");
            var expected = JObject.Parse(@"{'scope': {}, 'key': 'value'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Scopes14IgnoreTabsInsideEndScopeBrackets() {
            var result = Archie.Load(@"
{scope}
{       }
key:value
");
            var expected = JObject.Parse(@"{'scope': {}, 'key': 'value'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Scopes15IgnoreSpacesSurroundingEndScope() {
            var result = Archie.Load(@"
{scope}
  {}  
key:value
");
            var expected = JObject.Parse(@"{'scope': {}, 'key': 'value'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Scopes16IgnoreTabsSurroundingEndScope() {
            var result = Archie.Load(@"
{scope}
    {}  
key:value
");
            var expected = JObject.Parse(@"{'scope': {}, 'key': 'value'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Scopes17KeysCanBeOverwrittenAsNamespacesByScope() {
            var result = Archie.Load(@"
key: value
{key}
subkey: subvalue
");
            var expected = JObject.Parse(@"{'key': {'subkey': 'subvalue'}}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }

    }
}
