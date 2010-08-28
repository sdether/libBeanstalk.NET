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
using System.Linq;

namespace Droog.Beanstalk.Client.Test {

    [TestFixture]
    public class BeanstalkProtocolTests {
        private MockSocket _mockSocket;
        private BeanstalkClient _client;

        [SetUp]
        public void Setup() {
            _mockSocket = new MockSocket { Connected = true };
            _client = new BeanstalkClient(_mockSocket);
        }

        [Test]
        public void Dispose_disconnects_from_server() {
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
            Assert.IsFalse(response.Buried);
        }

        [Test]
        public void Can_put_data_buried() {
            _mockSocket.Expect("put 123 0 60 3\r\nfoo\r\n", "BURIED 456\r\n");
            var data = "foo".AsStream();
            var response = _client.Put(123, TimeSpan.Zero, TimeSpan.FromSeconds(60), data, data.Length);
            _mockSocket.Verify();
            Assert.AreEqual(456, response.JobId);
            Assert.IsTrue(response.Buried);
        }

        [Test]
        public void Can_set_tube() {
            _mockSocket.Expect("use bob\r\n", "USING bob\r\n");
            _client.CurrentTube = "bob";
            _mockSocket.Verify();
            Assert.AreEqual("bob", _client.CurrentTube);
        }

        [Test]
        public void Can_reserve_without_timeout() {
            _mockSocket.Expect("reserve\r\n", "RESERVED 123 3\r\nbar\r\n");
            var job = _client.Reserve();
            _mockSocket.Verify();
            Assert.AreEqual(123, job.Id);
            Assert.AreEqual("bar", job.Data.AsText());
        }

        [Test]
        public void Can_reserve_with_timeout() {
            _mockSocket.Expect("reserve-with-timeout 10\r\n", "RESERVED 123 3\r\nbar\r\n");
            var job = _client.Reserve(TimeSpan.FromSeconds(10));
            _mockSocket.Verify();
            Assert.AreEqual(123, job.Id);
            Assert.AreEqual("bar", job.Data.AsText());
        }

        [Test]
        public void Can_reserve_with_zero_timeout() {
            _mockSocket.Expect("reserve-with-timeout 0\r\n", "RESERVED 123 3\r\nbar\r\n");
            var job = _client.Reserve(TimeSpan.Zero);
            _mockSocket.Verify();
            Assert.AreEqual(123, job.Id);
            Assert.AreEqual("bar", job.Data.AsText());
        }

        [Test]
        public void Can_reserve_with_timeout_throwing_deadline_soon() {
            _mockSocket.Expect("reserve-with-timeout 10\r\n", "DEADLINE_SOON\r\n");
            try {
                _client.Reserve(TimeSpan.FromSeconds(10));
                Assert.Fail("didn't throw deadline soon");
            } catch(DeadlineSoonException) {

            } catch(Exception e) {
                Assert.Fail("wrong exeption: {0}", e);
            }
            _mockSocket.Verify();
        }

        [Test]
        public void Can_reserve_with_timeout_timing_out() {
            _mockSocket.Expect("reserve-with-timeout 10\r\n", "TIMED_OUT\r\n");
            try {
                _client.Reserve(TimeSpan.FromSeconds(10));
                Assert.Fail("didn't time out");
            } catch(TimedoutException) {

            } catch(Exception e) {
                Assert.Fail("wrong exeption: {0}", e);
            }
            _mockSocket.Verify();
        }

        [Test]
        public void Can_TryReserve_with_timeout() {
            _mockSocket.Expect("reserve-with-timeout 10\r\n", "RESERVED 123 3\r\nbar\r\n");
            Job job;
            Assert.AreEqual(ReservationStatus.Reserved, _client.TryReserve(TimeSpan.FromSeconds(10), out job));
            _mockSocket.Verify();
            Assert.AreEqual(123, job.Id);
            Assert.AreEqual("bar", job.Data.AsText());
        }

        [Test]
        public void Can_TryReserve_with_timeout_timing_out() {
            _mockSocket.Expect("reserve-with-timeout 10\r\n", "TIMED_OUT\r\n");
            Job job;
            Assert.AreEqual(ReservationStatus.TimedOut, _client.TryReserve(TimeSpan.FromSeconds(10), out job));
            _mockSocket.Verify();
            Assert.IsNull(job);
        }

        [Test]
        public void Can_TryReserve_with_timeout_throwing_deadline_soon() {
            _mockSocket.Expect("reserve-with-timeout 10\r\n", "DEADLINE_SOON\r\n");
            Job job;
            Assert.AreEqual(ReservationStatus.DeadlineSoon, _client.TryReserve(TimeSpan.FromSeconds(10), out job));
            _mockSocket.Verify();
            Assert.IsNull(job);
        }

        [Test]
        public void Can_delete() {
            _mockSocket.Expect("delete 123\r\n", "DELETED\r\n");
            Assert.IsTrue(_client.Delete(123));
            _mockSocket.Verify();
        }

        [Test]
        public void Can_delete_not_found() {
            _mockSocket.Expect("delete 123\r\n", "NOT_FOUND\r\n");
            Assert.IsFalse(_client.Delete(123));
            _mockSocket.Verify();
        }

