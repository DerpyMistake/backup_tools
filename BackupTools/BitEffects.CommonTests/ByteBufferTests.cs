using NUnit.Framework;
using BitEffects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BitEffects.Tests
{
    [TestFixture]
    public class ByteBufferTests
    {
        [Test]
        public void InputMatchesOutput()
        {
            ByteBuffer buffer = new ByteBuffer(64);

            buffer.Write(Encoding.UTF8.GetBytes("ABC"), 3);
            buffer.Write(Encoding.UTF8.GetBytes("DEF"), 3);
            buffer.Write(Encoding.UTF8.GetBytes("G"), 1);
            string result = Encoding.UTF8.GetString(buffer.Read(7));

            Assert.AreEqual("ABCDEFG", result);
        }
    }
}