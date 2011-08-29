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
using System.Net;
using System.Threading;
using Droog.Beanstalk.Client.Net;
using NUnit.Framework;

namespace Droog.Beanstalk.Client.Test {

    [TestFixture]
    public class ConnectionPoolTests {

        [Test]
        public void Same_ipaddress_port_gets_same_pool() {
            var pool = ConnectionPool.GetPool(new IPAddress(12345), 123);
            Assert.AreSame(pool, ConnectionPool.GetPool(new IPAddress(12345), 123));
        }

        [Test]
        public void Different_ipaddress_port_gets_differnt_pool() {
            var pool = ConnectionPool.GetPool(new IPAddress(12345), 123);
            Assert.AreNotSame(pool, ConnectionPool.GetPool(new IPAddress(234234), 123));
        }

        [Test]
        public void Same_host_port_gets_same_pool() {
            var pool = ConnectionPool.GetPool("foo", 123);
            Assert.AreSame(pool, ConnectionPool.GetPool("foo", 123));
        }

        [Test]
        public void Different_host_port_gets_different_pool() {
            var pool = ConnectionPool.GetPool("foo", 123);
            Assert.AreNotSame(pool, ConnectionPool.GetPool("bob", 123));
        }

        [Test]
        public void Too_many_busy_connections_throws() {
            Func<ISocket> socketFactory = () => new MockSocket();
            var pool = new ConnectionPool(socketFactory) { MaxConnections = 5 };
            var s1 = pool.GetSocket();
            var s2 = pool.GetSocket();
            var s3 = pool.GetSocket();
            var s4 = pool.GetSocket();
            var s5 = pool.GetSocket();
            try {
                pool.GetSocket();
                Assert.Fail("didn't throw");
            } catch(PoolExhaustedException) {
                return;
            } catch(Exception e) {
                Assert.Fail(string.Format("threw {0} instead of PoolExhaustedException", e));
            }
        }

        [Test]
        public void When_pool_has_too_many_connections_and_gc_has_run_unreferenced_busy_sockets_are_collected() {
            Func<ISocket> socketFactory = () => new MockSocket();
            var pool = new ConnectionPool(socketFactory) { MaxConnections = 5 };
            var s1 = pool.GetSocket();
            var s2 = pool.GetSocket();
            var s3 = pool.GetSocket();
            var s4 = pool.GetSocket();
            var s5 = pool.GetSocket();
            s5 = null;
            GC.Collect();
            var s6 = pool.GetSocket();
            Assert.IsNotNull(s6);
        }

        [Test]
        public void Disposing_pool_socket_returns_it_to_the_pool() {
            var sockets = new List<MockSocket>();
            Func<ISocket> socketFactory = () => {
                var socket = new MockSocket();
                sockets.Add(socket);
                return socket;
            };
            var pool = new ConnectionPool(socketFactory);
            var s1 = pool.GetSocket();
            Assert.AreEqual(1, sockets.Count);
            s1.Dispose();
            var s2 = pool.GetSocket();
            Assert.AreEqual(1, sockets.Count);
        }

        [Test]
        public void Disposing_pool_socket_does_not_dispose_wrapped_socket() {
            var sockets = new List<MockSocket>();
            Func<ISocket> socketFactory = () => {
                var socket = new MockSocket();
                sockets.Add(socket);
                return socket;
            };
            var pool = new ConnectionPool(socketFactory);
            var s1 = pool.GetSocket();
            Assert.AreEqual(1, sockets.Count);
            s1.Dispose();
            Assert.IsFalse(s1.Connected);
            Assert.IsTrue(sockets[0].Connected);
        }

        [Test]
        public void Getting_multiple_pool_sockets_returns_different_instances() {
            var sockets = new List<MockSocket>();
            Func<ISocket> socketFactory = () => {
                var socket = new MockSocket();
                sockets.Add(socket);
                return socket;
            };
            var pool = new ConnectionPool(socketFactory);
            var s1 = pool.GetSocket();
            var s2 = pool.GetSocket();
            Assert.AreEqual(2, sockets.Count);
        }

        [Test]
        public void Disconnected_sockets_are_removed_from_pool() {
            var sockets = new List<MockSocket>();
            Func<ISocket> socketFactory = () => {
                var socket = new MockSocket();
                sockets.Add(socket);
                return socket;
            };
            var pool = new ConnectionPool(socketFactory);
            var s1 = pool.GetSocket();
            sockets[0].Dispose();
            s1.Dispose();
            var s2 = pool.GetSocket();
            Assert.AreEqual(2, sockets.Count);
        }

