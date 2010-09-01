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
        public void Bulk_put_reserve_delete_with_multiple_connections() {
            var pool = ConnectionPool.GetPool(TestConfig.Host, TestConfig.Port);
            var goSignal = new ManualResetEvent(false);
            var n = 10000;
            var enqueue = 10;
            var dequeue = 6;
            var data = new List<MemoryStream>();
            for(var i = 0; i < n; i++) {
                data.Add(("data-" + i).AsStream());
            }
            var idx = -1;
            var r = new Random();
            var enqueued = 0;
            WaitCallback enqueueWorker = id => {
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
            WaitCallback dequeueWorker = id => {
                goSignal.WaitOne();
                Thread.Sleep(200 + (int)id * 100);
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
                ThreadPool.QueueUserWorkItem(dequeueWorker, i);
            }
            for(var i = 0; i < enqueue; i++) {
                ThreadPool.QueueUserWorkItem(enqueueWorker, i);
            }
            Thread.Sleep(1000);
            goSignal.Set();
            while(dequeued < n) {
                Thread.Sleep(500);
                Console.WriteLine("{0}>{1} - busy: {2}, idle: {3}", dequeued, enqueued, pool.ActiveConnections, pool.IdleConnections);
            }
        }

        private BeanstalkClient CreateClient() {
            return new BeanstalkClient(TestConfig.Host, TestConfig.Port);
        }
    }
}
