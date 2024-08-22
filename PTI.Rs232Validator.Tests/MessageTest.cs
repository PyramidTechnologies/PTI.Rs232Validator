namespace PTI.Rs232Validator.Tests
{
    using Messages;
    using NUnit.Framework;

    public class MessageTest
    {
        /// <summary>
        ///     Ensure that host messages are detected
        /// </summary>
        [Test]
        public void HostParseTest()
        {
            // Setup
            var raw = new byte[] {0x02, 0x08, 0x10, 0x7F, 0x00, 0x00, 0x03, 0x67};

            // Execute
            var message = new TestMessage(raw);

            // Assert
            Assert.True(message.IsHostMessage);
        }

        /// <summary>
        ///     Ensure that host messages are detected
        /// </summary>
        [Test]
        public void NotHostParseTest()
        {
            // Setup
            var raw = new byte[]
            {
                0x02, 0x0B, 0x20, 0x01, 0x10, 0x00, 0x00, 0x12, 0x13, 0x03, 0x3B
            };

            // Execute
            var message = new TestMessage(raw);

            // Assert
            Assert.False(message.IsHostMessage);
        }

        /// <summary>
        ///     Detect ACK message
        /// </summary>
        [Test]
        public void IsAckTest()
        {
            // Setup
            var raw = new byte[] {0x02, 0x08, 0x11, 0x7F, 0x00, 0x00, 0x03, 0x68};

            // Execute
            var message = new TestMessage(raw);

            // Assert
            Assert.True(message.Ack);
        }

        /// <summary>
        ///     Detect ACK message
        /// </summary>
        [Test]
        public void IsNotAckTest()
        {
            // Setup
            var raw = new byte[] {0x02, 0x08, 0x10, 0x7F, 0x00, 0x00, 0x03, 0x67};

            // Execute
            var message = new TestMessage(raw);

            // Assert
            Assert.False(message.Ack);
        }

        /// <summary>
        ///     Test that serialization works
        /// </summary>
        [Test]
        public void SerializeTest()
        {
            // Setup
            var raw = new byte[] {0x02, 0x08, 0x10, 0x7F, 0x00, 0x00, 0x03, 0x67};

            // Execute
            var message = new TestMessage(raw);

            // Assert
            Assert.AreEqual(raw, message.Serialize());
        }
    }

    /// <summary>
    ///     Concrete wrapper to test base message members
    /// </summary>
    internal class TestMessage : Rs232Message
    {
        public TestMessage(byte[] payload) : base(payload)
        {
        }
    }
}