        [Test]
        public void Disconnected_socket_in_available_pool_is_disposed_on_attempted_reuse() {
            var sockets = new List<MockSocket>();
            Func<ISocket> socketFactory = () => {
                var socket = new MockSocket();
                sockets.Add(socket);
                return socket;
            };
            var pool = new ConnectionPool(socketFactory);
            var s1 = pool.GetSocket();
            s1.Dispose();
            Assert.AreEqual(1, sockets.Count);
            sockets[0].Dispose();
            var s2 = pool.GetSocket();
            Assert.AreEqual(2, sockets.Count);
        }

        [Test]
        public void Disconnected_busy_sockets_are_collected_at_cleanup() {
            var sockets = new List<MockSocket>();
            Func<ISocket> socketFactory = () => {
                var socket = new MockSocket();
                sockets.Add(socket);
                return socket;
            };
            var pool = new ConnectionPool(socketFactory) { CleanupInterval = TimeSpan.FromSeconds(1) };
            var s = pool.GetSocket();
            sockets[0].Connected = false;
            Assert.AreEqual(0, sockets[0].DisposeCalled);
            Wait(() => sockets[0].DisposeCalled > 0, TimeSpan.FromSeconds(5), "socket didn't get cleaned up");
        }

        [Test]
        public void Unreferenced_busy_sockets_are_collected_at_cleanup() {
            var sockets = new List<MockSocket>();
            Func<ISocket> socketFactory = () => {
                var socket = new MockSocket();
                sockets.Add(socket);
                return socket;
            };
            var pool = new ConnectionPool(socketFactory) { CleanupInterval = TimeSpan.FromSeconds(1) };
            pool.GetSocket();
            Assert.AreEqual(0, sockets[0].DisposeCalled);
            GC.Collect();
            Wait(() => sockets[0].DisposeCalled > 0, TimeSpan.FromSeconds(5), "socket didn't get cleaned up");
        }

        [Test]
        public void Idle_available_socket_is_collected_at_cleanup() {
            var sockets = new List<MockSocket>();
            Func<ISocket> socketFactory = () => {
                var socket = new MockSocket();
                sockets.Add(socket);
                return socket;
            };
            var pool = new ConnectionPool(socketFactory) { CleanupInterval = TimeSpan.FromSeconds(1) };
            pool.GetSocket().Dispose();
            Assert.AreEqual(0, sockets[0].DisposeCalled);
            Wait(() => sockets[0].DisposeCalled > 0, TimeSpan.FromSeconds(5), "socket didn't get cleaned up");
        }

        [Test]
        public void Disposing_pool_cleans_up_sockets() {
            var sockets = new List<FakeDisposableSocket>();
            Func<ISocket> socketFactory = () => {
                var socket = new FakeDisposableSocket();
                sockets.Add(socket);
                return socket;
            };
            var pool = new ConnectionPool(socketFactory);
            var s1 = pool.GetSocket();
            var s2 = pool.GetSocket();
            s1.Dispose();
            s2.Dispose();
            Assert.AreEqual(2, sockets.Count, "wrong number of sockets created");
            Assert.IsFalse(sockets[0].Disposed, "first socket was disposed");
            Assert.IsFalse(sockets[1].Disposed, "second socket was disposed");
            pool.Dispose();
            Assert.IsTrue(sockets[0].Disposed, "first socket was not disposed");
            Assert.IsTrue(sockets[1].Disposed, "second socket was not disposed");
        }

        [Test]
        public void Disposing_pool_does_not_affect_busy_sockets() {
            var sockets = new List<FakeDisposableSocket>();
            Func<ISocket> socketFactory = () => {
                var socket = new FakeDisposableSocket();
                sockets.Add(socket);
                return socket;
            };
            var pool = new ConnectionPool(socketFactory);
            var s1 = pool.GetSocket();
            Assert.AreEqual(1, sockets.Count, "wrong number of sockets created");
            Assert.IsFalse(sockets[0].Disposed, "socket was disposed");
            pool.Dispose();
            Assert.IsFalse(sockets[0].Disposed, "socket was disposed");
            s1.Dispose();
        }

        private void Wait(Func<bool> func, TimeSpan timeout, string failMessage) {
            var end = DateTime.UtcNow.Add(timeout);
            while(DateTime.UtcNow < end) {
                Thread.Sleep(200);
                if(func()) {
                    return;
                }
            }
            Assert.Fail(failMessage);
        }
    }
}
