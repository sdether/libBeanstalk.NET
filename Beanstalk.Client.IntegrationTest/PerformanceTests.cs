/*
 * libBeanstalk.NET 
 * Copyright (C) 2010 Arne F. Claassen
 * geekblog [at] claassen [dot] net
 * http://github.com/sdether/libBeanstalk.NET 
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Droog.Beanstalk.Client.Net;
using Droog.Beanstalk.Client.Test;
using NUnit.Framework;
using System.Linq;

namespace Droog.Beanstalk.Client.IntegrationTest {

    [Ignore("Performance tests, run by hand")]
    [TestFixture]
    public class PerformanceTests {

        [Test]
        public void Sequential_bulk_put_reserve_delete() {
            var n = 10000;
            var data = new List<MemoryStream>();
            for(var i = 0; i < n; i++) {
                data.Add(("data-" + i).AsStream());
            }
            using(var client = CreateClient()) {
                var stopwatch = Stopwatch.StartNew();
                foreach(var item in data) {
                    var put = client.Put(100, TimeSpan.Zero, TimeSpan.FromMinutes(2), item, item.Length);
                }
                stopwatch.Stop();
                Console.WriteLine("put: {0:0} items/sec", n / stopwatch.Elapsed.TotalSeconds);

                var jobs = new List<Job>();
                stopwatch = Stopwatch.StartNew();
                while(true) {
                    try {
                        var job = client.Reserve(TimeSpan.Zero);
                        jobs.Add(job);
                    } catch(TimedoutException) {
                        break;
                    }
                }
                stopwatch.Stop();
                Console.WriteLine("reserve: {0:0} items/sec", n / stopwatch.Elapsed.TotalSeconds);
                Assert.AreEqual(data.Count, jobs.Count);
                for(var i = 0; i < n; i++) {
                    Assert.AreEqual(data[i].AsText(), jobs[i].Data.AsText());
                }

                stopwatch = Stopwatch.StartNew();
                foreach(var job in jobs) {
                    Assert.IsTrue(client.Delete(job.Id));
                }
                stopwatch.Stop();
                Console.WriteLine("delete: {0:0} items/sec", n / stopwatch.Elapsed.TotalSeconds);
            }
        }

        [Test]
        public void Producer_consumer() {
            using(var pool = ConnectionPool.GetPool(TestConfig.Host, TestConfig.Port)) {
                var tube = Guid.NewGuid().ToString();
                var produced = new List<string>();
                var consumed = new List<string>();
                var n = 10000;
                var producerTimer = new Stopwatch();
                var producer = new Thread(() => {
                    Console.WriteLine("producer started");
                    using(var producerClient = new BeanstalkClient(pool)) {
                        producerClient.CurrentTube = tube;
                        Console.WriteLine("producing into tube '{0}'", producerClient.CurrentTube);
                        producerTimer.Start();
                        for(var i = 0; i < n; i++) {
                            var data = Guid.NewGuid().ToString();
                            produced.Add(data);
                            producerClient.PutString(data);
                            if(i % 2000 == 0) {
                                Console.WriteLine("enqueued {0} @ {1:0} items/sec", i, i / producerTimer.Elapsed.TotalSeconds);
                            }
                        }
                        producerTimer.Stop();
                        Console.WriteLine("done producing");
                    }
                });
                producer.Start();
                Thread.Sleep(2000);
                var consumerTimer = new Stopwatch();
                Console.WriteLine("consumer started");
                using(var consumerClient = new BeanstalkClient(pool)) {
                    consumerClient.WatchedTubes.Add(tube);
                    consumerClient.WatchedTubes.Remove(BeanstalkClient.DEFAULT_TUBE);
                    Console.WriteLine("consuming from tube '{0}'", tube);
                    consumerTimer.Start();
                    while(consumed.Count < n) {
                        var job = consumerClient.ReserveString(TimeSpan.Zero);
                        consumed.Add(job.Data);
                        consumerClient.Delete(job.JobId);
                        if(consumed.Count % 2000 == 0) {
                            Console.WriteLine("dequeued {0} @ {1:0} items/sec", consumed.Count, consumed.Count / consumerTimer.Elapsed.TotalSeconds);
                        }
                    }
                    consumerTimer.Stop();
                    Console.WriteLine("done consuming");
                }
                producer.Join();
                Assert.AreEqual(n, produced.Count, "wrong number of produced items");
                Assert.AreEqual(n, consumed.Count, "wrong number of consumed items");
                Assert.AreEqual(produced.OrderBy(x => x).ToArray(), consumed.OrderBy(x => x).ToArray());
                Console.WriteLine("final enqueue: {0:0} items/sec", n / producerTimer.Elapsed.TotalSeconds);
                Console.WriteLine("final dequeue: {0:0} items/sec", n / consumerTimer.Elapsed.TotalSeconds);
            }
        }

        [Test]
        public void Producer_consumer_load_test_runs_forever() {
            var pool = ConnectionPool.GetPool(TestConfig.Host, TestConfig.Port);
            var tube = "forever-" + Guid.NewGuid().ToString();
            Console.WriteLine("using tube: {0}", tube);
            var set = 0;
            while(true) {
                set++;
                var produced = new List<string>();
                var consumed = new List<string>();
                var n = 10000;
                var producerTimer = new Stopwatch();
                var producer = new Thread(() => {
                    using(var producerClient = new BeanstalkClient(pool)) {
                        producerClient.CurrentTube = tube;
                        producerTimer.Start();
                        for(var i = 0; i < n; i++) {
                            var data = Guid.NewGuid().ToString();
                            produced.Add(data);
                            producerClient.PutString(data);
                        }
                        producerTimer.Stop();
                    }
                });
                producer.Start();
                Thread.Sleep(1000);
                var consumerTimer = new Stopwatch();
                using(var consumerClient = new BeanstalkClient(pool)) {
                    consumerClient.WatchedTubes.Add(tube);
                    consumerClient.WatchedTubes.Remove(BeanstalkClient.DEFAULT_TUBE);
                    consumerTimer.Start();
                    while(consumed.Count < n) {
                        var job = consumerClient.ReserveString(TimeSpan.Zero);
                        consumed.Add(job.Data);
                        consumerClient.Delete(job.JobId);
                    }
                    consumerTimer.Stop();
                }
                producer.Join();
                Assert.AreEqual(n, produced.Count, "wrong number of produced items");
                Assert.AreEqual(n, consumed.Count, "wrong number of consumed items");
                Assert.AreEqual(produced.OrderBy(x => x).ToArray(), consumed.OrderBy(x => x).ToArray());
                Console.WriteLine("set {0} enqueue/dequeue: {1:0}/{2:0} items/sec",
                    set,
                    n / producerTimer.Elapsed.TotalSeconds,
                    n / consumerTimer.Elapsed.TotalSeconds
                );
                using(var client = new BeanstalkClient(pool)) {
                    foreach(var t in client.GetTubes()) {
                        var tubeStats = client.GetTubeStats(t);
                        Console.WriteLine("{0}:total:{1}/buried:{2}/delayed:{3}/ready:{4}/reserved:{5}/urgent:{6}/waiting:{7}",
                            tubeStats.Name,
                            tubeStats.TotalJobs,
                            tubeStats.CurrentBuriedJobs,
                            tubeStats.CurrentDelayedJobs,
                            tubeStats.CurrentReadyJobs,
                            tubeStats.CurrentReservedJobs,
                            tubeStats.CurrentUrgentJobs,
                            tubeStats.CurrentWaiting
                        );
                    }
                }
            }
        }

        [Test]
        public void Bulk_put_reserve_delete_with_multiple_connections() {
            var pool = ConnectionPool.GetPool(TestConfig.Host, TestConfig.Port);
            var goSignal = new ManualResetEvent(false);
            var n = 10000;
            var enqueue = 7;
            var dequeue = 5;
            var data = new List<MemoryStream>();
            for(var i = 0; i < n; i++) {
                data.Add(("data-" + i).AsStream());
            }
            var idx = -1;
            var r = new Random();
            var enqueued = 0;
            ParameterizedThreadStart enqueueWorker = id => {
                goSignal.WaitOne();
                Thread.Sleep((int)id * 100);
                Console.WriteLine("enqueue worker {0:00} started", id);
                var client = new BeanstalkClient(pool);
                while(true) {
                    var i = Interlocked.Increment(ref idx);
                    if(i >= n) {
                        break;
                    }
                    var item = data[i];
                    Interlocked.Increment(ref enqueued);
                    client.Put(100, TimeSpan.Zero, TimeSpan.FromMinutes(2), item, item.Length);
                }
                client.Dispose();
                Console.WriteLine("enqueue worker {0:00} finished", id);
            };
            var dequeued = 0;
            ParameterizedThreadStart dequeueWorker = id => {
                goSignal.WaitOne();
                Thread.Sleep(500 + (int)id * 100);
                Console.WriteLine("dequeue worker {0:00} started", id);
                var client = new BeanstalkClient(pool);
                while(true) {
                    try {
                        var job = client.Reserve(TimeSpan.Zero);
                        client.Delete(job.Id);
                        Interlocked.Increment(ref dequeued);
                    } catch(TimedoutException) {
                        break;
                    }
                }
                client.Dispose();
                Console.WriteLine("dequeue worker {0:00} finished", id);
            };
            for(var i = 0; i < dequeue; i++) {
                new Thread(dequeueWorker).Start(i);
            }
            for(var i = 0; i < enqueue; i++) {
                new Thread(enqueueWorker).Start(i);
            }
            Thread.Sleep(1000);
            goSignal.Set();
            while(dequeued < n) {
                Console.WriteLine("{0}>{1} - busy: {2}, idle: {3}", dequeued, enqueued, pool.ActiveConnections, pool.IdleConnections);
                Thread.Sleep(200);
            }
            Thread.Sleep(1000);
            Console.WriteLine("{0}>{1} - busy: {2}, idle: {3}", dequeued, enqueued, pool.ActiveConnections, pool.IdleConnections);
        }

        private BeanstalkClient CreateClient() {
            return new BeanstalkClient(TestConfig.Host, TestConfig.Port);
        }
    }
}
