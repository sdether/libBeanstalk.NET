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

namespace Droog.Beanstalk.Client.Util {
    public static class TypeConverter {
        public static T As<T>(this object value) {
            var type = typeof(T);
            if(type == typeof(object)) {
                return (T)value;
            }

            // check if target type is nullable
            var nullableType = Nullable.GetUnderlyingType(type);
            if(nullableType != null) {
                if(value == null) {
                    return (T)value;
                }
                type = nullableType;
            }

            // check if type is enum and value is a value type
            if(type.IsEnum) {
                var valueType = value.GetType();
                if(valueType.IsValueType && (value is Byte || value is Int32 || value is SByte || value is Int16 || value is Int64 || value is UInt16 || value is UInt32 || value is UInt64)) {
                    return (T)Enum.ToObject(type, value);
                }
            }

            // check if value is string
            if(value is string) {
                if(type.IsEnum) {

                    // target type is enum
                    try {
                        return (T)Enum.Parse(type, (string)value, true);
                    } catch {
                        throw new InvalidCastException();
                    }
                }
            }

            // system type converter
            return (T)Convert.ChangeType(value, type);
        }
    }
}