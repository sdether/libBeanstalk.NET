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
using Droog.Beanstalk.Client.Net;
using Droog.Beanstalk.Client.Protocol;

namespace Droog.Beanstalk.Client {

    public class BeanstalkClient : IBeanstalkClient, IWatchedTubeClient {
        public const string DEFAULT_TUBE = "default";
        private readonly ISocket _socket;
        private readonly byte[] _buffer = new byte[16 * 1024];
        private readonly TubeCollectionProxy _watchedTubes;
        private readonly BeanstalkDefaults _defaults = new BeanstalkDefaults();
        private bool _disposed;
        private string _currentTube = DEFAULT_TUBE;

        public BeanstalkClient(IPAddress address, int port)
            : this(ConnectionPool.GetPool(address, port)) {
        }

        public BeanstalkClient(string host, int port)
            : this(ConnectionPool.GetPool(host, port)) {
        }

        public BeanstalkClient(IConnectionPool pool)
            : this(pool.GetSocket()) {
        }

        public BeanstalkClient(ISocket socket) {
            if (socket == null) {
                throw new ArgumentNullException("socket");
            }
            _socket = socket;
            InitSocket();
            _currentTube = DEFAULT_TUBE;
            _watchedTubes = new TubeCollectionProxy(this, new[] { DEFAULT_TUBE });
        }

        private void InitSocket() {
            Exec(Request.Create(RequestCommand.Use).AppendArgument(DEFAULT_TUBE).ExpectStatuses(ResponseStatus.Using));
            var client = ((IWatchedTubeClient)this);
            var tubes = new HashSet<string>();
            foreach(var tube in client.ListWatchedTubes()) {
                tubes.Add(tube);
            }
            if(tubes.Contains(DEFAULT_TUBE)) {
                tubes.Remove(DEFAULT_TUBE);
            } else {
                client.Watch(DEFAULT_TUBE);
            }
            var watched = 1;
            foreach(var tube in tubes) {
                watched = client.Ignore(tube);
            }
            if(watched != 1) {
                _socket.Dispose();
                throw new InitException();
            }
        }

        public IWatchedTubeCollection WatchedTubes { get { return _watchedTubes; } }

        public bool Disposed {
            get {
                Connected();
                return _disposed;
            }
        }

        public string CurrentTube {
            get {
                // TODO: query the server? Sometimes?
                return _currentTube;
            }
            set {
                var response = Exec(Request.Create(RequestCommand.Use).AppendArgument(value).ExpectStatuses(ResponseStatus.Using));
                _currentTube = response.Arguments[0];
            }
        }

