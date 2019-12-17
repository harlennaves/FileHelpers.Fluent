﻿using FileHelpers.Fluent.Core.Descriptors;
using FileHelpers.Fluent.Core.Events;
using FileHelpers.Fluent.Core.Exceptions;
using FileHelpers.Fluent.Core.Extensions;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileHelpers.Fluent.Fixed
{
    public sealed class FluentFixedEngine : FluentEventEngineBase
    {
        public FluentFixedEngine(IRecordDescriptor descriptor, Encoding encoding = null) : base(descriptor, encoding)
        {
        }

        #region Private Methods
        private void CheckFieldArrayDescriptor(string fieldName, IArrayFieldInfoDescriptor recordInfo)
        {
            if (recordInfo.ArrayLength <= 0)
                throw new BadFluentConfigurationException($"The property {fieldName} must be the {nameof(recordInfo.ArrayLength)} length greater than 0");

            if (recordInfo.ArrayItemLength <= 0)
                throw new BadFluentConfigurationException($"The property {fieldName} must be the {nameof(recordInfo.ArrayItemLength)} length greater than 0");

            if (recordInfo.ArrayItemLength > recordInfo.ArrayLength)
                throw new BadFluentConfigurationException($"The {nameof(recordInfo.ArrayLength)} of property {fieldName} must be greater than {nameof(recordInfo.ArrayItemLength)}");

            if ((recordInfo.ArrayLength % recordInfo.ArrayItemLength) != 0)
                throw new BadFluentConfigurationException($"The remainder of {nameof(recordInfo.ArrayLength)} division by {nameof(recordInfo.ArrayItemLength)} can not be different than 0");

            var arrayRecordInfo = recordInfo as IRecordDescriptor;

            if (arrayRecordInfo == null)
                throw new BadFluentConfigurationException($"The property {fieldName} is not an array builder");

            CheckDescriptor(arrayRecordInfo, true);
        }

        private void CheckFieldDescriptor(string fieldName, IFixedFieldInfoDescriptor fieldDescriptor)
        {
            if (fieldDescriptor == null)
                throw new ArgumentNullException(nameof(fieldDescriptor));
            if (fieldDescriptor.Length <= 0)
                throw new BadFluentConfigurationException($"The property {fieldName} must be a length gearter than 0");
        }

        #endregion

        #region Protected Override Methods

        protected override void CheckFieldDescriptor(string fieldName, IFieldInfoTypeDescriptor fieldDescriptor)
        {
            if (fieldDescriptor.IsArray)
            {
                CheckFieldArrayDescriptor(fieldName, fieldDescriptor as IArrayFieldInfoDescriptor);
                return;
            }
            CheckFieldDescriptor(fieldName, fieldDescriptor as IFixedFieldInfoDescriptor);
        }

        protected override async Task<ExpandoObject> ReadLineAsync(string currentLine, IRecordDescriptor descriptor) =>
            await Task.Run(() =>
            {
                var item = new ExpandoObject();

                var offset = 0;
                foreach (KeyValuePair<string, IFieldInfoTypeDescriptor> fieldInfoTypeDescriptor in descriptor.Fields)
                {
                    if (fieldInfoTypeDescriptor.Value.IsArray)
                    {
                        item.InternalTryAdd(fieldInfoTypeDescriptor.Key,
                            ((IArrayFieldInfoDescriptor)fieldInfoTypeDescriptor.Value).StringToArray(currentLine,
                                ref offset));
                        continue;
                    }

                    item.InternalTryAdd(fieldInfoTypeDescriptor.Key,
                        ((IFixedFieldInfoDescriptor)fieldInfoTypeDescriptor.Value).StringToRecord(currentLine, ref offset));
                }

                return item;
            });

        #endregion

        public override ExpandoObject[] ReadBuffer(byte[] buffer) =>
            ReadBufferAsync(buffer).GetAwaiter().GetResult();

        public override Task<ExpandoObject[]> ReadBufferAsync(byte[] buffer)
        {
            using (var stream = new MemoryStream(buffer))
                using (var streamReader = new StreamReader(stream, Encoding))
                    return ReadStreamAsync(streamReader);
        }

        public override ExpandoObject[] ReadStream(StreamReader reader) =>
            ReadStreamAsync(reader).GetAwaiter().GetResult();

        public override async Task<ExpandoObject[]> ReadStreamAsync(StreamReader reader)
        {
            IList<ExpandoObject> items = new List<ExpandoObject>();

            string currentLine = await reader.ReadLineAsync();
            int currentLineNumber = 1;
            while (currentLine != null)
            {
                if (!string.IsNullOrWhiteSpace(currentLine))
                {
                    var beforeReadArgs = OnBeforeReadRecord(currentLine, currentLineNumber);
                    if (!beforeReadArgs.SkipRecord)
                    {
                        if (beforeReadArgs.LineChanged)
                            currentLine = beforeReadArgs.Line;

                        ExpandoObject item = await ReadLineAsync(currentLine, Descriptor);

                        var afterReadArgs = OnAfterReadRecord(currentLine, currentLineNumber, item);

                        items.Add(afterReadArgs.Record);
                    }
                }
                currentLineNumber++;
                currentLine = await reader.ReadLineAsync();
            }

            return items.ToArray();
        }

        public override ExpandoObject[] ReadString(string source)
        {
            if (source == null)
                source = string.Empty;

            using (var stream = new MemoryStream(Encoding.GetBytes(source)))
            {
                using (var streamReader = new StreamReader(stream))
                {
                    return ReadStream(streamReader);
                }
            }
        }

        public override void WriteStream(TextWriter writer, IEnumerable<ExpandoObject> records, bool flush = true) =>
            WriteStreamAsync(writer, records, flush).GetAwaiter().GetResult();

        public override async Task WriteStreamAsync(TextWriter writer, IEnumerable<ExpandoObject> records, bool flush = true)
        {
            writer.NewLine = Environment.NewLine;
            var lineNumber = 1;
            foreach (ExpandoObject expandoObject in records)
            {
                var beforeWriteArgs = OnBeforeWriteRecord(expandoObject, lineNumber);

                var record = (beforeWriteArgs.LineChanged ? beforeWriteArgs.Record : expandoObject) as IDictionary<string, object>;

                if (record == null || beforeWriteArgs.SkipRecord)
                {
                    lineNumber++;
                    continue;
                }
                var sb = new StringBuilder();
                foreach (KeyValuePair<string, object> keyValuePair in record)
                {
                    if (!Descriptor.Fields.TryGetValue(keyValuePair.Key, out IFieldInfoTypeDescriptor fieldDescriptor))
                        throw new Exception($"The field {keyValuePair.Key} is not configured");

                    if (fieldDescriptor.IsArray)
                    {
                        sb.Append(((IArrayFieldInfoDescriptor)fieldDescriptor).ArrayToString(
                            (IEnumerable<dynamic>)keyValuePair.Value));
                        continue;
                    }

                    sb.Append(((IFixedFieldInfoDescriptor)fieldDescriptor).RecordToString(keyValuePair.Value));
                }

                var afterWriteArgs = OnAfterWriteRecord(sb.ToString(), lineNumber, expandoObject);

                await writer.WriteLineAsync(afterWriteArgs.Line);

                if (flush)
                    await writer.FlushAsync();
                lineNumber++;
            }
        }

        public override string WriteString(IEnumerable<ExpandoObject> records)
        {
            var sb = new StringBuilder();
            using (var writer = new StringWriter(sb))
            {
                WriteStream(writer, records);
                return sb.ToString();
            }
        }
    }
}
