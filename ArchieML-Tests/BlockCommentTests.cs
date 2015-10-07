using System;
using ArchieML;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace ArchieML_Tests {
    [TestClass]
    public class BlockCommentTests {
        [TestMethod]
        public void Skip00IgnoresSpacesAroundSkip() {
            var result = Archie.Load(@"
  :skip  
key:value
:endskip
");
            var expected = new JObject();
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Skip01IgnoresTabsAroundSkip() {
            var result = Archie.Load(@"
        :skip       
key:value
:endskip
");
            var expected = new JObject();
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Skip02IgnoresSpacesAroundEndSkip() {
            var result = Archie.Load(@"
:skip
key:value
  :endskip  
");
            var expected = new JObject();
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Skip03IgnoresTabsAroundEndSkip() {
            var result = Archie.Load(@"
:skip
key:value
		:endskip		
");
            var expected = new JObject();
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Skip04ParsingResumesAfterEndSkip() {
            var result = Archie.Load(@"
:skip
:endskip
key:value
");
            var expected = new JObject(new JProperty("key", "value"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Skip05SkipCommandsAreCaseInsensitive() {
            var result = Archie.Load(@"
:sKiP
key:value
:eNdSkIp
");
            var expected = new JObject();
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Skip06ParseSkipWithAdditionalCharacters() {
            var result = Archie.Load(@"
:skipthis
key:value
:endskip
");
            var expected = new JObject();
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Skip07IgnoresContentOnLineAfterSkipAndSpace() {
            var result = Archie.Load(@"
:skip this text  
key:value
:endskip
");
            var expected = new JObject();
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Skip08IgnoresContentOnLineAfterSkipAndTab() {
            var result = Archie.Load(@"
:skip	this text		
key:value
:endskip
");
            var expected = new JObject();
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void Skip09ParseEndskipWithAdditionalCharacters() {
            var result = Archie.Load(@"
:skip
:endskiptheabove
key:value
");
            var expected = new JObject(new JProperty("key", "value"));
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
    }
}
