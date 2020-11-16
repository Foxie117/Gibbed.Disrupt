using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Gibbed.Disrupt.BinaryObjectInfo.FieldHandlers.Ids
{
    internal class PathHashHandler : Ints.Int32Handler
    {
        protected int Hash32(string text)
        {
            return (int) FileFormats.Hashing.FNV64.Compute_WD1_R(text);
        }

        public override int Parse(string text)
        {
            if (text.StartsWith("0x") == false)
            {
                int output = 0;

                output = this.Hash32(text);

                Console.WriteLine($"{text} -> {output:X8}");

                return output;
            }

            int value;

            if (int.TryParse(text.Substring(2),
                              NumberStyles.AllowHexSpecifier,
                              CultureInfo.InvariantCulture,
                              out value) == false)
            {
                throw new FormatException("failed to parse hex Id");
            }

            return value;
        }

        public override string Compose(int value)
        {
            return "0x" + ((uint)value).ToString("X8", CultureInfo.InvariantCulture);
        }
    }
}
