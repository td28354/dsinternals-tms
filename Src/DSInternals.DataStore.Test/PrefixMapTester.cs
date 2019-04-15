using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using DSInternals.Common;

namespace DSInternals.DataStore.Test
{
    [TestClass]
    public class PrefixMapTester
    {
        // Sample prefix map after Exchange 2016 schema import.
        private static byte[] ExchangeBinaryPrefixMap = "060000006200000023480b002a864886f7140104b658664f310c002a864886f7140104b658668331090c002a864886f7140105b6583e83dc600a002a864886f714010614014b6d0a002a864886f714010614024f750b002a864886f7140106140183".HexToBinary();

        // Sample prefix map provided by Christoffer Andersson. Contains duplicate prefixes, which is a little bit strange.
        private static byte[] SamplePrefixMap = "0E000000D800000023480B002A864886F7140104B65866BE180B002A864886F7140105B6583E84670C002A864886F7140104B6586683E14A0C002A864886F7140104B65866816C3D0C002A864886F7140105B6583E81D62C0C002A864886F7140105B6583E83AE720A002A864886F7140106140152690A002A864886F71401061402905F0A002A864886F7140106180249160A002A864886F71401061801F16D0B002A864886F71401061D0201F15A0B002A864886F71401061D0202BB410B002A864886F7140106140183E92609002A864886F714010283".HexToBinary();

        [TestMethod]
        public void PrefixMap_Vector1()
        {
            var map = new PrefixMap(ExchangeBinaryPrefixMap);

            // Should contain 39 builtin prefixes + 6 new
            Assert.AreEqual(45, map.Count);

            // Test one of the decoded prefixes
            bool contains1 = map.ContainsPrefix(18467);
            Assert.AreEqual(true, contains1);
            
            string prefix = map[18467];
            Assert.AreEqual("1.2.840.113556.1.4.7000.102", prefix);

            // Test non-existing prefix
            bool contains2 = map.ContainsPrefix(1234);
            Assert.AreEqual(false, contains2);
        }

        [TestMethod]
        public void PrefixMap_Vector2()
        {
            var map = new PrefixMap(SamplePrefixMap);

            // Should contain 39 builtin refixes + 14 additional
            Assert.AreEqual(53, map.Count);
        }

        [TestMethod]
        public void PrefixMap_TranstaleBuiltin()
        {
            var map = new PrefixMap();

            // givenName attribute
            string oid = map.Translate(42);
            Assert.AreEqual("2.5.4.42", oid);

            // objectSid attribute
            oid = map.Translate(589970);
            Assert.AreEqual("1.2.840.113556.1.4.146", oid);

            // searchFlags attribute
            oid = map.Translate(131406);
            Assert.AreEqual("1.2.840.113556.1.2.334", oid);

            // Entry-TTL attribute
            oid = map.Translate(1769475);
            Assert.AreEqual("1.3.6.1.4.1.1466.101.119.3", oid);

            /* The following examples are taken from https://docs.microsoft.com/en-us/openspecs/windows_protocols/ms-drsr/6f53317f-2263-48ee-86c1-4580bf97232c */

            // countryName attribute
            oid = map.Translate(0x00000006);
            Assert.AreEqual("2.5.4.6", oid);

            // country class
            oid = map.Translate(0x00010002);
            Assert.AreEqual("2.5.6.2", oid);

            // instanceType attribute
            oid = map.Translate(0x00020001);
            Assert.AreEqual("1.2.840.113556.1.2.1", oid);

            // container class
            oid = map.Translate(0x00030017);
            Assert.AreEqual("1.2.840.113556.1.3.23", oid);

            // attribute syntax: distinguished name
            oid = map.Translate(0x00080001);
            Assert.AreEqual("2.5.5.1", oid);

            // RDN attribute
            oid = map.Translate(0x00090001);
            Assert.AreEqual("1.2.840.113556.1.4.1", oid);

            // securityObject class
            oid = map.Translate(0x000a0001);
            Assert.AreEqual("1.2.840.113556.1.5.1", oid);

            // uid attribute
            oid = map.Translate(0x00150001);
            Assert.AreEqual("0.9.2342.19200300.100.1.1", oid);

            // carLicense attribute
            oid = map.Translate(0x00160001);
            Assert.AreEqual("2.16.840.1.113730.3.1.1", oid);

            // crossRefContainer class
            oid = map.Translate(0x00170035);
            Assert.AreEqual("1.2.840.113556.1.5.7000.53", oid);

            // ditContentRules attribute
            oid = map.Translate(0x00180002);
            Assert.AreEqual("2.5.21.2", oid);

            // createTimeStamp attribute
            oid = map.Translate(0x00190001);
            Assert.AreEqual("2.5.18.1", oid);

            // subSchema class
            oid = map.Translate(0x001a0001);
            Assert.AreEqual("2.5.20.1", oid);
        }

    [TestMethod]
        public void PrefixMap_TranstaleUser()
        {
            var map = new PrefixMap(ExchangeBinaryPrefixMap);

            // ms-Exch-Admins
            string oid = map.Translate(827294608);
            Assert.AreEqual("1.2.840.113556.1.4.7000.102.50064", oid);

            // ms-Exch-Folder-Affinity-List
            oid = map.Translate(1210264401);
            Assert.AreEqual("1.2.840.113556.1.4.7000.102.11089", oid);
        }


        [TestMethod]
        public void PrefixMap_NullInput()
        {
            byte[] binaryPrefixMap = null;
            var map = new PrefixMap(binaryPrefixMap);
            
            // Should only contain 39 builtin prefixes
            Assert.AreEqual(39, map.Count);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void PrefixMap_EmptyInput()
        {
            byte[] binaryPrefixMap = new byte[0];
            var map = new PrefixMap(binaryPrefixMap);
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void PrefixMap_InvalidInput1()
        {
            byte[] binaryPrefixMap = { 1, 2, 3, 4, 5 };
            var map = new PrefixMap(binaryPrefixMap);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void PrefixMap_InvalidInput2()
        {
            byte[] binaryPrefixMap = { 1, 2, 3, 4, 5, 6, 7, 8 };
            var map = new PrefixMap(binaryPrefixMap);
        }
    }
}
