/* Copyright (c) 2014 Rick (rick 'at' gibbed 'dot' us)
 *
 * This software is provided 'as-is', without any express or implied
 * warranty. In no event will the authors be held liable for any damages
 * arising from the use of this software.
 *
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 *
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would
 *    be appreciated but is not required.
 *
 * 2. Altered source versions must be plainly marked as such, and must not
 *    be misrepresented as being the original software.
 *
 * 3. This notice may not be removed or altered from any source
 *    distribution.
 */

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Xml;
using System.Xml.XPath;
using CBR;

namespace Gibbed.Disrupt.BinaryObjectInfo
{
    public class FieldHandling
    {
        private static readonly Dictionary<FieldType, string> _TypeNames;
        private static readonly Dictionary<FieldType, IFieldHandler> _Handlers;

        static FieldHandling()
        {
            _TypeNames = new Dictionary<FieldType, string>();
            foreach (var value in Enum.GetValues(typeof(FieldType)).Cast<FieldType>())
            {
                _TypeNames.Add(value, Enum.GetName(typeof(FieldType), value));
            }

            _Handlers = new Dictionary<FieldType, IFieldHandler>
            {
                [FieldType.BinHex] = new FieldHandlers.BinHexHandler(),
                [FieldType.Boolean] = new FieldHandlers.BooleanHandler(),
                [FieldType.Int8] = new FieldHandlers.Ints.Int8Handler(),
                [FieldType.Int16] = new FieldHandlers.Ints.Int16Handler(),
                [FieldType.Int32] = new FieldHandlers.Ints.Int32Handler(),
                [FieldType.Int64] = new FieldHandlers.Ints.Int64Handler(),
                [FieldType.UInt8] = new FieldHandlers.UInts.UInt8Handler(),
                [FieldType.UInt16] = new FieldHandlers.UInts.UInt16Handler(),
                [FieldType.UInt32] = new FieldHandlers.UInts.UInt32Handler(),
                [FieldType.UInt64] = new FieldHandlers.UInts.UInt64Handler(),
                [FieldType.Float] = new FieldHandlers.FloatHandler(),
                [FieldType.Vector2] = new FieldHandlers.Vector2Handler(),
                [FieldType.Vector3] = new FieldHandlers.Vector3Handler(),
                [FieldType.Vector4] = new FieldHandlers.Vector4Handler(),
                [FieldType.Vector] = new FieldHandlers.VectorHandler(),
                [FieldType.VectorColor] = new FieldHandlers.VectorColorHandler(),
                [FieldType.VectorInt] = new FieldHandlers.VectorIntHandler(),
                [FieldType.Quaternion] = new FieldHandlers.Vector4Handler(),
                [FieldType.String] = new FieldHandlers.StringHandler(),
                [FieldType.Enum] = new FieldHandlers.EnumHandler(),
                [FieldType.StringId] = new FieldHandlers.Ids.StringIdHandler(),
                [FieldType.NoCaseStringId] = new FieldHandlers.Ids.NoCaseStringIdHandler(),
                [FieldType.PathId] = new FieldHandlers.Ids.PathIdHandler(),
                //[FieldType.Rml] = new FieldHandlers.RmlHandler(),
                [FieldType.Array32] = new FieldHandlers.Array32Handler(),
                [FieldType.StringHash32] = new FieldHandlers.Ids.StringHash32Handler(),
                [FieldType.StringHash64] = new FieldHandlers.Ids.StringHash64Handler()
            };
        }

        public static string GetTypeName(FieldType type)
        {
            if (_TypeNames.ContainsKey(type) == false)
            {
                throw new NotSupportedException("unknown type");
            }

            return _TypeNames[type];
        }

        public static byte[] Import(FieldType type, string text)
        {
            if (_Handlers.ContainsKey(type) == false)
            {
                throw new NotSupportedException(string.Format("no handler for {0}", type));
            }

            var serializer = _Handlers[type] as IValueHandler;
            if (serializer == null)
            {
                throw new NotSupportedException(string.Format("handler for {0} is not a value handler", type));
            }

            return serializer.Import(FieldType.Invalid, text);
        }

