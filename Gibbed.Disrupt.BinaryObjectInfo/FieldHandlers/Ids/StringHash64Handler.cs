using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;

namespace Gibbed.Disrupt.BinaryObjectInfo.FieldHandlers.Ids
{
    internal class StringHash64Handler : Ints.Int64Handler
    {
        protected ulong Hash(string text)
        {
            return FileFormats.Hashing.CRC64_WD2.Compute(text);
        }

        public override long Parse(string text)
        {
            if (text.StartsWith("0x") == false)
            {
                return (long) this.Hash(text);
            }

            long value;

            if (long.TryParse(text.Substring(2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out value) == false)
            {
                throw new FormatException("failed to parse hex Id");
            }

            return value;
        }

        public override string Compose(long value)
        {
            return "0x" + ((ulong)value).ToString("X16", CultureInfo.InvariantCulture);
        }
    }
}
