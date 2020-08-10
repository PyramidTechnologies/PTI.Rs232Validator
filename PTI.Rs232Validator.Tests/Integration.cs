namespace PTI.Rs232Validator.Tests
{
    using Emulator;
    using NUnit.Framework;

    public class Integration
    {

        [Test]
        public void TestSinglePoll()
        {
            // Setup
            var loopCount = 5;
            
            // Execute
            var emulator = EmulationRunner.RunFor(loopCount);

            // Assert
            Assert.GreaterOrEqual(emulator.TotalPollCount, loopCount);
        }
    }
}