        public static byte[] Import(FieldType type, FieldType arrayType, XPathNavigator nav)
        {
            if (nav == null)
            {
                throw new ArgumentNullException("nav");
            }

            if (_Handlers.ContainsKey(type) == false)
            {
                throw new NotSupportedException(string.Format("no handler for {0}", type));
            }

            var serializer = _Handlers[type];
            return serializer.Import(arrayType, nav);
        }

        public static T Deserialize<T>(FieldType type, byte[] buffer)
        {
            return Deserialize<T>(type, buffer, 0, buffer.Length);
        }

        public static T Deserialize<T>(FieldType type, byte[] buffer, int offset, int count)
        {
            if (_Handlers.ContainsKey(type) == false)
            {
                throw new NotSupportedException(string.Format("no handler for {0}", type));
            }

            var serializer = _Handlers[type] as ValueHandler<T>;
            if (serializer == null)
            {
                throw new NotSupportedException(string.Format("handler for {0} is not a value handler", type));
            }

            int read;
            var value = serializer.Deserialize(buffer, offset, count, out read);

            if (read != count)
            {
                string name = type.ToString();

                throw new FormatException(string.Format("did not consume all data for {0} with type {1} (read {2}, total {3})", name, type.ToString(), read, count));
            }

            return value;
        }

        public static void Export(FieldType type,
                                  FieldType arrayType,
                                  byte[] buffer,
                                  int offset,
                                  int count,
                                  XmlWriter writer,
                                  out int read)
        {
            Export(type, arrayType, buffer, offset, count, writer, out read, 0);
        }

        public static void Export(FieldType type,
                                  FieldType arrayType,
                                  byte[] buffer,
                                  int offset,
                                  int count,
                                  XmlWriter writer,
                                  out int read,
                                  uint itemNo)
        {
            if (writer == null)
            {
                throw new ArgumentNullException("writer");
            }

            if (!_Handlers.ContainsKey(type))
            {
                throw new NotSupportedException(string.Format("no handler for {0}", type));
            }

            var serializer = _Handlers[type];
            serializer.Export(arrayType, buffer, offset, count, writer, out read);

            if (read != count)
            {
                string name = type.ToString();

                throw new FormatException(string.Format("did not consume all data for {0} with type {1} (read {2}, total {3})", name, type.ToString(), read, count));
            }
        }
    }

    public struct HashValue
    {
        public UInt32 CRC32;
        public UInt32 CRC32_R;
        public UInt32 FNV64_WD1;
        public UInt64 CRC64_WD2;
    }

    public enum HashType
    {
        None = 0,
        CRC32 = 1,
        StringHash32, CRC32R = 2,
        CRC64 = 3,
        HashType04, CRC64R = 4,
        FNV64 = 5,
        PathHash, FNV64_WD1 = 6,
        StringHash64, CRC64_WD2 = 7
    }

    public struct FieldInfo
    {
        public struct KeyInfo
        {
            public string type;
            public string name;
        }

        public struct ValueInfo
        {
            public string type;
            public string value;
            //public FieldType fieldType;
        }

        public KeyInfo key;
        public ValueInfo value;

        public static bool operator ==(FieldInfo a, FieldInfo b)
        {
            return a.Equals(b);
        }
        public static bool operator !=(FieldInfo a, FieldInfo b)
        {
            return !a.Equals(b);
        }
    }

    public static class HashHandler
    {
        public static HashValue ResolveString_old(string input)
        {
            return new HashValue
            {
                CRC32 = FileFormats.Hashing.CRC32.Compute(input),
                CRC32_R = FileFormats.Hashing.CRC32.Compute_R(input),
                FNV64_WD1 = FileFormats.Hashing.FNV64.Compute_WD1(input),
                CRC64_WD2 = FileFormats.Hashing.CRC64_WD2.Compute(input)
            };
        }

        public static ulong[] CollectHashes(string input)
        {
            ulong[] hashArray = new ulong[Enum.GetNames(typeof(HashType)).Length];

            hashArray[(int) HashType.CRC32] = (ulong) FileFormats.Hashing.CRC32.Compute(input);
            hashArray[(int) HashType.CRC32R] = (ulong) FileFormats.Hashing.CRC32.Compute_R(input);
            hashArray[(int) HashType.FNV64_WD1] = (ulong) FileFormats.Hashing.FNV64.Compute_WD1(input);
            hashArray[(int) HashType.CRC64_WD2] = (ulong) FileFormats.Hashing.CRC64_WD2.Compute_R(input);

            return hashArray;
        }
    }

