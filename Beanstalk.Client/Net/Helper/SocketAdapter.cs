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
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Droog.Beanstalk.Client.Net.Helper {
    public class SocketAdapter : ISocket {

        public static ISocket Open(string host, int port, TimeSpan connectTimeout) {
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
        }

        public static ISocket Open(IPAddress address, int port, TimeSpan connectTimeout) {
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
        }

        private readonly TcpClient _tcpClient;

        public SocketAdapter(TcpClient tcpClient) {
            _tcpClient = tcpClient;
        }

        public void Dispose() {
            _tcpClient.Close();
        }

        public bool Connected {
            get { return _tcpClient.Connected; }
        }

        public int Send(byte[] buffer, int offset, int size) {
            return _tcpClient.Client.Send(buffer, offset, size, SocketFlags.None);
        }

        public int Receive(byte[] buffer, int offset, int size) {
            return _tcpClient.Client.Receive(buffer, offset, size, SocketFlags.None);
        }
    }
}