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
using System.Collections.Generic;
using System.IO;

namespace Droog.Beanstalk.Client.Protocol {
    public class Response {
        private static readonly Dictionary<string, ResponseInfo> _responseLookup = new Dictionary<string, ResponseInfo>();

        static Response() {
            _responseLookup.Add("BAD_FORMAT", new ResponseInfo(ResponseStatus.BadFormat, false));
            _responseLookup.Add("BURIED", new ResponseInfo(ResponseStatus.Buried, false));
            _responseLookup.Add("DEADLINE_SOON", new ResponseInfo(ResponseStatus.DeadlineSoon, false));
            _responseLookup.Add("DELETED", new ResponseInfo(ResponseStatus.Deleted, false));
            _responseLookup.Add("DRAINING", new ResponseInfo(ResponseStatus.Draining, false));
            _responseLookup.Add("EXPECTED_CRLF", new ResponseInfo(ResponseStatus.ExpectedCrlf, false));
            _responseLookup.Add("FOUND", new ResponseInfo(ResponseStatus.Found, true));
            _responseLookup.Add("INSERTED", new ResponseInfo(ResponseStatus.Inserted, false));
            _responseLookup.Add("INTERNAL_ERROR", new ResponseInfo(ResponseStatus.InternalError, false));
            _responseLookup.Add("JOB_TOO_BIG", new ResponseInfo(ResponseStatus.JobTooBig, false));
            _responseLookup.Add("KICKED", new ResponseInfo(ResponseStatus.Kicked, false));
            _responseLookup.Add("NOT_FOUND", new ResponseInfo(ResponseStatus.NotFound, false));
            _responseLookup.Add("NOT_IGNORED", new ResponseInfo(ResponseStatus.NotIgnored, false));
            _responseLookup.Add("OK", new ResponseInfo(ResponseStatus.Ok, true));
            _responseLookup.Add("OUT_OF_MEMORY", new ResponseInfo(ResponseStatus.OutOfMemory, false));
            _responseLookup.Add("RELEASED", new ResponseInfo(ResponseStatus.Released, false));
            _responseLookup.Add("RESERVED", new ResponseInfo(ResponseStatus.Reserved, true));
            _responseLookup.Add("TIMED_OUT", new ResponseInfo(ResponseStatus.TimedOut, false));
            _responseLookup.Add("TOUCHED", new ResponseInfo(ResponseStatus.Touched, false));
            _responseLookup.Add("UNKNOWN_COMMAND", new ResponseInfo(ResponseStatus.UnknownCommand, false));
            _responseLookup.Add("USING", new ResponseInfo(ResponseStatus.Using, false));
            _responseLookup.Add("WATCHING", new ResponseInfo(ResponseStatus.Watching, false));
        }

        public readonly ResponseStatus Status;
        public readonly string[] Arguments;
        private readonly long _dataLength;
        private readonly MemoryStream _data;

        public Response(string response) {
            var tokens = response.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            ResponseInfo info = null;
            if(tokens.Length == 0) {
                throw new EmptyResponseException();
            }
            if(!_responseLookup.TryGetValue(tokens[0], out info)) {
                throw new UnknowResponseException(tokens[0]);
            }
            Arguments = new string[tokens.Length - 1];
            if(Arguments.Length > 0) {
                Array.Copy(tokens,1,Arguments,0,Arguments.Length);
            }
            Status = info.Status;
            if(!info.HasData) {
                return;
            }
            _dataLength = long.Parse(tokens[tokens.Length - 1]);
            _data = new MemoryStream((int)_dataLength);
        }

        public Stream Data {
            get {
                if(_data == null) {
                    return null;
                }
                _data.Position = 0;
                return _data;
            }
        }
        public long DataLength { get { return _dataLength; } }

        public void AddData(byte[] buffer, int offset, int count) {
            _data.Write(buffer, offset, count);
        }
    }
}