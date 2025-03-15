using System.Text;
using KMPAccounting.Objects.BookKeeping;
using KMPAccounting.Objects.Serialization;
using Microsoft.Testing.Platform.Extensions;

namespace KMPAccounting.Testing
{
    [TestClass]
    public sealed class SerializationTests
    {
        [TestMethod]
        public void TestSimpleTransactionEmptyRemarksUnindented()
        {
            TestSimpleTransaction("", false);
        }

        [TestMethod]
        public void TestSimpleTransactionEmptyRemarksIndented()
        {
            TestCompositeTransaction("", true);
        }

        [TestMethod]
        public void TestSimpleTransactionSingleLineRemarksUnindented()
        {
            TestSimpleTransaction("Single line", false);
        }

        [TestMethod]
        public void TestSimpleTransactionSingleLineRemarksIndented()
        {
            TestSimpleTransaction("Single line", true);
        }

        [TestMethod]
        public void TestSimpleTransactionMultiLineRemarksUnindented()
        {
            TestSimpleTransaction("This is remark line 1.\nThis is the 2nd line.", false);
        }

        [TestMethod]
        public void TestSimpleTransactionMultiLine2RemarksUnindented()
        {
            TestSimpleTransaction("This is remark line 1.\nThis is the 2nd line.\n", false);
        }

        [TestMethod]
        public void TestSimpleTransactionMultiLineRemarksIndented()
        {
            TestSimpleTransaction("This is remark line 1.\nThis is the 2nd line.", true);
        }

        [TestMethod]
        public void TestSimpleTransactionMultiLine2RemarksIndented()
        {
            TestSimpleTransaction("This is remark line 1.\nThis is the 2nd line.\n", true);
        }

        [TestMethod]
        public void TestCompositeTransactionNullRemarksUnindented()
        {
            TestCompositeTransaction(null, false);
        }

        [TestMethod]
        public void TestCompositeTransactionNullRemarksIndented()
        {
            TestCompositeTransaction(null, true);
        }

        [TestMethod]
        public void TestCompositeTransactionEmptyRemarksUnindented()
        {
            TestCompositeTransaction("", false);
        }

        [TestMethod]
        public void TestCompositeTransactionEmptyRemarksIndented()
        {
            TestCompositeTransaction("", true);
        }

        [TestMethod]
        public void TestCompositeTransactionSingleLineRemarksUnindented()
        {
            TestCompositeTransaction("Single line", false);
        }

        [TestMethod]
        public void TestCompositeTransactionSingleLineRemarksIndented()
        {
            TestCompositeTransaction("Single line", true);
        }

        [TestMethod]
        public void TestCompositeTransactionMultiLineRemarksUnindented()
        {
            TestCompositeTransaction("This is remark line 1.\nThis is the 2nd line.", false);
        }

        [TestMethod]
        public void TestCompositeTransactionMultiLine2RemarksUnindented()
        {
            TestCompositeTransaction("This is remark line 1.\nThis is the 2nd line.\n", false);
        }

        [TestMethod]
        public void TestCompositeTransactionMultiLineRemarksIndented()
        {
            TestCompositeTransaction("This is remark line 1.\nThis is the 2nd line.", true);
        }

        [TestMethod]
        public void TestCompositeTransactionMultiLine2RemarksIndented()
        {
            TestCompositeTransaction("This is remark line 1.\nThis is the 2nd line.\n", true);
        }

        private void TestSimpleTransaction(string remarks, bool indented)
        {
            var date = DateTime.Now;
            var debitAccount = "Asset.Cash";
            var creditAccount = "Equity";

            var toSave = KMPAccounting.Objects.AccountHelper.CreateTransaction(date,
                debitAccount, creditAccount, 2000m, remarks);

            var sb = new StringBuilder();
            toSave.Serialize(sb, indented);

            var str = sb.ToString();

            TextReader tr = new StringReader(str);
            var lr = new LineLoader(tr);
            var toLoad = EntryDeserializationFactory.Deserialize(lr, indented);

            Assert.AreEqual(toSave, toLoad);
        }

        private void TestCompositeTransaction(string? remarks, bool indented)
        {
            var date = DateTime.Now;
            var debitAccount1 = "Asset.Cash";
            var debitAccount2 = "Asset.Saving";
            var creditAccount1 = "Equity";
            var creditAccount2 = "Liability.CreditCard";

            var toSave = KMPAccounting.Objects.AccountHelper.CreateTransaction(date,
                [(debitAccount1, 1000m), (debitAccount2, 3000m)],
                [(creditAccount1, 2000m), (creditAccount2, 2000m)],
                remarks);

            var sb = new StringBuilder();
            toSave.Serialize(sb, indented);

            var str = sb.ToString();

            TextReader tr = new StringReader(str);
            var lr = new LineLoader(tr);
            var toLoad = EntryDeserializationFactory.Deserialize(lr, indented);

            Assert.AreEqual(toSave, toLoad);
        }
    }
}
