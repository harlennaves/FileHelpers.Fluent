﻿using System;
using System.Globalization;

using FileHelpers.Fluent.Core.Exceptions;

namespace FileHelpers.Fluent.Core.Converters
{
    public class FloatConverter : DecimalNumberBaseConverter
    {
        public FloatConverter() : base($"N{CultureInfo.InvariantCulture.NumberFormat.NumberDecimalDigits}")
        {

        }

        public FloatConverter(string format) :
            base(format)
        {

        }

        public override string FieldType => "float";

        public override object StringToField(string @from)
        {
            if (string.IsNullOrWhiteSpace(from))
                return 0.0F;

            float to = 0.0F;

            float.TryParse(from.Trim(),
                    NumberStyles.Number | NumberStyles.AllowExponent,
                    CultureInfo.InvariantCulture,
                    out to);

            if (!string.IsNullOrWhiteSpace(DecimalSeparator))
                return to;

            if (
                !float.TryParse(from.Trim(),
                    NumberStyles.Number | NumberStyles.AllowExponent,
                    CultureInfo.InvariantCulture,
                    out float res))
                throw new ConvertException(from, typeof(float));
            return to / Math.Pow(10, DecimalDigits);
        }
    }
}
