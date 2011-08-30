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

using System.IO;
using System.Text;

namespace Droog.Beanstalk.Client {
    public class Job {
        private readonly uint _id;
        private readonly Stream _data;
        private readonly long _length;

        public Job(uint jobId, Stream data, long length) {
            _id = jobId;
            _data = data;
            _length = length;
        }

        public uint Id { get { return _id; } }
        public Stream Data { get { return _data; } }
        public long DataLength { get { return _length; } }
    }

    public class Job<T> {
        private readonly uint _jobId;
        private readonly T _data;

        public Job(uint jobId, T data) {
            _jobId = jobId;
            _data = data;
        }

        public uint JobId { get { return _jobId; } }
        public T Data { get { return _data; } }
    }


}