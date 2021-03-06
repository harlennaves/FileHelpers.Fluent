﻿using FileHelpers.Fluent.Core.Descriptors;

namespace FileHelpers.Fluent.Fixed
{
    public class FixedRecordDescriptor : RecordDescriptor
    {
        public FixedMode FixedMode { get; set; }

        public bool IgnoreEmptyLines { get; set; }

        public FixedRecordDescriptor()
        {
            FixedMode = FixedMode.ExactLength;
            IgnoreEmptyLines = true;
        }

        public FluentFixedEngine Build() =>
            new FluentFixedEngine(this);

        public override bool Equals(object obj)
        {
            var fixedObj = obj as FixedRecordDescriptor;

            if (fixedObj == null)
                return true;

            foreach (var field in Fields)
            {
                var newField = fixedObj.Fields.ContainsKey(field.Key) ? fixedObj.Fields[field.Key] : null;

                if (newField == null || !field.Equals(newField))
                    return false;
            }

            return
                    FixedMode == fixedObj.FixedMode
                    && IgnoreEmptyLines == fixedObj.IgnoreEmptyLines
                ;

        }
    }
}