    public static class TypeGuesser
    {
        public static bool FieldHandler(byte[] fieldValueBytes, out string fieldTypeString, out string fieldValueString)
        {
            bool outcome = false;

            string valueString = BitConverter.ToString(fieldValueBytes).Replace("-", "");

            FieldType fieldType = FieldType.BinHex;
            string fieldValue = valueString;

            FieldType r_outputFieldType = fieldType;
            string r_outputFieldValue = fieldValue;

            // determine valid field type
            if (TryParse(fieldValueBytes, out r_outputFieldType, out r_outputFieldValue) == true)
            {
                //Console.WriteLine("Found type");

                fieldType = r_outputFieldType;
                fieldValue = r_outputFieldValue;

                outcome = true;
            }

            fieldTypeString = FieldHandling.GetTypeName(fieldType);
            fieldValueString = fieldValue;

            //Console.WriteLine($"FieldHandler:\n\tType: {fieldTypeString}\n\tValue: {fieldValueString}");

            return outcome;
        }

        public static bool TryParse(byte[] value, out FieldType fieldType, out string fieldValue)
        {
            bool outcome = false;

            T Deserialize<T>(byte[] inputValue, FieldType inputType)
            {
                outcome = true;

                var output = FieldHandling.Deserialize<T>(inputType, inputValue);

                return output;
            }

            fieldType = FieldType.BinHex;
            fieldValue = "";

            byte preTrail = 0;
            byte trail = 0;

            if (value.Length > 1)
            {
                trail = value[value.Length - 1];

                if (value.Length > 2)
                {
                    preTrail = value[value.Length - 2];
                }
            }

            if (value.Length == 1)
            {
                if (value[0] > 1 &&
                    value[0] != 255)
                {
                    fieldType = FieldType.Int8;
                    fieldValue = Deserialize<sbyte>(value, fieldType).ToString();
                }
            }
            else if (value.Length == 2)
            {
                fieldType = FieldType.Int16;
                fieldValue = Deserialize<short>(value, fieldType).ToString();
            }
            else
            {
                //if (value.Length > 4 && trail == 0 && preTrail > 0x29 && preTrail != 0x40)
                if (value.Length > 4 && trail == 0 && preTrail > 0)
                {
                    //Utility.Log($"String field\n\tTrailing byte: {trail}\n\tPre-trailing byte: {preTrail}");
                    //Utility.Log($"{BitConverter.ToString(value).Replace("-", " ")}\n\tTrailing byte: {trail}\n\tPre-trailing byte: {preTrail}");

                    fieldType = FieldType.String;
                    fieldValue = Deserialize<string>(value, fieldType);
                }
            }

            fieldValue = fieldValue.ToString();

            //PostProcessField(fieldValue, fieldType, value.Length, out fieldValue, out fieldType);
            //fieldValue = FieldHandling.Deserialize<string>(fieldType, value);
            //outcome = true;

            return outcome;
        }

        public static void PostProcessField(string inputFieldValue, FieldType inputFieldType, int inputFieldValueLength, out string outputFieldValue, out FieldType outputFieldType)
        {
            outputFieldValue = inputFieldValue;
            outputFieldType = inputFieldType;

            FieldType fieldType = FieldType.BinHex;

            int r_fieldValue_int = 0;
            float r_fieldValue_float = 0;

            if (int.TryParse(inputFieldValue, out r_fieldValue_int) == true)
            {
                switch (inputFieldValueLength)
                {
                    case 1:
                        fieldType = FieldType.Int8;
                        break;
                    case 2:
                        fieldType = FieldType.Int16;
                        break;
                    case 4:
                        fieldType = FieldType.Int32;
                        break;
                    case 8:
                        fieldType = FieldType.Int64;
                        break;
                }

                outputFieldValue = r_fieldValue_int.ToString();
            }
            else if (float.TryParse(inputFieldValue, out r_fieldValue_float) == true)
            {
                fieldType = FieldType.Float;
                outputFieldValue = r_fieldValue_float.ToString();
            }

            outputFieldType = fieldType;
        }
    }
}
