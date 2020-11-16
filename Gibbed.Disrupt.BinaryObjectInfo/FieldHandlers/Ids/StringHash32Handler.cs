using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Gibbed.Disrupt.BinaryObjectInfo.FieldHandlers.Ids
{
    internal class StringHash32Handler : BaseHandler
    {
        protected override uint Hash(string text)
        {
            return FileFormats.Hashing.CRC32.Compute(text);
        }
    }
}
