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
using NUnit.Framework;

namespace Droog.Beanstalk.Client.Test {

    [TestFixture]
    public class BeanstalkProtocolTests {
        private MockSocket _mockSocket;
        private BeanstalkClient _client;
        private int _socketCreated = 0;

        [SetUp]
        public void Setup() {
            _socketCreated = 0;
            _mockSocket = new MockSocket();
            _client = new BeanstalkClient(() => {
                _socketCreated++;
                _mockSocket.Connected = true;
                return _mockSocket;
            });
        }

        [Test]
        public void Can_connect_to_and_disconnect_from_server() {
            _client.Connect();
            Assert.AreEqual(1, _socketCreated);
            _client.Close();
            Assert.AreEqual(1, _mockSocket.CloseCalled);
        }

        [Test]
        public void Dispose_disconnects_from_server() {
            _client.Connect();
            Assert.AreEqual(1, _socketCreated);
            _client.Dispose();
            Assert.AreEqual(1, _mockSocket.CloseCalled);
        }

        [Test]
        public void Can_put_data() {
            _mockSocket.Expect("put 123 0 60 3\r\nfoo\r\n", "INSERTED 456\r\n");
            var data = "foo".AsStream();
            var response = _client.Put(123, TimeSpan.Zero, TimeSpan.FromSeconds(60), data, data.Length);
            _mockSocket.Verify();
            Assert.AreEqual(456, response.JobId);
        }

        [Test]
        public void Can_set_tube() {
            _mockSocket.Expect("use bob\r\n", "USING bob\r\n");
            _client.CurrentTube = "bob";
            _mockSocket.Verify();
            Assert.AreEqual("bob",_client.CurrentTube);
        }

        [Test]
        public void Can_reserve_without_timeout() {
            _mockSocket.Expect("reserve\r\n", "RESERVED 123 3\r\nbar\r\n");
            var job = _client.Reserve();
            _mockSocket.Verify();
            Assert.AreEqual(123,job.Id);
            Assert.AreEqual("bar",job.Data.AsText());
        }

        [Test]
        public void Can_reserve_with_timeout() {
            _mockSocket.Expect("reserve 10\r\n", "RESERVED 123 3\r\nbar\r\n");
            var job = _client.Reserve(TimeSpan.FromSeconds(10));
            _mockSocket.Verify();
            Assert.AreEqual(123, job.Id);
            Assert.AreEqual("bar", job.Data.AsText());
        }
    }
}
