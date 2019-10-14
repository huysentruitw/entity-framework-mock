/*
 * Copyright 2017-2019 Wouter Huysentruit
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *   http://www.apache.org/licenses/LICENSE-2.0
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
using System.Data.SqlClient;
using System.Reflection;
using System.Runtime.Serialization;

namespace EntityFrameworkMock
{
    public static class SqlExceptionCreator
    {
        public static SqlException Create(string message, int errorCode)
        {
            SqlException exception = Instantiate<SqlException>();
            SetProperty(exception, "_message", message);

            var error = GetSqlError(errorCode);
            var errorCollection = GetSqlErrorCollection(error);

            SetProperty(exception, "_errors", errorCollection);
            return exception;
        }

        private static T Instantiate<T>() where T : class
        {
            return FormatterServices.GetUninitializedObject(typeof(T)) as T;
        }

        private static void SetProperty<T>(T targetObject, string fieldName, object value)
        {
            var field = typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            if (field == null)
            {
                throw new InvalidOperationException("No field with name " + fieldName);
            }

            field.SetValue(targetObject, value);
        }

        private static SqlErrorCollection GetSqlErrorCollection(params object[] errors)
        {
            var field = GetField<SqlErrorCollection>("errors")
                ?? GetField<SqlErrorCollection>("_errors")
                ?? throw new InvalidOperationException("No errors field found");

            var errorCollection = Instantiate<SqlErrorCollection>();
            if (field == null)
            {
                return errorCollection;
            }

            var errorsList = GetErrorsList(field.FieldType, errors);

            field.SetValue(errorCollection, errorsList);

            return errorCollection;
        }

        private static SqlError GetSqlError(int errorCode)
        {
            var field = GetField<SqlError>("number")
                ?? GetField<SqlError>("_number")
                ?? throw new InvalidOperationException("No number field found");

            var error = Instantiate<SqlError>();
            field.SetValue(error, errorCode);

            return error;
        }

        private static FieldInfo GetField<T>(string fieldName)
        {
            return typeof(T).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        }

        private static object GetErrorsList(Type type, object[] errors)
        {
            if (type == typeof(ArrayList))
            {
                return new ArrayList(errors);
            }

            if (type == typeof(List<object>))
            {
                return new List<object>(errors);
            }

            throw new InvalidOperationException("Error collections of type {type} are not supported");
        }
    }
}
