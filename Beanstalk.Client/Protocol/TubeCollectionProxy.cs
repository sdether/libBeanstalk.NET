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
using System.Collections;
using System.Collections.Generic;

namespace Droog.Beanstalk.Client.Protocol {
    public class TubeCollectionProxy : IWatchedTubeCollection {

        private readonly IWatchedTubeClient _client;
        private readonly HashSet<string> _tubes;

        public TubeCollectionProxy(IWatchedTubeClient client, IEnumerable<string> tubes) {
            _client = client;
            _tubes = new HashSet<string>(tubes);
        }

        public IEnumerator<string> GetEnumerator() {
            return _tubes.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }

        public void Add(string tube) {
            _tubes.Add(tube);
            var watched = _client.Watch(tube);
            if(watched != _tubes.Count) {
                Refresh();
            }
        }

        public bool Contains(string tube) {
            return _tubes.Contains(tube);
        }

        public void CopyTo(string[] array, int arrayIndex) {
            _tubes.CopyTo(array, arrayIndex);
        }

        public bool Remove(string tube) {
            var removed = _tubes.Remove(tube);
            if(!removed) {
                return false;
            }
            var watched = _client.Ignore(tube);
            if(watched != _tubes.Count || _tubes.Count == 0) {
                Refresh();
            }
            return true;
        }

        public int Count {
            get { return _tubes.Count; }
        }

        public void Refresh() {
            _tubes.Clear();
            foreach(var tube in _client.ListWatchedTubes()) {
                _tubes.Add(tube);
            }
        }
    }
}
