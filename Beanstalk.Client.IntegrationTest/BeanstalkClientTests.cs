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
        public void Can_change_current_tube() {
            using (var client = CreateClient()) {
                Assert.AreEqual("default", client.CurrentTube);
                client.CurrentTube = "foo";
                Assert.AreEqual("foo", client.CurrentTube);
            }
        }

        [Test]
        public void Can_Watch_and_Ignore_tubes() {
            using(var client = CreateClient()) {
                Assert.AreEqual(1,client.WatchedTubes.Count);
                client.WatchedTubes.Add("foo");
                Assert.AreEqual(2, client.WatchedTubes.Count);
                Assert.AreEqual(new[]{"default","foo"},client.WatchedTubes.OrderBy(x => x).ToArray());
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
            }
        }

        private BeanstalkClient CreateClient() {
            return new BeanstalkClient(TestConfig.Host, TestConfig.Port);
        }
    }
}