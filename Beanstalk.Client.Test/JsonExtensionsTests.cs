/*
 * libBeanstalk.NET 
 * Copyright (C) 2011 Arne F. Claassen
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
using System.IO;
using System.Linq;
using System.Text;
using Beanstalk.Client.Json;
using Moq;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace Droog.Beanstalk.Client.Test {

    [TestFixture]
    public class JsonExtensionsTests {

        [Test]
        public void Can_put_json_object() {
            var json = @"{""foo"":""bar"",""baz"":[1,2,3]}";
            var jObject = JObject.Parse(json);
            var client = new FakeClient();
            client.PutJson(jObject);
            Assert.AreEqual(json, client.PutJson);
        }

        [Test]
        public void Can_reserve_json_object() {
            var client = new FakeClient();
            client.ReserveJson = @"{""foo"":""bar"",""baz"":[1,2,3]}";
            var job = client.ReserveJson();
            Assert.AreEqual(JObject.Parse(client.ReserveJson).ToString(), job.Data.ToString());
        }

        public class FakeClient : IBeanstalkClient {
            public string PutJson;
            public string ReserveJson;
            public PutResponse Put(uint priority, TimeSpan delay, TimeSpan timeToRun, Stream request, long length) {
                using(var reader = new StreamReader(request)) {
                    PutJson = reader.ReadToEnd();
                }
                return new PutResponse(false, 1);
            }

            public BeanstalkDefaults Defaults {
                get { return new BeanstalkDefaults(); }
            }

            public void Dispose() {
                throw new NotImplementedException();
            }

            public bool Disposed {
                get { throw new NotImplementedException(); }
            }

            public string CurrentTube {
                get { throw new NotImplementedException(); }
                set { throw new NotImplementedException(); }
            }


            public IWatchedTubeCollection WatchedTubes {
                get { throw new NotImplementedException(); }
            }

            public Job Reserve() {
                var stream = new MemoryStream(Encoding.UTF8.GetBytes(JObject.Parse(ReserveJson).ToString()));
                return new Job(1, stream);
            }

            public Job Reserve(TimeSpan timeout) {
                throw new NotImplementedException();
            }

            public ReservationStatus TryReserve(TimeSpan timeout, out Job job) {
                throw new NotImplementedException();
            }

            public bool Delete(uint jobId) {
                throw new NotImplementedException();
            }

            public ReleaseStatus Release(uint jobId, uint priority, TimeSpan delay) {
                throw new NotImplementedException();
            }

            public bool Bury(uint jobId, uint priority) {
                throw new NotImplementedException();
            }

            public bool Touch(uint jobId) {
                throw new NotImplementedException();
            }

            public Job Peek(uint jobId) {
                throw new NotImplementedException();
            }

            public Job PeekReady() {
                throw new NotImplementedException();
            }

            public Job PeekDelayed() {
                throw new NotImplementedException();
            }

            public Job PeekBuried() {
                throw new NotImplementedException();
            }

            public uint Kick(uint bound) {
                throw new NotImplementedException();
            }

            public JobStats GetJobStats(uint jobId) {
                throw new NotImplementedException();
            }

            public TubeStats GetTubeStats(string tube) {
                throw new NotImplementedException();
            }

            public ServerStats GetServerStats() {
                throw new NotImplementedException();
            }

            public IEnumerable<string> GetTubes() {
                throw new NotImplementedException();
            }
        }
    }
}
