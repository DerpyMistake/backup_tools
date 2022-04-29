using NUnit.Framework;
using BitEffects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace BitEffects.Tests
{
    [TestFixture]
    public class StreamSplitterTests
    {
        [Test]
        async public Task InputMatchesOutput()
        {
            byte[] inputData = Encoding.UTF8.GetBytes("ABCDEFG");
            string[] results = { "", "", "" };
            int numParts = 0;

            StreamSplitter splitter = new StreamSplitter(3, processData);
            void processData(byte[] data, int index)
            {
                numParts++;
                results[index] = Encoding.UTF8.GetString(data);
            }

            using (var ms = new MemoryStream(inputData))
            {
                await splitter.Run(ms);
            }

            Assert.AreEqual(3, numParts);
            Assert.AreEqual("ABC", results[0]);
            Assert.AreEqual("DEF", results[1]);
            Assert.AreEqual("G", results[2]);
        }
    }
}