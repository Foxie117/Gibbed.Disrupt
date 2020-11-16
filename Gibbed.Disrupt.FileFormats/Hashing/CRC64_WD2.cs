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

namespace Gibbed.Disrupt.FileFormats.Hashing
{
    public static class CRC64_WD2
    {
        public static ulong Compute(string value)
        {
            string str = value.Replace("/", "\\").ToLower();

            ulong hash64 = 0xCBF29CE484222325;

            foreach (char t in str)
            {
                hash64 *= (ulong)0x100000001B3;
                hash64 ^= (ulong)t;
            }

            return hash64 & 0x1FFFFFFFFFFFFFFF | 0xA000000000000000;
        }

        public static ulong Compute_R(string value)
        {
            ulong hash = Compute(value);

            byte[] bytes = BitConverter.GetBytes(hash);

            Array.Reverse(bytes);

            return BitConverter.ToUInt64(bytes, 0);
        }
    }
}
