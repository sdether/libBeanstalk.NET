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
using System.IO;
using System.Linq;
using Droog.Beanstalk.Client.Net;
using Droog.Beanstalk.Client.Test;
using NUnit.Framework;

namespace Droog.Beanstalk.Client.IntegrationTest {
    [TestFixture]
    public class BeanstalkClientTests {

        [Test]
        public void Put_Reserve_Delete() {
            using(var client = CreateClient()) {
                var data = "abc";
                var stream = data.AsStream();
                var put = client.Put(100, TimeSpan.Zero, TimeSpan.FromMinutes(2), stream, data.Length);
                var reserve = client.Reserve();
                Assert.AreEqual(put.JobId, reserve.Id);
                Assert.IsTrue(client.Delete(reserve.Id));
                using(var reader = new StreamReader(reserve.Data)) {
                    Assert.AreEqual(data, reader.ReadToEnd());
                }
            }
        }

        [Test]
        public void Sequential_clients_reuse_connection() {
            var pool = ConnectionPool.Create(TestConfig.Host, TestConfig.Port);
            Assert.AreEqual(0, pool.ActiveConnections);
            Assert.AreEqual(0, pool.IdleConnections);
            using(var client = new BeanstalkClient(pool)) {
                var data = "abc";
                var stream = data.AsStream();
                var put = client.Put(100, TimeSpan.Zero, TimeSpan.FromMinutes(2), stream, data.Length);
                var reserve = client.Reserve();
                Assert.AreEqual(put.JobId, reserve.Id);
                Assert.IsTrue(client.Delete(reserve.Id));
                using(var reader = new StreamReader(reserve.Data)) {
                    Assert.AreEqual(data, reader.ReadToEnd());
                }
                Assert.AreEqual(1, pool.ActiveConnections);
                Assert.AreEqual(0, pool.IdleConnections);
            }
            Assert.AreEqual(0, pool.ActiveConnections);
            Assert.AreEqual(1, pool.IdleConnections);
            using(var client = new BeanstalkClient(pool)) {
                var data = "abc";
                var stream = data.AsStream();
                var put = client.Put(100, TimeSpan.Zero, TimeSpan.FromMinutes(2), stream, data.Length);
                var reserve = client.Reserve();
                Assert.AreEqual(put.JobId, reserve.Id);
                Assert.IsTrue(client.Delete(reserve.Id));
                using(var reader = new StreamReader(reserve.Data)) {
                    Assert.AreEqual(data, reader.ReadToEnd());
                }
                Assert.AreEqual(1, pool.ActiveConnections);
                Assert.AreEqual(0, pool.IdleConnections);
            }
            Assert.AreEqual(0, pool.ActiveConnections);
            Assert.AreEqual(1, pool.IdleConnections);
        }

        [Test]
        public void Can_change_current_tube() {
            using(var client = CreateClient()) {
                Assert.AreEqual("default", client.CurrentTube);
                client.CurrentTube = "foo";
                Assert.AreEqual("foo", client.CurrentTube);
            }
        }

        [Test]
        public void Can_Watch_and_Ignore_tubes() {
            using(var client = CreateClient()) {
                Assert.AreEqual(1, client.WatchedTubes.Count);
                client.WatchedTubes.Add("foo");
                Assert.AreEqual(2, client.WatchedTubes.Count);
                Assert.AreEqual(new[] { "default", "foo" }, client.WatchedTubes.OrderBy(x => x).ToArray());
                client.WatchedTubes.Refresh();
                Assert.AreEqual(2, client.WatchedTubes.Count);
                Assert.AreEqual(new[] { "default", "foo" }, client.WatchedTubes.OrderBy(x => x).ToArray());
                client.WatchedTubes.Remove("default");
                Assert.AreEqual(1, client.WatchedTubes.Count);
                Assert.AreEqual(new[] { "foo" }, client.WatchedTubes.ToArray());
            }
        }

        [Test]
        public void Can_get_stats() {
            using(var client = CreateClient()) {
                var stats = client.GetServerStats();
                Assert.IsNotNull(stats["version"]);
                Assert.AreEqual(stats.Version, stats["version"]);
                Assert.Greater(stats.Uptime, TimeSpan.Zero);
            }
        }
        [Test]
        public void Can_get_tube_stats() {
            using(var client = CreateClient()) {
                foreach(var tube in client.GetTubes()) {
                    var tubeStats = client.GetTubeStats(tube);
                    Console.WriteLine("--Name:                {0} --------------------", tubeStats.Name);
                    Console.WriteLine("  TotalJobs:           {0}", tubeStats.TotalJobs);
                    Console.WriteLine("  CurrentBuriedJobs:   {0}", tubeStats.CurrentBuriedJobs);
                    Console.WriteLine("  CurrentDelayedJobs:  {0}", tubeStats.CurrentDelayedJobs);
                    Console.WriteLine("  CurrentReadyJobs:    {0}", tubeStats.CurrentReadyJobs);
                    Console.WriteLine("  CurrentReservedJobs: {0}", tubeStats.CurrentReservedJobs);
                    Console.WriteLine("  CurrentUrgentJobs:   {0}", tubeStats.CurrentUrgentJobs);
                    Console.WriteLine("  CurrentWaiting:      {0}", tubeStats.CurrentWaiting);
                }
            }
        }

        [Test]
        public void Removal_of_last_watched_tube_is_ignored() {
            using(var client = CreateClient()) {
                client.WatchedTubes.Add("bob");
                client.WatchedTubes.Remove("default");
                Assert.AreEqual(1, client.WatchedTubes.Count);
                Assert.AreEqual("bob", client.WatchedTubes.First());
                client.WatchedTubes.Remove("bob");
                Assert.AreEqual(1, client.WatchedTubes.Count);
                Assert.AreEqual("bob", client.WatchedTubes.First());
            }
        }

        [Test, Ignore]
        public void Wipe_all_queues() {
            using(var client = CreateClient()) {
                long expectedJobs = 0;
                foreach(var tube in client.GetTubes()) {
                    client.WatchedTubes.Add(tube);
                    var tubeStats = client.GetTubeStats(tube);
                    expectedJobs += tubeStats.CurrentReadyJobs;
                    Console.WriteLine("{0}: {1}", tube, tubeStats.CurrentReadyJobs);
                }
                var counter = 0;
                while(true) {
                    Job job;
                    if(client.TryReserve(TimeSpan.Zero, out job) == ReservationStatus.TimedOut) {
                        break;
                    }
                    client.Delete(job.Id);
                    counter++;
                    if(counter % 1000 == 0) {
                        Console.WriteLine("removed {0} pending messages", counter);
                    }
                }
                Console.WriteLine("removed a total of {0} pending messages");
            }
        }

        private BeanstalkClient CreateClient() {
            return new BeanstalkClient(TestConfig.Host, TestConfig.Port);
        }
    }
}