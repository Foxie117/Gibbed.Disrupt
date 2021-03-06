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
using System.Text;

namespace Gibbed.Disrupt.FileFormats
{
    public static class FileDetection
    {
        public static Tuple<string, string> Detect(byte[] guess, int read)
        {
            if (read == 0)
            {
                return new Tuple<string, string>("null", null);
            }

            if (read >= 5 &&
                guess[0] == 'M' &&
                guess[1] == 'A' &&
                guess[2] == 'G' &&
                guess[3] == 'M' &&
                guess[4] == 'A')
            {
                return new Tuple<string, string>("ui", "mgb");
            }

            if (read >= 3 &&
                guess[0] == 'B' &&
                guess[1] == 'I' &&
                guess[2] == 'K')
            {
                return new Tuple<string, string>("gfx", "bik");
            }

            if (read >= 3 &&
                guess[0] == 'U' &&
                guess[1] == 'E' &&
                guess[2] == 'F')
            {
                return new Tuple<string, string>("ui", "feu");
            }

            if (read >= 3 &&
                guess[0] == 0 &&
                guess[1] == 0 &&
                guess[2] == 0xFF)
            {
                return new Tuple<string, string>("misc", "maybe.rml");
            }

            if (read >= 8 &&
                guess[4] == 'h' &&
                guess[5] == 'M' &&
                guess[6] == 'v' &&
                guess[7] == 'N')
            {
                return new Tuple<string, string>("gfx", "hMvN");
            }

            if (read >= 8 &&
                guess[4] == 'Q' &&
                guess[5] == 'E' &&
                guess[6] == 'S' &&
                guess[7] == 0)
            {
                return new Tuple<string, string>("game", "cseq");
            }

            if (read >= 20 &&
                guess[16] == 'W' &&
                guess[17] == 0xE0 &&
                guess[18] == 0xE0 &&
                guess[19] == 'W')
            {
                return new Tuple<string, string>("gfx", "hkx");
            }

            if (read >= 2 && guess[0] == 'p' && guess[1] == 'A')
            {
                return new Tuple<string, string>("animations", "dpax");
            }

            if ((read >= 20 && guess[16] == 'S' &&
                guess[17] == 't' &&
                guess[18] == 'r' &&
                guess[19] == 'm') || (read >= 20 && guess[16] == 'm' &&
                guess[17] == 'r' &&
                guess[18] == 't' &&
                guess[19] == 'S'))
            {
                return new Tuple<string, string>("strm", "bin");
            }

            if (read >= 4)
            {
                uint magic = BitConverter.ToUInt32(guess, 0);

                if (magic == 0x00584254 || magic == 0x54425800) // '\0XBT'
                {
                    return new Tuple<string, string>("gfx", "xbt");
                }

                if (magic == 0x4D455348) // 'MESH'
                {
                    return new Tuple<string, string>("gfx", "xbg");
                }

                if (magic == 0x54414D00 || magic == 0x004D4154) // '\0MAT'
                {
                    return new Tuple<string, string>("gfx", "material.bin");
                }

                if (magic == 0x53504B02) // 'SPK\2'
                {
                    return new Tuple<string, string>("sfx", "spk");
                }

                if (magic == 0x00032A02)
                {
                    return new Tuple<string, string>("sfx", "sbao");
                }

                if (magic == 0x4643626E) // 'FCbn'
                {
                    return new Tuple<string, string>("game", "fcb");
                }

                if (magic == 0x534E644E) // 'SNdN'
                {
                    return new Tuple<string, string>("game", "rnv");
                }

                if (magic == 0x474E5089) // 'PNG\x89'
                {
                    return new Tuple<string, string>("gfx", "png");
                }

                if (magic == 0x4D564D00)
                {
                    return new Tuple<string, string>("gfx", "MvN");
                }

                if (magic == 0x61754C1B)
                {
                    return new Tuple<string, string>("scripts", "luab");
                }

                if (magic == 0x47454F4D || magic == 1297040711)
                {
                    return new Tuple<string, string>("gfx", "xbg");
                }

                if (magic == 0x00014C53)
                {
                    return new Tuple<string, string>("languages", "loc");
                }

                if (magic == 175074913)
                {
                    return new Tuple<string, string>("annotation", "ano");
                }

                if (magic == 1112818504 || magic == 1212372034)
                {
                    return new Tuple<string, string>("cbatch", "cbatch");
                }

                if (magic == 1281970290)
                {
                    return new Tuple<string, string>("lightprobe", "lipr.bin");
                }

                if (magic == 1299591697)
                {
                    return new Tuple<string, string>("move", "bin");
                }

                if (magic == 1397508178)
                {
                    return new Tuple<string, string>("roadresources", "hgfx");
                }

                if (magic == 1196247376)
                {
                    return new Tuple<string, string>("gfx", "xbgmip");
                }

                if (magic == 1397901394 || magic == 1380471379)
                {
                    return new Tuple<string, string>("srhr", "bin");
                }

                if (magic == 1397902418 || magic == 1380733523)
                {
                    return new Tuple<string, string>("srlr", "bin");
                }

                if (magic == 1396921426 || magic == 1381253971)
                {
                    return new Tuple<string, string>("sctr", "bin");
                }

                if (magic == 1731347019)
                {
                    return new Tuple<string, string>("bink", "bik");
                }

                if (magic == 1414677829 || magic == 1162170964)
                {
                    return new Tuple<string, string>("tree", "bin");
                }

                if (magic == 1397508178 || magic == 1380469843)
                {
                    return new Tuple<string, string>("rhls", "bin");
                }

                if (magic == 1714503984)
                {
                    return new Tuple<string, string>("dialog", "stimuli.dsc.pack");
                }

                if (magic == 1346981191)
                {
                    return new Tuple<string, string>("pimg", "bin");
                }

                if (magic == 1163084098)
                {
                    return new Tuple<string, string>("wlu", "fcb");
                }

                if (magic == 14492 || magic == 2620915712)
                {
                    return new Tuple<string, string>("eight", "bin");
                }
            }

            string text = Encoding.ASCII.GetString(guess, 0, read);

            if (read >= 3 && text.StartsWith("-- ") == true)
            {
                return new Tuple<string, string>("scripts", "lua");
            }

            if (read >= 6 && text.StartsWith("<root>") == true)
            {
                return new Tuple<string, string>("misc", "root.xml");
            }

            if (read >= 9 && text.StartsWith("<package>") == true)
            {
                return new Tuple<string, string>("ui", "mbg.desc");
            }

            if (read >= 12 && text.StartsWith("<NewPartLib>") == true)
            {
                return new Tuple<string, string>("misc", "NewPartLib.xml");
            }

            if (read >= 14 && text.StartsWith("<BarkDataBase>") == true)
            {
                return new Tuple<string, string>("misc", "BarkDataBase.xml");
            }

            if (read >= 13 && text.StartsWith("<BarkManager>") == true)
            {
                return new Tuple<string, string>("misc", "BarkManager.xml");
            }

            if (read >= 17 && text.StartsWith("<ObjectInventory>") == true)
            {
                return new Tuple<string, string>("misc", "ObjectInventory.xml");
            }

            if (read >= 21 && text.StartsWith("<CollectionInventory>") == true)
            {
                return new Tuple<string, string>("misc", "CollectionInventory.xml");
            }

            if (read >= 14 && text.StartsWith("<SoundRegions>") == true)
            {
                return new Tuple<string, string>("misc", "SoundRegions.xml");
            }

            if (read >= 11 && text.StartsWith("<MovieData>") == true)
            {
                return new Tuple<string, string>("misc", "MovieData.xml");
            }

            if (read >= 8 && text.StartsWith("<Profile") == true)
            {
                return new Tuple<string, string>("misc", "Profile.xml");
            }

            if (read >= 12 && text.StartsWith("<stringtable") == true)
            {
                return new Tuple<string, string>("text", "xml");
            }

            if (read >= 5 && text.StartsWith("<?xml") == true)
            {
                return new Tuple<string, string>("misc", "xml");
            }

            if (read >= 1 && text.StartsWith("<Sequence>") == true)
            {
                return new Tuple<string, string>("game", "seq");
            }

            if (read >= 8 && text.StartsWith("<Binary>"))
            {
                return new Tuple<string, string>("pilot", "pnm");
            }

            return null;
        }
    }
}
