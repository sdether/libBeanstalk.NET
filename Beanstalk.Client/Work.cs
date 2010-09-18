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

namespace Droog.Beanstalk.Client {
    public class Work<T> : IDisposable {
        private readonly IBeanstalkClient _client;
        private readonly T _data;
        private readonly uint _jobId;

        public Work(IBeanstalkClient client, T data, uint jobId) {
            _client = client;
            _data = data;
            _jobId = jobId;
        }

        public uint Id { get { return _jobId; } }
        public T Data { get { return _data; } }
        public uint Priority { get; set; }
        public WorkStatus Status { get; private set; }

        public bool Delete() {
            if(Status != WorkStatus.Active) {
                throw new InvalidOperationException(string.Format("Cannot delete work in '{0}' state", Status));
            }
            var deleted = _client.Delete(_jobId);
            if(deleted) {
                Status = WorkStatus.Deleted;
            }
            return deleted;
        }

        public ReleaseStatus Release() {
            return Release(Priority, TimeSpan.Zero);
        }

        public ReleaseStatus Release(uint priority, TimeSpan delay) {
            if(Status != WorkStatus.Active) {
                throw new InvalidOperationException(string.Format("Cannot release work in '{0}' state", Status));
            }
            Priority = priority;
            var response = _client.Release(_jobId, Priority, delay);
            if(response != ReleaseStatus.NotFound) {
                Status = WorkStatus.Released;
            }
            return response;
        }

        public void Bury() {
            throw new NotImplementedException();
        }

        public void Bury(uint priority) {
            throw new NotImplementedException();
        }

        public void Touch() {
            throw new NotImplementedException();
        }

        public void Dispose() {
            throw new NotImplementedException();
        }
    }
}