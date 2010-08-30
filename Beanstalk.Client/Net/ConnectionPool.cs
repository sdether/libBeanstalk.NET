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
using Droog.Beanstalk.Client.Net.Helper;
using System.Linq;

namespace Droog.Beanstalk.Client.Net {

    // TODO: need to timeout idle pools
    public class ConnectionPool : IConnectionPool {

        private class Available {
            public readonly DateTime Queued;
            public readonly ISocket Socket;

            public Available(ISocket socket) {
                Queued = DateTime.UtcNow;
                Socket = socket;
            }
        }

        public static readonly TimeSpan DefaultConnectTimeout = TimeSpan.FromSeconds(10);
        public static readonly TimeSpan DefaultIdleTimeout = TimeSpan.FromSeconds(30);
        public static readonly int DefaultMaxConnections = 100;
        private static readonly Dictionary<string, ConnectionPool> _pools = new Dictionary<string, ConnectionPool>();

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

        public static ConnectionPool Create(IPAddress address, int port) {
            return new ConnectionPool(address, port);
        }

        public static ConnectionPool Create(string host, int port) {
            return new ConnectionPool(host, port);
        }

        private readonly Func<ISocket> _socketFactory;
        private readonly Queue<Available> _availableSockets = new Queue<Available>();
        private readonly Dictionary<ISocket, WeakReference> _busySockets = new Dictionary<ISocket, WeakReference>();
        private readonly Timer _socketCleanupTimer;
        private TimeSpan _cleanupInterval = TimeSpan.FromSeconds(60);

        private ConnectionPool(IPAddress address, int port) : this(() => SocketAdapter.Open(address, port, DefaultConnectTimeout)) { }

        private ConnectionPool(string host, int port) : this(() => SocketAdapter.Open(host, port, DefaultConnectTimeout)) { }

        public ConnectionPool(Func<ISocket> socketFactory) {
            ConnectTimeout = DefaultConnectTimeout;
            MaxConnections = DefaultMaxConnections;
            _socketFactory = socketFactory;
            _socketCleanupTimer = new Timer(ReapSockets, null, _cleanupInterval, _cleanupInterval);
        }

        public TimeSpan IdleTimeout { get; set; }
        public TimeSpan ConnectTimeout { get; set; }
        public int MaxConnections { get; set; }

        public TimeSpan CleanupInterval {
            get { return _cleanupInterval; }
            set {
                if(value == _cleanupInterval) {
                    return;
                }
                _cleanupInterval = value;
                _socketCleanupTimer.Change(_cleanupInterval, _cleanupInterval);
            }
        }

        public ISocket GetSocket() {
            lock(_availableSockets) {
                ISocket socket = null;
                while(socket == null && _availableSockets.Count > 0) {
                    if(_availableSockets.Count <= 0) {
                        break;
                    }
                    var available = _availableSockets.Dequeue();
                    if(available.Socket.Connected) {
                        socket = available.Socket;
                        continue;
                    }
                    available.Socket.Dispose();
                }
                if(socket == null) {
                    if(_availableSockets.Count + _busySockets.Count >= MaxConnections) {
                        ReapSockets(null);
                        if(_availableSockets.Count + _busySockets.Count >= MaxConnections) {
                            throw new PoolExhaustedException(MaxConnections);
                        }
                        return GetSocket();
                    }
                    socket = _socketFactory();
                }
                return WrapSocket(socket);
            }
        }

        private ISocket WrapSocket(ISocket socket) {
            var poolsocket = new PoolSocket(socket, Reclaim);
            _busySockets.Add(socket, new WeakReference(poolsocket));
            return poolsocket;
        }

        private void Reclaim(ISocket socket) {
            lock(_availableSockets) {
                if(!_busySockets.Remove(socket)) {
                    return;
                }
                if(!socket.Connected || _availableSockets.Count + _busySockets.Count >= MaxConnections) {

                    // drop socket
                    socket.Dispose();
                    return;
                }
                _availableSockets.Enqueue(new Available(socket));
            }
        }

        private void ReapSockets(object state) {
            lock(_availableSockets) {
                var dead = new List<ISocket>();
                foreach(var busy in _busySockets) {
                    if(!busy.Key.Connected || !busy.Value.IsAlive) {
                        dead.Add(busy.Key);
                    }
                }
                foreach(var socket in dead) {
                    _busySockets.Remove(socket);
                    if(socket.Connected) {
                        _availableSockets.Enqueue(new Available(socket));
                    } else {
                        socket.Dispose();
                    }
                }
                var staleTime = DateTime.UtcNow.Subtract(IdleTimeout);
                while(true) {
                    if(!_availableSockets.Any() || _availableSockets.Peek().Queued > staleTime) {
                        break;
                    }
                    var idle = _availableSockets.Dequeue();
                    idle.Socket.Dispose();
                }
            }
        }
    }
}
