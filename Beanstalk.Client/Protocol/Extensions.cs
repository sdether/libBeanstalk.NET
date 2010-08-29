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
using Droog.Beanstalk.Client.Net;

namespace Droog.Beanstalk.Client.Protocol {
    public static class Extensions {

        public static void Write(this Stream stream, byte[] buffer) {
            stream.Write(buffer, 0, buffer.Length);
        }

        public static void SendRequest(this ISocket socket, Request request, byte[] buffer) {
            var offset = 0;
            foreach(var info in request.GetData()) {
                while(info.HasData) {
                    offset = info.FillBuffer(buffer, offset);
                    if(offset < buffer.Length) {
                        continue;
                    }
                    socket.SendBuffer(buffer, offset);
                    offset = 0;
                }
            }
            if(offset == 0) {
                return;
            }
            socket.SendBuffer(buffer, offset);
        }

        private static void SendBuffer(this ISocket socket, byte[] buffer, int count) {
            var offset = 0;
            while(count > 0) {
                var sent = socket.Send(buffer, offset, count);
                offset += sent;
                count -= sent;
            }
        }

        public static Response ReceiveResponse(this ISocket socket, byte[] buffer) {

            // read status line
            var totalRead = 0;
            var lineEndIndex = 0;
            var carriageReturn = false;
            var read = 0;
            while(lineEndIndex == 0) {
                read = socket.Receive(buffer, totalRead, buffer.Length);
                if(read == -1) {
                    throw new WriteException();
                }

                // look for \r\n
                for(var i = totalRead; i < totalRead + read; i++) {
                    if(buffer[i] == '\r') {
                        carriageReturn = true;
                    } else if(carriageReturn && buffer[i] == '\n') {
                        lineEndIndex = i;
                        break;
                    }
                }
                totalRead += read;
            }
            var response = new Response(Encoding.ASCII.GetString(buffer, 0, lineEndIndex - 1));
            var expectedDataBytes = response.DataLength;
            if(expectedDataBytes == 0) {
                return response;
            }
            var offset = lineEndIndex + 1;
            int dataLength = 0;
            if(offset < read) {

                // there is some data in buffer
                var remaining = read - offset;
                dataLength = Math.Min((int)expectedDataBytes, remaining);
                response.AddData(buffer, offset, dataLength);
                expectedDataBytes -= remaining;
                offset += dataLength;
            }
            while(expectedDataBytes > 0) {
                read = socket.Receive(buffer, 0, buffer.Length);
                if(read == -1) {
                    throw new ReadException("Unexpected end of stream");
                }
                dataLength = Math.Min((int)expectedDataBytes, read);
                response.AddData(buffer, 0, dataLength);
                expectedDataBytes -= read;
                offset = dataLength;
            }
            var tail = new byte[2];
            var tailOffset = 0;
            var tailCount = 2;
            if(expectedDataBytes == -1) {
                tailOffset = 1;
                tailCount = 1;
                tail[0] = buffer[offset];
            } else if(expectedDataBytes == -2) {
                tailOffset = 2;
                tailCount = 0;
                tail[0] = buffer[offset];
                tail[1] = buffer[offset + 1];
            }
            if(tailCount > 0) {
                socket.Receive(tail, tailOffset, tailCount);
            }
            if(tail[0] != '\r' || tail[1] != '\n') {
                throw new ReadException("Expected trailing CrLf");
            }
            return response;
        }

        public static ReleaseStatus ToReleaseStatus(this ResponseStatus status) {
            try {
                return (ReleaseStatus)Enum.Parse(typeof(ReleaseStatus), status.ToString());
            } catch {
                throw new InvalidReleaseStatusException(status);
            }
        }

        public static ReservationStatus ToReservationStatus(this ResponseStatus status) {
            try {
                return (ReservationStatus)Enum.Parse(typeof(ReservationStatus), status.ToString());
            } catch {
                throw new InvalidReservationStatusException(status);
            }
        }
    }
}
