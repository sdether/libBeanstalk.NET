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

using System;
using System.IO;

namespace Droog.Beanstalk.Client.Protocol {
    public class RequestData {
        private readonly byte[] _bytes;
        private readonly Stream _stream;
        private long _remaining;
        private long _byteOffset;

        public RequestData(Stream stream, long length) {
            _stream = stream;
            _remaining = length;
        }

        public RequestData(byte[] bytes) {
            _bytes = bytes;
            _remaining = bytes.Length;
        }

        public bool HasData {
            get { return _remaining > 0; }
        }

        public int FillBuffer(byte[] buffer, int offset) {
            var count = Math.Min(buffer.Length - offset, (int)_remaining);
            if(_stream == null) {
                Array.Copy(_bytes, _byteOffset, buffer, offset, count);
                _byteOffset += count;
            } else {
                _stream.Read(buffer, offset, count);
            }
            offset += count;
            _remaining -= count;
            return offset;
        }
    }
}