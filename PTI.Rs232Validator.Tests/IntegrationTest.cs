namespace PTI.Rs232Validator.Tests
{
    using System;
    using Emulator;
    using NUnit.Framework;

    public class Integration
    {
        [Test]
        public void TestSinglePoll()
        {
            // Setup
            var runner = new EmulationRunner<ApexEmulator>(TimeSpan.FromMilliseconds(1));
            var loopCount = 5;

            // Execute
            var emulator = runner.RunIdleFor(loopCount);

            // Assert
            Assert.GreaterOrEqual(emulator.TotalPollCount, loopCount);
        }
    }
}