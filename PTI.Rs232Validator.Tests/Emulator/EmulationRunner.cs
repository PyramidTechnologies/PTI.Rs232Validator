namespace PTI.Rs232Validator.Tests.Emulator
{
    using System;
    using System.Threading;

    public static class EmulationRunner
    {
        private static readonly TimeSpan TestPollingPeriod = TimeSpan.FromMilliseconds(1);
        
        public static ApexEmulator RunFor(int loops)
        {
            var emulator = new ApexEmulator();
            var validator = new ApexValidator(new Rs232Config(emulator) {PollingPeriod = TestPollingPeriod});
            
            validator.StartPollingLoop();

            while (emulator.TotalPollCount < loops)
            {
                Thread.Sleep(TestPollingPeriod);
            }

            validator.StopPollingLoop();

            return emulator;
        }
    }
}