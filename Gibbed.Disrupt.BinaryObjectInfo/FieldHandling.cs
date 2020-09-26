﻿/* Copyright (c) 2014 Rick (rick 'at' gibbed 'dot' us)
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
                [FieldType.Array32] = new FieldHandlers.Array32Handler()
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
}
