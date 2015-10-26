using System;
using ArchieML;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;

namespace ArchieML_Tests {
    [TestClass]
    public class WebTests {
        [TestMethod]
        public void TestSimpleUrlLoad() {
            var result = Archie.LoadUrl("https://raw.githubusercontent.com/newsdev/archieml.org/gh-pages/test/1.0/values.1.aml");
            var expected = JObject.Parse("{'test': 'Parses key value pairs','result': '{\"key\": \"value\"}','key': 'value'}");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void TestGDocLoad() {
            var result = Archie.LoadPublicGoogleDoc("https://docs.google.com/document/d/1Lu7Vv4s2UIlVfBa2aBh3Q1_kUB6SyjkOJpgqFNEOS_o/edit?usp=sharing");
            var expected = JObject.Parse("{'headline': 'Bait and Switch, a Common Ploy of Patriots and Seahawks', 'leadin': 'Lead in copy', 'sections': [{'kicker': 'New England Patriots', 'hed': 'Patriots vs. Ravens, Jan. 10', 'intro': 'Intro copy', 'video': 'link', 'image': 'c-pats'}, {'kicker': 'Seattle Seahawks', 'hed': 'Seahawks vs. Eagles, Dec. 7', 'intro': 'Intro copy', 'video': 'link', 'image': 'c-seahawks'} ] }");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        public void TestGDocLoadById() {
            var result = Archie.LoadPublicGoogleDoc("1Lu7Vv4s2UIlVfBa2aBh3Q1_kUB6SyjkOJpgqFNEOS_o");
            var expected = JObject.Parse("{'headline': 'Bait and Switch, a Common Ploy of Patriots and Seahawks', 'leadin': 'Lead in copy', 'sections': [{'kicker': 'New England Patriots', 'hed': 'Patriots vs. Ravens, Jan. 10', 'intro': 'Intro copy', 'video': 'link', 'image': 'c-pats'}, {'kicker': 'Seattle Seahawks', 'hed': 'Seahawks vs. Eagles, Dec. 7', 'intro': 'Intro copy', 'video': 'link', 'image': 'c-seahawks'} ] }");
            Assert.IsTrue(JToken.DeepEquals(result, expected));
        }
        [TestMethod]
        [ExpectedException(typeof(System.Net.WebException))]
        public void TestGDocLoadUrlWithoutDocumentId() {
            Archie.LoadPublicGoogleDoc("http://google.com/");
            Assert.Fail();
        }

    }
}
