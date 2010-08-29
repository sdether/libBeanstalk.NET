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

namespace Droog.Beanstalk.Client.Net {

    // TODO: need to timeout idle sockets
    // TODO: need to remove idle pools
    public class ConnectionPool : IConnectionPool {

        public static readonly TimeSpan DefaultConnectTimeout = TimeSpan.FromSeconds(10);
        public static readonly int DefaultMaxConnections = 100;
        private static Dictionary<string, ConnectionPool> _pools = new Dictionary<string, ConnectionPool>();

        public static ConnectionPool GetPool(IPAddress address, int port) {
            lock(_pools) {
                ConnectionPool pool;
                var key = string.Format("{0}:{1}", address, port);
                if(!_pools.TryGetValue(key, out pool)) {
                    pool = new ConnectionPool(address, port);
                    _pools[key] = pool;
                }
                return pool;
            }
        }

        public static ConnectionPool GetPool(string host, int port) {
            lock(_pools) {
                ConnectionPool pool;
                var key = string.Format("{0}:{1}", host, port);
                if(!_pools.TryGetValue(key, out pool)) {
                    pool = new ConnectionPool(host, port);
                    _pools[key] = pool;
                }
                return pool;
            }
        }

        private readonly Func<ISocket> _socketFactory;
        private readonly Queue<ISocket> _availableSockets = new Queue<ISocket>();
        private readonly HashSet<ISocket> _busySockets = new HashSet<ISocket>();

        private ConnectionPool(IPAddress address, int port) {
            ConnectTimeout = DefaultConnectTimeout;
            MaxConnections = DefaultMaxConnections;
            _socketFactory = () => SocketAdapter.Open(address, port, ConnectTimeout);
        }

        private ConnectionPool(string host, int port) {
            ConnectTimeout = DefaultConnectTimeout;
            MaxConnections = DefaultMaxConnections;
            _socketFactory = () => SocketAdapter.Open(host, port, ConnectTimeout);
        }

        public ConnectionPool(Func<ISocket> socketFactory) {
            ConnectTimeout = DefaultConnectTimeout;
            MaxConnections = DefaultMaxConnections;
            _socketFactory = socketFactory;
        }

        public TimeSpan ConnectTimeout { get; set; }
        public int MaxConnections { get; set; }

        public ISocket GetSocket() {
            lock(_availableSockets) {
                if(_availableSockets.Count > 0) {
                    return WrapSocket(_availableSockets.Dequeue());
                }
                if(_availableSockets.Count + _busySockets.Count >= MaxConnections) {
                    throw new PoolExhaustedException(MaxConnections);
                }
                return WrapSocket(_socketFactory());
            }
        }

        private ISocket WrapSocket(ISocket socket) {
            _busySockets.Add(socket);
            return new PoolSocket(socket, Reclaim);
        }

        private void Reclaim(ISocket socket) {
            lock(_availableSockets) {
                if(!_busySockets.Remove(socket)) {
                    throw new InvalidOperationException("tried to remove a socket from pool that doesn't belong to the pool");
                }
                if(!socket.Connected || _availableSockets.Count + _busySockets.Count >= MaxConnections) {

                    // drop socket
                    socket.Dispose();
                    return;
                }
                _availableSockets.Enqueue(socket);
            }
        }
    }

    public class PoolExhaustedException : Exception {
        public readonly int MaxConnections;

        public PoolExhaustedException(int maxConnections) {
            MaxConnections = maxConnections;
        }
    }
}
