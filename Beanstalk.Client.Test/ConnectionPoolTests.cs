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
        public void Different_host_port_gets_differnt_pool() {
            var pool = ConnectionPool.GetPool("foo", 123);
            Assert.AreNotSame(pool, ConnectionPool.GetPool("bob", 123));
        }

        [Test]

        public void Too_many_busy_connections_throws() {
            Func<ISocket> socketFactory = () => new MockSocket();
            var pool = new ConnectionPool(socketFactory);
            pool.MaxConnections = 5;
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
    }
}
