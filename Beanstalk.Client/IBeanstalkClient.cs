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

namespace Droog.Beanstalk.Client
{
    public interface IBeanstalkClient : IDisposable
    {

        // Connection state related
        bool IsDisposed { get; }
        
        // Consumer related
        string CurrentTube { get; set; }

        BeanstalkDefaults Defaults { get; }

        PutResponse Put(uint priority, TimeSpan delay, TimeSpan timeToRun, Stream request, long length);

        // Producer related
        IWatchedTubeCollection WatchedTubes { get; }

        Job Reserve();
        Job Reserve(TimeSpan timeout);
        ReservationStatus TryReserve(TimeSpan timeout, out Job job);
        bool Delete(uint jobId);
        ReleaseStatus Release(uint jobId, uint priority, TimeSpan delay);
        bool Bury(uint jobId, uint priority);
        bool Touch(uint jobId);

        // Misc
        Job Peek(uint jobId);
        Job PeekReady();
        Job PeekDelayed();
        Job PeekBuried();
        uint Kick(uint bound);
        JobStats GetJobStats(uint jobId);
        TubeStats GetTubeStats(string tube);
        ServerStats GetServerStats();
        IEnumerable<string> GetTubes();
    }
}