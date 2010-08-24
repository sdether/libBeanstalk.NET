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
using System.IO;
using System.Text;

namespace Droog.Beanstalk.Client.Protocol {
    public class Request {

        private static readonly byte[][] _commands = new[] {
            Encoding.ASCII.GetBytes("put"),
            Encoding.ASCII.GetBytes("use"),
            Encoding.ASCII.GetBytes("reserve"),
            Encoding.ASCII.GetBytes("reserve-with-timeout"),
            Encoding.ASCII.GetBytes("delete"),
            Encoding.ASCII.GetBytes("release"),
            Encoding.ASCII.GetBytes("bury"),
            Encoding.ASCII.GetBytes("touch"),
            Encoding.ASCII.GetBytes("watch"),
            Encoding.ASCII.GetBytes("ignore"),
            Encoding.ASCII.GetBytes("peek"),
            Encoding.ASCII.GetBytes("peek-ready"),
            Encoding.ASCII.GetBytes("peek-delayed"),
            Encoding.ASCII.GetBytes("peek-buried"),
            Encoding.ASCII.GetBytes("kick"),
            Encoding.ASCII.GetBytes("stats-job"),
            Encoding.ASCII.GetBytes("stats-tube"),
            Encoding.ASCII.GetBytes("stats"),
            Encoding.ASCII.GetBytes("list-tubes"),
            Encoding.ASCII.GetBytes("list-tubes-watched"),
        };

        private static readonly byte[] _linefeed = new[] { (byte)'\r', (byte)'\n' };

        public static Request Create(RequestCommand command) {
            return new Request(command);
        }

        private readonly RequestCommand _command;
        private readonly MemoryStream _request = new MemoryStream();
        private Stream _data;
        private long _dataLength;
        private bool _done;
        private ResponseStatus _expectedStatuses;

        private Request(RequestCommand command) {
            _command = command;
            _request.Write(_commands[(int)command]);
        }

        public RequestCommand Command { get { return _command; } }
        public ResponseStatus ExpectedStatuses { get { return _expectedStatuses; } }

        public Request AppendArgument(string arg) {
            ThrowIfDone();
            _request.WriteByte((byte)' ');
            _request.Write(Encoding.ASCII.GetBytes(arg));
            return this;
        }

        public Request AppendArgument(uint arg) {
            ThrowIfDone();
            _request.WriteByte((byte)' ');
            _request.Write(Encoding.ASCII.GetBytes(arg.ToString()));
            return this;
        }

        public Request AppendArgument(TimeSpan arg) {
            ThrowIfDone();
            _request.WriteByte((byte)' ');
            _request.Write(Encoding.ASCII.GetBytes(((uint)arg.TotalSeconds).ToString()));
            return this;
        }

        public Request WithData(Stream data, long dataLength) {
            ThrowIfDone();
            _data = data;
            _dataLength = dataLength;
            return AppendArgument((uint)_dataLength);
        }

        public Request ExpectStatuses(ResponseStatus statuses) {
            ThrowIfDone();
            _expectedStatuses = statuses;
            return this;
        }

        public RequestData[] GetData() {
            ThrowIfDone();
            _done = true;
            _request.Write(_linefeed);
            _request.Seek(0, SeekOrigin.Begin);
            if(_data == null) {
                return new[] { new RequestData(_request, _request.Length) };
            }
            if(_data.CanSeek) {
                _data.Seek(0, SeekOrigin.Begin);
            }
            return new[] { new RequestData(_request, _request.Length), new RequestData(_data, _dataLength), new RequestData(_linefeed) };
        }

        private void ThrowIfDone() {
            if(_done) {
                throw new InvalidOperationException("cannot append request after it has been converted to streams");
            }
        }
    }
}