        [Test]
        public void Can_release() {
            _mockSocket.Expect("release 123 456 0\r\n", "RELEASED\r\n");
            Assert.AreEqual(ReleaseStatus.Released, _client.Release(123, 456, TimeSpan.Zero));
            _mockSocket.Verify();
        }

        [Test]
        public void Can_release_buried() {
            _mockSocket.Expect("release 123 456 0\r\n", "BURIED\r\n");
            Assert.AreEqual(ReleaseStatus.Buried, _client.Release(123, 456, TimeSpan.Zero));
            _mockSocket.Verify();
        }

        [Test]
        public void Can_release_not_found() {
            _mockSocket.Expect("release 123 456 0\r\n", "NOT_FOUND\r\n");
            Assert.AreEqual(ReleaseStatus.NotFound, _client.Release(123, 456, TimeSpan.Zero));
            _mockSocket.Verify();
        }

        [Test]
        public void Can_bury() {
            _mockSocket.Expect("bury 123 456\r\n", "BURIED\r\n");
            Assert.IsTrue(_client.Bury(123, 456));
            _mockSocket.Verify();
        }

        [Test]
        public void Can_bury_not_found() {
            _mockSocket.Expect("bury 123 456\r\n", "NOT_FOUND\r\n");
            Assert.IsFalse(_client.Bury(123, 456));
            _mockSocket.Verify();
        }

        [Test]
        public void Can_touch() {
            _mockSocket.Expect("touch 123\r\n", "TOUCHED\r\n");
            Assert.IsTrue(_client.Touch(123));
            _mockSocket.Verify();
        }

        [Test]
        public void Can_touch_not_found() {
            _mockSocket.Expect("touch 123\r\n", "NOT_FOUND\r\n");
            Assert.IsFalse(_client.Touch(123));
            _mockSocket.Verify();
        }

        [Test]
        public void Can_watch_tube() {
            _mockSocket.Expect("watch bob\r\n", "WATCHING 2\r\n");
            _client.WatchedTubes.Add("bob");
            _mockSocket.Verify();
            Assert.AreEqual(new[] { "bob", "default" }, _client.WatchedTubes.OrderBy(x => x).ToArray());
        }

        [Test]
        public void Can_ignore_tube() {
            _mockSocket.Expect("watch bob\r\n", "WATCHING 2\r\n");
            _client.WatchedTubes.Add("bob");
            _mockSocket.Verify();
            _mockSocket.Expect("ignore default\r\n", "WATCHING 1\r\n");
            _client.WatchedTubes.Remove("default");
            _mockSocket.Verify();
            Assert.AreEqual(new[] { "bob" }, _client.WatchedTubes.ToArray());
        }

        [Test]
        public void Can_refresh_watched_tubes() {
            _mockSocket.Expect("list-tubes-watched\r\n", "OK 13\r\n---\r\n- bill\r\n\r\n");
            _client.WatchedTubes.Refresh();
            _mockSocket.Verify();
            Assert.AreEqual(new[] { "bill" }, _client.WatchedTubes.ToArray());
        }

        [Test]
        public void Can_peek_by_id() {
            _mockSocket.Expect("peek 123\r\n", "FOUND 123 3\r\nbar\r\n");
            var job = _client.Peek(123);
            _mockSocket.Verify();
            Assert.AreEqual(123, job.Id);
            Assert.AreEqual("bar", job.Data.AsText());
        }

        [Test]
        public void Can_peek_by_id_not_found() {
            _mockSocket.Expect("peek 123\r\n", "NOT_FOUND\r\nbar\r\n");
            Assert.IsNull(_client.Peek(123));
            _mockSocket.Verify();
        }

        [Test]
        public void Can_PeekReady() {
            _mockSocket.Expect("peek-ready\r\n", "FOUND 123 3\r\nbar\r\n");
            var job = _client.PeekReady();
            _mockSocket.Verify();
            Assert.AreEqual(123, job.Id);
            Assert.AreEqual("bar", job.Data.AsText());
        }

        [Test]
        public void Can_PeekReady_not_found() {
            _mockSocket.Expect("peek-ready\r\n", "NOT_FOUND\r\nbar\r\n");
            Assert.IsNull(_client.PeekReady());
            _mockSocket.Verify();
        }

        [Test]
        public void Can_PeekDelayed() {
            _mockSocket.Expect("peek-delayed\r\n", "FOUND 123 3\r\nbar\r\n");
            var job = _client.PeekDelayed();
            _mockSocket.Verify();
            Assert.AreEqual(123, job.Id);
            Assert.AreEqual("bar", job.Data.AsText());
        }

        [Test]
        public void Can_PeekDelayed_not_found() {
            _mockSocket.Expect("peek-delayed\r\n", "NOT_FOUND\r\nbar\r\n");
            Assert.IsNull(_client.PeekDelayed());
            _mockSocket.Verify();
        }

        [Test]
        public void Can_PeekBuried() {
            _mockSocket.Expect("peek-buried\r\n", "FOUND 123 3\r\nbar\r\n");
            var job = _client.PeekBuried();
            _mockSocket.Verify();
            Assert.AreEqual(123, job.Id);
            Assert.AreEqual("bar", job.Data.AsText());
        }

        [Test]
        public void Can_PeekBuried_not_found() {
            _mockSocket.Expect("peek-buried\r\n", "NOT_FOUND\r\nbar\r\n");
            Assert.IsNull(_client.PeekBuried());
            _mockSocket.Verify();
        }

    }
}
