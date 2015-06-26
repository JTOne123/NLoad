﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;

namespace NLoad
{
    public sealed class LoadTest<T> where T : ITest, new()
    {
        private readonly LoadTestConfiguration _configuration;
        private readonly List<Heartbeat> _heartbeat = new List<Heartbeat>();
        private readonly ManualResetEvent _quitEvent = new ManualResetEvent(false);

        public event EventHandler<Heartbeat> Heartbeat;

        #region Ctor

        [ExcludeFromCodeCoverage]
        public LoadTest(LoadTestConfiguration configuration)
        {
            if (configuration == null)
            {
                throw new ArgumentNullException("configuration");
            }

            _configuration = configuration;
        }

        #endregion

        #region Properties

        public LoadTestConfiguration Configuration
        {
            get { return _configuration; }
        }

        #endregion

        public LoadTestResult Run()
        {
            try
            {
                var stopWatch = Stopwatch.StartNew();

                var testRunners = CreateAndInitializeTestRunners(_configuration.NumberOfThreads, _quitEvent);

                StartTestRunners(testRunners);

                MonitorHeartRate();

                ShutdownTestRunners();

                WaitForTestRunners(testRunners);

                stopWatch.Stop();


                var maxResponseTime = TimeSpan.Zero;
                var minResponseTime = TimeSpan.Zero;
                var averageResponseTime = TimeSpan.Zero;

                var testRuns = testRunners.Where(k => k.Result != null && k.Result.TestRuns != null)
                                             .SelectMany(k => k.Result.TestRuns)
                                             .ToList();

                if (testRuns.Any())
                {
                    maxResponseTime = testRuns.Max(k => k.ResponseTime);
                    minResponseTime = testRuns.Min(k => k.ResponseTime);
                    averageResponseTime = new TimeSpan(Convert.ToInt64((testRuns.Average(k => k.ResponseTime.Ticks))));
                }

                var result = new LoadTestResult
                {
                    TestRunnersResults = testRunners.Select(k => k.Result),
                    TotalIterations = testRunners.Where(k => k.Result != null).Sum(k => k.Result.Iterations), //TestRunner<T>.TotalIterations, 
                    TotalRuntime = stopWatch.Elapsed,
                    Heartbeat = _heartbeat,

                    MaxResponseTime = maxResponseTime,
                    MinResponseTime = minResponseTime,
                    AverageResponseTime = averageResponseTime,
                };

                if (_heartbeat == null)
                {
                    return result;
                }

                var heartbeats = _heartbeat.Where(k => !double.IsNaN(k.Throughput));

                if (!heartbeats.Any())
                {
                    return result;
                }

                result.MaxThroughput = _heartbeat.Where(k => !double.IsNaN(k.Throughput)).Max(k => k.Throughput);
                result.MinThroughput = _heartbeat.Where(k => !double.IsNaN(k.Throughput)).Min(k => k.Throughput);
                result.AverageThroughput = _heartbeat.Where(k => !double.IsNaN(k.Throughput)).Average(k => k.Throughput);

                return result;
            }
            catch (Exception e)
            {
                throw new NLoadException("An error occurred while running load test. See inner exception for details.", e);
            }
        }

        private static List<TestRunner<T>> CreateAndInitializeTestRunners(int count, ManualResetEvent quitEvent)
        {
            var testRunners = new List<TestRunner<T>>(count);

            for (var i = 0; i < count; i++)
            {
                var testRunner = new TestRunner<T>(quitEvent);

                testRunner.Initialize();

                testRunners.Add(testRunner);
            }

            return testRunners;
        }

        private static void StartTestRunners(List<TestRunner<T>> testRunners)
        {
            testRunners.ForEach(k => k.Start()); //todo: add error handling
        }

        private void MonitorHeartRate()
        {
            var running = true;

            while (double.IsNaN(TestRunner<T>.TotalIterations))
            {
            }

            var start = DateTime.UtcNow;

            while (running)
            {
                var elapsed = DateTime.UtcNow - start;

                if (elapsed < _configuration.Duration)
                {
                    var throughput = TestRunner<T>.TotalIterations / elapsed.TotalSeconds;

                    if (double.IsNaN(throughput))
                    {
                        continue;
                    }

                    OnHeartbeat(throughput, elapsed);

                    if (DateTime.UtcNow - start < _configuration.Duration)
                    {
                        Thread.Sleep(1000);
                    }
                }
                else
                {
                    running = false;
                }
            }
        }

        private void ShutdownTestRunners()
        {
            _quitEvent.Set();
        }

        private static void WaitForTestRunners(List<TestRunner<T>> testRunners)
        {
            while (true)
            {
                if (testRunners.Any(w => w.IsBusy))
                {
                    Thread.Sleep(1); //todo: ???
                }
                else
                {
                    break;
                }
            }
        }

        private void OnHeartbeat(double throughput, TimeSpan delta)
        {
            var heartbeat = new Heartbeat(DateTime.UtcNow, throughput, delta);

            _heartbeat.Add(heartbeat);

            var handler = Heartbeat;

            if (handler != null)
            {
                handler(this, heartbeat); //todo: add try catch?
            }
        }
    }
}