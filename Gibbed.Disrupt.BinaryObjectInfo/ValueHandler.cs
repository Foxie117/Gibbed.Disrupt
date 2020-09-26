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

using System.Xml;
using System.Xml.XPath;

namespace Gibbed.Disrupt.BinaryObjectInfo
{
    internal abstract class ValueHandler<T> : IFieldHandler, IValueHandler
    {
        public abstract byte[] Serialize(T value);

        public abstract T Parse(string text);

        public byte[] Import(FieldType arrayFieldType, string text)
        {
            var value = this.Parse(text);
            var bytes = this.Serialize(value);
            return bytes;
        }

        public byte[] Import(FieldType arrayFieldType, XPathNavigator nav)
        {
            var value = this.Parse(nav.Value);
            var bytes = this.Serialize(value);
            return bytes;
        }

        public abstract T Deserialize(byte[] buffer, int offset, int count, out int read);

        public abstract string Compose(T value);

        public string Compose(byte[] data, int offset, int count, out int read)
        {
            var value = this.Deserialize(data, offset, count, out read);
            return this.Compose(value);
        }

        public void Export(FieldType arrayFieldType,
                           byte[] data,
                           int offset,
                           int count,
                           XmlWriter writer,
                           out int read)
        {
            var value = this.Deserialize(data, offset, count, out read);

            var outStr = this.Compose(value);

            writer.WriteString(outStr);
        }
    }
}
