﻿using System;
using System.Collections.Concurrent;
using System.Globalization;

namespace FileHelpers.Fluent.Core.Converters
{
    public static class ConverterFactory
    {
        private static readonly ConcurrentDictionary<string, ConverterBase> converterInstances = new ConcurrentDictionary<string, ConverterBase>();

        public static ConverterBase GetConverter(Type type, string format)
        {
            string instanceName = type.FullName;
            if (!string.IsNullOrWhiteSpace(format))
                instanceName += $"_{format}";

            if (converterInstances.TryGetValue(instanceName, out ConverterBase converterInstance))
                return converterInstance;

            converterInstance = (string.IsNullOrWhiteSpace(format)
                ? Activator.CreateInstance(type)
                : Activator.CreateInstance(type, format)) as ConverterBase;

            converterInstances.TryAdd(instanceName, converterInstance);
            return converterInstance;
        }

        public static ConverterBase GetDefaultConverter(Type type, string pattern)
        {
            string instanceName = type.FullName;
            if (converterInstances.TryGetValue(instanceName, out ConverterBase converterInstance))
                return converterInstance;
            
            switch (type.Name)
            {
                case "Boolean":
                    converterInstance = string.IsNullOrWhiteSpace(pattern) 
                                            ? new BooleanConverter() 
                                            : new BooleanConverter(pattern);
                    break;
                case "Byte":
                    converterInstance = new ByteConverter();
                    break;
                case "DateTime":
                    converterInstance = string.IsNullOrWhiteSpace(pattern) 
                                            ? new DateTimeConverter(CultureInfo.InvariantCulture.DateTimeFormat.ShortDatePattern) 
                                            : new DateTimeConverter(pattern);
                    break;
                case "Decimal":
                    converterInstance = string.IsNullOrWhiteSpace(pattern) 
                                            ? new DecimalConverter()
                                            : new DecimalConverter(pattern);
                    break;
                case "Double":
                    converterInstance = string.IsNullOrWhiteSpace(pattern)
                                            ? new DoubleConverter()
                                            : new DoubleConverter(pattern);
                    break;
                case "Short":
                    converterInstance = new ShortConverter();
                    break;
                case "Int32":
                    converterInstance = new IntegerConverter();
                    break;
                case "Int64":
                    converterInstance = new LongConverter();
                    break;
                case "Single":
                    converterInstance = string.IsNullOrWhiteSpace(pattern)
                                            ? new FloatConverter()
                                            : new FloatConverter(pattern);
                    break;
                case "UShort":
                    converterInstance = new UShortConverter();
                    break;
                case "UInt32":
                    converterInstance = new UIntegerConverter();
                    break;
                case "UInt64":
                    converterInstance = new ULongConverter();
                    break;
            }

            if (converterInstance != null)
                converterInstances.TryAdd(instanceName, converterInstance);

            return converterInstance;
        }
    }
}
