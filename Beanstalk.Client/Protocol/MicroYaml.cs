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
    public class MicroYaml {
        public static IEnumerable<string> ParseList(Response response) {
            using(var reader = new StreamReader(response.Data)) {
                string line;
                while(null != (line = reader.ReadLine())) {
                    if (line.StartsWith("- ")) {
                        yield return line.Substring(2).Trim();
                    }
                }
            }
        }

        public static IDictionary<string,string> ParseDictionary(Response response) {
            var dictionary = new Dictionary<string, string>();
            using(var reader = new StreamReader(response.Data)) {
                string line;
                while(null != (line = reader.ReadLine())) {
                    if(line.StartsWith("---")) {
                        continue;
                    }
                    var pair = line.Split(new[] {": "}, 2, StringSplitOptions.RemoveEmptyEntries);
                    dictionary[pair[0]] = pair[1];
                }
            }
            return dictionary;
        }
    }
}