        public BeanstalkDefaults Defaults {
            get { return _defaults; }
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
            var response = Exec(Request.Create(RequestCommand.ReserveWithTimeout)
                .AppendArgument(timeout)
                .ExpectStatuses(ResponseStatus.DeadlineSoon | ResponseStatus.TimedOut | ResponseStatus.Reserved));
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

        public ReservationStatus TryReserve(TimeSpan timeout, out Job job) {
            var response = Exec(Request.Create(RequestCommand.ReserveWithTimeout)
                .AppendArgument(timeout)
                .ExpectStatuses(ResponseStatus.DeadlineSoon | ResponseStatus.TimedOut | ResponseStatus.Reserved));
            switch(response.Status) {
                case ResponseStatus.Reserved:
                    job = new Job(uint.Parse(response.Arguments[0]), response.Data, long.Parse(response.Arguments[1]));
                    break;
                default:
                    job = null;
                    break;
            }
            return response.Status.ToReservationStatus();
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
            var response = Exec(Request.Create(RequestCommand.Bury)
                .AppendArgument(jobId)
                .AppendArgument(priority)
                .ExpectStatuses(ResponseStatus.Buried | ResponseStatus.NotFound));
            return response.Status == ResponseStatus.Buried;
        }

        public bool Touch(uint jobId) {
            var response = Exec(Request.Create(RequestCommand.Touch)
                .AppendArgument(jobId)
                .ExpectStatuses(ResponseStatus.Touched | ResponseStatus.NotFound));
            return response.Status == ResponseStatus.Touched;
        }

        public Job Peek(uint jobId) {
            return Peek(Request.Create(RequestCommand.Peek).AppendArgument(jobId));
        }

        public Job PeekReady() {
            return Peek(Request.Create(RequestCommand.PeekReady));
        }

        public Job PeekDelayed() {
            return Peek(Request.Create(RequestCommand.PeekDelayed));
        }

        public Job PeekBuried() {
            return Peek(Request.Create(RequestCommand.PeekBuried));
        }

        public uint Kick(uint bound) {
            var response = Exec(Request.Create(RequestCommand.Kick)
                .AppendArgument(bound)
                .ExpectStatuses(ResponseStatus.Kicked));
            return uint.Parse(response.Arguments[0]);
        }

        public JobStats GetJobStats(uint jobId) {
            var response = Exec(Request.Create(RequestCommand.StatsJob).AppendArgument(jobId).ExpectStatuses(ResponseStatus.Ok | ResponseStatus.NotFound));
            return response.Status == ResponseStatus.Ok ? new JobStats(MicroYaml.ParseDictionary(response)) : null;
        }

        public TubeStats GetTubeStats(string tube) {
            var response = Exec(Request.Create(RequestCommand.StatsTube).AppendArgument(tube).ExpectStatuses(ResponseStatus.Ok | ResponseStatus.NotFound));
            return response.Status == ResponseStatus.Ok ? new TubeStats(MicroYaml.ParseDictionary(response)) : null;
        }

        public ServerStats GetServerStats() {
            var response = Exec(Request.Create(RequestCommand.Stats).ExpectStatuses(ResponseStatus.Ok));
            return new ServerStats(MicroYaml.ParseDictionary(response));
        }

        public IEnumerable<string> GetTubes() {
            var response = Exec(Request.Create(RequestCommand.ListTubes).ExpectStatuses(ResponseStatus.Ok));
            return MicroYaml.ParseList(response);
        }

        int IWatchedTubeClient.Watch(string tube) {
            var response = Exec(Request.Create(RequestCommand.Watch).AppendArgument(tube).ExpectStatuses(ResponseStatus.Watching));
            return int.Parse(response.Arguments[0]);
        }

        int IWatchedTubeClient.Ignore(string tube) {
            var response = Exec(Request.Create(RequestCommand.Ignore).AppendArgument(tube).ExpectStatuses(ResponseStatus.Watching | ResponseStatus.NotIgnored));
            return response.Status == ResponseStatus.Watching ? int.Parse(response.Arguments[0]) : 0;
        }

        IEnumerable<string> IWatchedTubeClient.ListWatchedTubes() {
            var response = Exec(Request.Create(RequestCommand.ListTubesWatched).ExpectStatuses(ResponseStatus.Ok));
            return MicroYaml.ParseList(response);
        }

        private Job Peek(Request request) {
            var response = Exec(request.ExpectStatuses(ResponseStatus.Found | ResponseStatus.NotFound));
            return response.Status == ResponseStatus.NotFound
                ? null
                : new Job(uint.Parse(response.Arguments[0]), response.Data, long.Parse(response.Arguments[1]));
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
            ThrowIfDisposed();
            if(Connected()) {
                return;
            }
            ThrowIfDisposed();
        }

        private void ThrowIfDisposed() {
            if(_disposed) {
                throw new ObjectDisposedException(GetType().ToString());
            }
        }

        private bool Connected() {
            if(!_socket.Connected) {
                _socket.Dispose();
                _disposed = true;
            }
            return !_disposed;
        }

        public void Dispose() {
            Dispose(true);
        }

        private void Dispose(bool suppressFinalizer) {
            if(_disposed) {
                return;
            }
            if(suppressFinalizer) {
                GC.SuppressFinalize(this);
            }
	    if (_socket != null) {
                _socket.Dispose();
	    }
            _disposed = true;
        }

        ~BeanstalkClient() {
            Dispose(false);
        }
    }
}
