namespace PTI.Rs232Validator.Tests
{
    using System;
    using System.Linq;
    using Emulator;
    using NUnit.Framework;

    public class Integration
    {
        [Test]
        public void SinglePollTest()
        {
            // Setup
            var runner = new EmulationRunner<ApexEmulator>(TimeSpan.FromMilliseconds(1));
            var loopCount = 5;

            // Execute
            var emulator = runner.RunIdleFor(loopCount);

            // Assert
            Assert.GreaterOrEqual(emulator.TotalPollCount, loopCount);
        }

        [Test]
        public void CreditSequenceTest()
        {
            // Setup
            const int pollsBetween = 5;
            const int creditCount = 10;
            const byte creditIndex = 1;
            var runner = new EmulationRunner<ApexEmulator>(TimeSpan.FromMilliseconds(1));
            var expected = Enumerable.Repeat(creditIndex, creditCount).ToList();

            // Execute
            var emulator = runner.CreditEveryNLoops(pollsBetween, creditCount, creditIndex);

            // Assert
            Assert.AreEqual(expected, emulator.IssueCredits.ToList());
        }
    }
}