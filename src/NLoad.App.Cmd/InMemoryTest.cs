using System;
using System.Threading;

namespace NLoad.App.Cmd
{
    internal sealed class InMemoryTest : ITest
    {
        public void Initialize()
        {
            //Console.WriteLine("Initialize test...");
        }

        public TestResult Execute()
        {
            Thread.Sleep(1);

            return new TestResult(true);
        }
    }
}