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
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Droog.Beanstalk.Client.Protocol;

namespace Droog.Beanstalk.Client {
    public class BeanstalkClient : IBeanstalkClient {
        public static readonly TimeSpan DefaultConnectTimeout = TimeSpan.FromSeconds(10);

        private readonly Func<ISocket> _socketFactory;
        private ISocket _socket;
        private bool _isDisposed;
        private readonly byte[] _buffer = new byte[16 * 1024];
        private string _currentTube;

        public BeanstalkClient(IPAddress address, int port)
            : this(address, port, DefaultConnectTimeout) {
        }

        public BeanstalkClient(IPAddress address, int port, TimeSpan connectTimeout) {
            _socketFactory = () => {
                var timeout = new ManualResetEvent(false);
                Exception connectFailure = null;
                var tcpClient = new TcpClient();
                var ar = tcpClient.BeginConnect(address, port, r => {
                    try {
                        tcpClient.EndConnect(r);
                    } catch(Exception e) {
                        connectFailure = e;
                    } finally {
                        timeout.Set();
                    }
                }, null);

                if(!timeout.WaitOne(connectTimeout)) {
                    tcpClient.EndConnect(ar);
                    throw new TimeoutException();
                }
                if(connectFailure != null) {
                    throw new ConnectException(connectFailure);
                }
                return new SocketAdapter(tcpClient);
            };
        }

        public BeanstalkClient(string host, int port)
            : this(host, port, DefaultConnectTimeout) {
        }

        public BeanstalkClient(string host, int port, TimeSpan connectTimeout) {
            _socketFactory = () => {
                var timeout = new ManualResetEvent(false);
                Exception connectFailure = null;
                var tcpClient = new TcpClient();
                var ar = tcpClient.BeginConnect(host, port, r => {
                    try {
                        tcpClient.EndConnect(r);
                    } catch(Exception e) {
                        connectFailure = e;
                    } finally {
                        timeout.Set();
                    }
                }, null);

                if(!timeout.WaitOne(connectTimeout)) {
                    tcpClient.EndConnect(ar);
                    throw new TimeoutException();
                }
                if(connectFailure != null) {
                    throw new ConnectException(connectFailure);
                }
                return new SocketAdapter(tcpClient);
            };
        }

        public BeanstalkClient(Func<ISocket> socketFactory) {
            _socketFactory = socketFactory;
        }

        public void Connect() {
            VerifyConnection();
        }

        public string CurrentTube {
            get {
                // TODO: query the server? Sometimes?
                return _currentTube;
            }
            set {
                Exec(Request.Create(RequestCommand.Use).AppendArgument(value).ExpectStatuses(ResponseStatus.Using));
                _currentTube = value;
            }
        }

        public PutResponse Put(uint priority, TimeSpan delay, TimeSpan timeToRun, Stream request, long length) {
            var response = Exec(Request.Create(RequestCommand.Put)
                .AppendArgument(priority)
                .AppendArgument(delay)
                .AppendArgument(timeToRun)
                .WithData(request, length)
                .ExpectStatuses(ResponseStatus.Inserted | ResponseStatus.Buried | ResponseStatus.ExpectedCrlf | ResponseStatus.JobTooBig));
            if(response.Status == ResponseStatus.Inserted || response.Status == ResponseStatus.Buried) {
                return new PutResponse(response.Status == ResponseStatus.Buried, uint.Parse(response.Arguments[0]));
            }
            throw new PutFailedException(response.Status);
        }

        public ICollection<string> WatchedTubes {
            get { throw new NotImplementedException(); }
        }

        public Job Reserve() {
            var response = Exec(Request.Create(RequestCommand.Reserve).ExpectStatuses(ResponseStatus.DeadlineSoon | ResponseStatus.Reserved));
            switch(response.Status) {
                case ResponseStatus.Reserved:
                    return new Job(uint.Parse(response.Arguments[0]), response.Data, long.Parse(response.Arguments[1]));
                case ResponseStatus.DeadlineSoon:
                    throw new DeadlineSoonException();
            }
            throw new ShouldNeverHappenException();
        }

        public Job Reserve(TimeSpan timeout) {
            var response = Exec(Request.Create(RequestCommand.Reserve)
                .AppendArgument(timeout)
                .ExpectStatuses(ResponseStatus.DeadlineSoon | ResponseStatus.TimedOut| ResponseStatus.Reserved));
            switch(response.Status) {
                case ResponseStatus.Reserved:
                    return new Job(uint.Parse(response.Arguments[0]), response.Data, long.Parse(response.Arguments[1]));
                case ResponseStatus.TimedOut:
                    throw new TimedoutException();
                case ResponseStatus.DeadlineSoon:
                    throw new DeadlineSoonException();
            }
            throw new ShouldNeverHappenException();
        }

        public bool Delete(uint jobId) {
            var response = Exec(Request.Create(RequestCommand.Delete)
                .AppendArgument(jobId)
                .ExpectStatuses(ResponseStatus.Deleted | ResponseStatus.NotFound));
            return response.Status == ResponseStatus.Deleted;
        }

        public ReleaseStatus Release(uint jobId, uint priority, TimeSpan delay) {
            var response = Exec(Request.Create(RequestCommand.Release)
                .AppendArgument(jobId)
                .AppendArgument(priority)
                .AppendArgument(delay)
                .ExpectStatuses(ResponseStatus.Released | ResponseStatus.Buried | ResponseStatus.NotFound));
            return response.Status.ToReleaseStatus();
        }

        public bool Bury(uint jobId, uint priority) {
            throw new NotImplementedException();
        }

        public bool Touch(uint jobId) {
            throw new NotImplementedException();
        }

        public PeekResponse Peek(uint jobId) {
            throw new NotImplementedException();
        }

        public PeekResponse PeekReady() {
            throw new NotImplementedException();
        }

        public PeekResponse PeekDelayed() {
            throw new NotImplementedException();
        }

        public PeekResponse PeekBuried() {
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

        public IEnumerable<string> KnownTubes {
            get { throw new NotImplementedException(); }
        }

        public bool Connected { get { return _socket == null ? false : _socket.Connected; } }

        public void Close() {
            if(_isDisposed) {
                throw new ObjectDisposedException(GetType().ToString());
            }
            if(_socket == null || !_socket.Connected) {
                return;
            }
            _socket.Close();
            _socket = null;
        }

        private Response Exec(Request request) {
            VerifyConnection();
            _socket.SendRequest(request, _buffer);
            var response = _socket.ReceiveResponse(_buffer);
            if((response.Status & request.ExpectedStatuses) != response.Status) {
                throw new InvalidStatusException(request.Command, response.Status);
            }
            return response;
        }

        private void VerifyConnection() {
            if(_isDisposed) {
                throw new ObjectDisposedException(GetType().ToString());
            }
            if(_socket != null && !_socket.Connected) {
                _socket.Close();
                _socket = null;
            }
            if(_socket == null) {
                _socket = _socketFactory();
            }
        }

        public void Dispose() {
            Dispose(true);
        }

        private void Dispose(bool suppressFinalizer) {
            if(_isDisposed) {
                return;
            }
            if(suppressFinalizer) {
                GC.SuppressFinalize(this);
            }
            Close();
            _isDisposed = true;
        }

        ~BeanstalkClient() {
            Dispose(false);
        }
    }
}