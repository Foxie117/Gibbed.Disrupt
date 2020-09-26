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
using System.IO;
using Gibbed.IO;
using CBR;

namespace Gibbed.Disrupt.FileFormats
{
    public class BinaryObjectFile
    {
        private const uint _Signature = 0x4643626E; // 'FCbn' FarCry Binary N???

        public string Header = "";
        public ushort Version = 3;
        public HeaderFlags Flags = HeaderFlags.None;
        public BinaryObject Root;

        public void Serialize(Stream output)
        {
            /*
            if (this.Version != 3)
            {
                throw new FormatException("unsupported file version");
            }
            */

            if (this.Flags != HeaderFlags.None)
            {
                throw new FormatException("unsupported file flags");
            }

            var endian = Endian.Little;
            using (var data = new MemoryStream())
            {
                uint totalObjectCount = 0, totalValueCount = 0;

                this.Root.Serialize(data, ref totalObjectCount,
                                    ref totalValueCount,
                                    endian);
                data.Flush();
                data.Position = 0;

                // write header
                string headerString = this.Header;

                if (headerString.Length > 0)
                {
                    Utility.Log("Writing header...");

                    byte[] headerBytes = Utility.HexToBytes(headerString);
                    string headerBytesString = BitConverter.ToString(headerBytes).Replace("-", "");

                    //Utility.Log($"Writing header...\n\tfrom: {headerString}\n\tto: {headerBytesString}");

                    output.Write(headerBytes, 0, headerBytes.Length);

                    Utility.Log("Done!");
                }

                // write magic
                output.WriteValueU32(_Signature, endian);

                // write version
                output.WriteValueU16(this.Version, endian);

                // write everything else
                output.WriteValueEnum<HeaderFlags>(this.Flags, endian);
                output.WriteValueU32(totalObjectCount, endian);
                output.WriteValueU32(totalValueCount, endian);
                output.WriteFromStream(data, data.Length);
            }
        }

        byte[] GetHeaderBytes(Stream input, long length)
        {
            input.Position = 0;

            byte[] buffer = new byte[length];

            int read = 0;
            int chunk = 0;

            while ((chunk = input.Read(buffer, read, buffer.Length - read)) > 0)
            {
                read += chunk;

                if (read == buffer.Length)
                {
                    int nextByte = input.ReadByte();

                    if (nextByte == -1)
                    {
                        return buffer;
                    }

                    byte[] newBuffer = new byte[buffer.Length];
                    Array.Copy(buffer, newBuffer, buffer.Length);

                    buffer = newBuffer;
                }
            }

            byte[] ret = new byte[read];
            Array.Copy(buffer, ret, read);
            return ret;
        }

        string GetHeaderString(Stream input, long offset)
        {
            byte[] bytes = GetHeaderBytes(input, offset);

            string header = BitConverter.ToString(bytes).Replace("-", "");

            FileLogger.Log(header);

            return header;
        }

        public void Deserialize(Stream input)
        {
            /*
            var magic = input.ReadValueU32(Endian.Little);
            var endian = Endian.Little;

            Console.WriteLine("0");

            if (magic != _Signature)
            {
                magic = input.ReadValueU32(Endian.Big);

                if (magic == _Signature)
                {
                    Console.WriteLine("1A");
                    endian = Endian.Big;
                }
                else
                {
                    Console.WriteLine("1B");
                    //throw new FormatException("invalid header magic");
                }
            }

            var version = input.ReadValueU16(endian);

            if (version != 3)
            {
                Console.WriteLine("2");
                //throw new FormatException("unsupported file version");
            }

            var flags = input.ReadValueEnum<HeaderFlags>(endian);

            if (flags != HeaderFlags.None)
            {
                Console.WriteLine("3");
                //throw new FormatException("unsupported file flags");
            }

            Console.Write($"\nFile info:\n\tMagic = {magic:X8}\n\tVersion = {version}\n\tFlags = {flags}\n");
            */


            var endian = Endian.Little;

            // store header, read magic
            uint magic = input.ReadValueU32(endian);
            long magicOffset = 0;
            string header = "";

            if (magic != _Signature)
            {
                Utility.Log("Reading header...");

                while (magicOffset == 0)
                {
                    var latest = input.ReadValueU32();

                    if (latest == _Signature)
                    {
                        magicOffset = input.Position - 4;

                        Utility.Log("Signature found!");

                        break;
                    }
                }

                header = GetHeaderString(input, magicOffset);

                input.Position = magicOffset;
                magic = input.ReadValueU32(endian);

                Utility.Log("Done!");
            }

            // store version
            var version = input.ReadValueU16(endian);

            // everything else
            var flags = input.ReadValueEnum<HeaderFlags>(endian);

            var totalObjectCount = input.ReadValueU32(endian);
            var totalValueCount = input.ReadValueU32(endian);

            var pointers = new List<BinaryObject>();

            Console.Write($"Magic: {magic:X8}\nVersion: {version:X4}\nFlags: {flags}\nHeader Length: {header.Length / 2}\n\n");

            this.Header = header;
            this.Version = version;
            this.Flags = flags;
            this.Root = BinaryObject.Deserialize(null, input, pointers, endian);
        }

        [Flags]
        public enum HeaderFlags : ushort
        {
            None = 0,

            Debug = 1 << 0, // "Not Stripped"
        }
    }
}
