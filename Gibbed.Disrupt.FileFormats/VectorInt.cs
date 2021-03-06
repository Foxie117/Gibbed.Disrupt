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
using System.Linq;

namespace Gibbed.Disrupt.FileFormats
{
    public struct VectorInt : ICloneable
    {
        public List<int> points;

        public VectorInt(List<int> points)
        {
            this.points = points;
        }

        public override string ToString()
        {
            return string.Join(",", points);
        }

        public override bool Equals(object obj)
        {
            if (obj == null || obj.GetType() != GetType())
            {
                return false;
            }

            return (VectorInt)obj == this;
        }

        public static bool operator !=(VectorInt a, VectorInt b)
        {
            return !a.points.SequenceEqual(b.points);
        }

        public static bool operator ==(VectorInt a, VectorInt b)
        {
            return a.points.SequenceEqual(b.points);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = 17;
                foreach (var point in points)
                {
                    hash = hash * 23 + point.GetHashCode();
                }
                return hash;
            }
        }

        public object Clone()
        {
            return new VectorInt(points);
        }
    }
}
