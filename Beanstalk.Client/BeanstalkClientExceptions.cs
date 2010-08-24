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
using Droog.Beanstalk.Client.Protocol;

namespace Droog.Beanstalk.Client {
    public abstract class BeanstalkClientExceptions : Exception {
        protected BeanstalkClientExceptions() { }
        protected BeanstalkClientExceptions(string message) : base(message) { }
        protected BeanstalkClientExceptions(string message, Exception exception) : base(message, exception) { }
    }

    public class EmptyResponseException : ConnectionException { }
    public abstract class ConnectionException : BeanstalkClientExceptions {
        protected ConnectionException() { }
        protected ConnectionException(string message) : base(message) { }

    }
    public class ReadException : ConnectionException {
        public ReadException(string message) : base(message) { }
    }
    public class WriteException : ConnectionException { }
    public class DeadlineSoonException : BeanstalkClientExceptions { }
    public class ShouldNeverHappenException : BeanstalkClientExceptions { }

    public class ConnectException : BeanstalkClientExceptions {
        public ConnectException(Exception exception)
            : base("Unable to Connect to Beanstalk server", exception) {
        }
    }

    public class UnknowResponseException : BeanstalkClientExceptions {
        public readonly string Response;
        public UnknowResponseException(string response)
            : base(string.Format("Response '{0}' is not supported by this client", response)) {
            Response = response;
        }
    }

    public class InvalidStatusException : BeanstalkClientExceptions {
        public readonly RequestCommand Command;
        public readonly ResponseStatus Status;

        public InvalidStatusException(RequestCommand command, ResponseStatus status)
            : base(string.Format("Response '{0}' is illegal for command '{1}'", status, command)) {
            Command = command;
            Status = status;
        }
    }

    public class PutFailedException : BeanstalkClientExceptions {
        public readonly ResponseStatus Status;

        public PutFailedException(ResponseStatus status)
            : base(string.Format("Put failed with response '{0}'", status)) {
            Status = status;
        }
    }
}
