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

namespace Droog.Beanstalk.Client.Protocol {

    [Flags]
    public enum ResponseStatus {
        BadFormat = 0x1,
        Buried = 0x2,
        DeadlineSoon = 0x4,
        Deleted = 0x8,
        Draining = 0x10,
        ExpectedCrlf = 0x20,
        Found = 0x40,
        Inserted = 0x80,
        InternalError = 0x100,
        JobTooBig = 0x200,
        Kicked = 0x400,
        NotFound = 0x800,
        NotIgnored = 0x1000,
        Ok = 0x2000,
        OutOfMemory = 0x4000,
        Released = 0x8000,
        Reserved = 0x10000,
        TimedOut = 0x20000,
        Touched = 0x40000,
        UnknownCommand = 0x80000,
        Using = 0x100000,
        Watching = 0x200000,
    }
}