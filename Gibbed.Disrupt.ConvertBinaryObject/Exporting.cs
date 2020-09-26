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

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Gibbed.Disrupt.BinaryObjectInfo;
using Gibbed.Disrupt.FileFormats;
using System.Text.RegularExpressions;
using System.Diagnostics;
using CBR;
using System;

namespace Gibbed.Disrupt.ConvertBinaryObject
{
    internal static class Exporting
    {
        public static void Export(string outputPath,
                                  BinaryObjectFile bof)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                CheckCharacters = false,
                OmitXmlDeclaration = false
            };

            using (var writer = XmlWriter.Create(outputPath, settings))
            {
                writer.WriteStartDocument();
                WriteRootNode(writer, new BinaryObject[0], bof);
                writer.WriteEndDocument();
            }
        }

        internal const uint EntityLibrariesHash = 0xBCDD10B4u; // crc32(EntityLibraries)
        internal const uint EntityLibraryHash = 0xE0BDB3DBu; // crc32(EntityLibrary)
        internal const uint NameHash = 0xFE11D138u; // crc32(Name);
        internal const uint EntityLibraryItemHash = 0x256A1FF9u; // unknown source name
        internal const uint DisLibItemIdHash = 0x8EDB0295u; // crc32(disLibItemId)
        internal const uint EntityHash = 0x0984415Eu; // crc32(Entity)
        internal const uint LibHash = 0xA90F3BCC; // crc32(lib)
        internal const uint LibItemHash = 0x72DE4948; // unknown source name
        internal const uint TextHidNameHash = 0x9D8873F8; // crc32(text_hidName)
        internal const uint NomadObjectTemplatesHash = 0x4C4C4CA4; // crc32(NomadObjectTemplates)
        internal const uint NomadObjectTemplateHash = 0x142371CF; // unknown source name
        internal const uint TemplateHash = 0x6E167DD5; // crc32(Template)

        public static void MultiExportEntityLibrary(string basePath,
                                                    string outputPath,
                                                    BinaryObjectFile bof)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                CheckCharacters = false,
                OmitXmlDeclaration = false
            };

            using (var writer = XmlWriter.Create(outputPath, settings))
            {
                writer.WriteStartDocument();

                var root = bof.Root;
                {
                    writer.WriteStartElement("object");
                    writer.WriteAttributeString("name", "EntityLibraries");

                    // add version attribute to root object
                    string bofVersion = bof.Version.ToString();

                    if (bofVersion != null)
                    {
                        writer.WriteAttributeString("version", bofVersion);
                    }

                    // add header attribute to root object
                    string bofHeader = bof.Header;

                    if (bofHeader.Length > 0)
                    {
                        writer.WriteAttributeString("header", bofHeader);
                    }

                    var libraryNames = new Dictionary<string, int>();

                    Directory.CreateDirectory(basePath);

                    foreach (var library in root.Children)
                    {
                        var chain = new[] { bof.Root, library };

                        var libraryName = FieldHandling.Deserialize<string>(FieldType.String, library.Fields[NameHash]);
                        var unsanitizedLibraryName = libraryName;

                        libraryName = libraryName.Replace('/', Path.DirectorySeparatorChar);
                        libraryName = libraryName.Replace('\\', Path.DirectorySeparatorChar);
                        libraryName = libraryName.Replace('"', '_').Replace(':', '_').Replace('*', '_').Replace('?', '_').Replace('<', '_').Replace('>', '_').Replace('|', '_');

                        if (libraryNames.ContainsKey(libraryName) == false)
                        {
                            libraryNames.Add(libraryName, 1);
                        }
                        else
                        {
                            libraryName = string.Format("{0} ({1})", libraryName, ++libraryNames[libraryName]);
                        }

                        var libraryPath = Path.Combine(libraryName, "@library.xml");

                        writer.WriteStartElement("object");
                        writer.WriteAttributeString("external", libraryPath);
                        writer.WriteEndElement();

                        libraryPath = Path.Combine(basePath, libraryPath);

                        var itemNames = new Dictionary<string, int>();

                        var libraryParentPath = Path.GetDirectoryName(libraryPath);
                        if (string.IsNullOrEmpty(libraryParentPath) == false)
                        {
                            Directory.CreateDirectory(libraryParentPath);
                        }

                        using (var libraryWriter = XmlWriter.Create(libraryPath, settings))
                        {
                            libraryWriter.WriteStartDocument();
                            libraryWriter.WriteStartElement("object");
                            libraryWriter.WriteAttributeString("name", "EntityLibrary");

                            libraryWriter.WriteStartElement("field");
                            libraryWriter.WriteAttributeString("name", "Name");
                            libraryWriter.WriteAttributeString("type", "String");
                            libraryWriter.WriteString(unsanitizedLibraryName);
                            libraryWriter.WriteEndElement();

                            foreach (var item in library.Children)
                            {
                                var itemName = FieldHandling.Deserialize<string>(FieldType.String, item.Fields[NameHash]);
                                itemName = itemName.Replace('/', Path.DirectorySeparatorChar);
                                itemName = itemName.Replace('\\', Path.DirectorySeparatorChar);
                                itemName = itemName.Replace('"', '_').Replace(':', '_').Replace('*', '_').Replace('?', '_').Replace('<', '_').Replace('>', '_').Replace('|', '_');

                                if (itemNames.ContainsKey(itemName) == false)
                                {
                                    itemNames.Add(itemName, 1);
                                }
                                else
                                {
                                    itemName = string.Format("{0} ({1})", itemName, ++itemNames[itemName]);
                                }

                                var itemPath = itemName + ".xml";

                                libraryWriter.WriteStartElement("object");
                                libraryWriter.WriteAttributeString("external", itemPath);
                                libraryWriter.WriteEndElement();

                                itemPath = Path.Combine(basePath, libraryName, itemPath);

                                var itemParentPath = Path.GetDirectoryName(itemPath);
                                if (string.IsNullOrEmpty(itemParentPath) == false)
                                {
                                    Directory.CreateDirectory(itemParentPath);
                                }

                                using (var itemWriter = XmlWriter.Create(itemPath, settings))
                                {
                                    itemWriter.WriteStartDocument();
                                    WriteNode(itemWriter,
                                              chain,
                                              item);
                                    itemWriter.WriteEndDocument();
                                }
                            }

                            libraryWriter.WriteEndDocument();
                        }
                    }

                    writer.WriteEndElement();
                }

                writer.WriteEndDocument();
            }
        }

        public static bool IsSuitableForEntityLibraryMultiExport(BinaryObjectFile bof)
        {
            if (bof.Root.Fields.Count != 0 ||
                bof.Root.NameHash != EntityLibrariesHash ||
                bof.Root.Children.Any(c => c.NameHash != EntityLibraryHash) == true)
            {
                return false;
            }

            var nameSeq = new[] { NameHash };
            var idAndNameSeq = new[] { DisLibItemIdHash, NameHash };

            foreach (var library in bof.Root.Children)
            {
                if (library.Fields.Keys.SequenceEqual(nameSeq) == false)
                {
                    return false;
                }

                if (library.Children.Any(sc => sc.NameHash != EntityLibraryItemHash) == true)
                {
                    return false;
                }

                foreach (var item in library.Children)
                {
                    if (item.Fields.Keys.OrderBy(h => h).SequenceEqual(idAndNameSeq) == false)
                    {
                        return false;
                    }

                    if (item.Children.Any(sc => sc.NameHash != EntityHash) == true)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        public static void MultiExportLibrary(string basePath,
                                              string outputPath,
                                              BinaryObjectFile bof)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                CheckCharacters = false,
                OmitXmlDeclaration = false
            };

            using (var writer = XmlWriter.Create(outputPath, settings))
            {
                writer.WriteStartDocument();

                var root = bof.Root;

                var chain = new[] { root };
                {
                    writer.WriteStartElement("object");
                    writer.WriteAttributeString("name", "lib");

                    // add version attribute to root object
                    string bofVersion = bof.Version.ToString();

                    if (bofVersion != null)
                    {
                        writer.WriteAttributeString("version", bofVersion);
                    }

                    // add header attribute to root object
                    string bofHeader = bof.Header;

                    if (bofHeader.Length > 0)
                    {
                        writer.WriteAttributeString("header", bofHeader);

                        /*
                        var headerBytes = Utility.HexToBytes(bofHeader);
                        var headerBytesString = BitConverter.ToString(headerBytes).Replace("-", "");

                        Console.WriteLine($"Test = {headerBytesString}");
                        */
                    }

                    Directory.CreateDirectory(basePath);

                    var itemNames = new Dictionary<string, int>();

                    foreach (var item in root.Children)
                    {
                        var itemName = FieldHandling.Deserialize<string>(FieldType.String, item.Fields[TextHidNameHash]);
                        itemName = itemName.Replace('/', Path.DirectorySeparatorChar);
                        itemName = itemName.Replace('\\', Path.DirectorySeparatorChar);
                        itemName = itemName.Replace('"', '_').Replace(':', '_').Replace('*', '_').Replace('?', '_').Replace('<', '_').Replace('>', '_').Replace('|', '_');

                        if (!itemNames.ContainsKey(itemName))
                        {
                            itemNames.Add(itemName, 1);
                        }
                        else
                        {
                            itemName = string.Format("{0} ({1})", itemName, ++itemNames[itemName]);
                        }

                        var itemPath = itemName + ".xml";

                        writer.WriteStartElement("object");
                        writer.WriteAttributeString("external", itemPath);
                        writer.WriteEndElement();

                        itemPath = Path.Combine(basePath, itemPath);

                        var itemParentPath = Path.GetDirectoryName(itemPath);
                        if (string.IsNullOrEmpty(itemParentPath) == false)
                        {
                            Directory.CreateDirectory(itemParentPath);
                        }

                        using (var itemWriter = XmlWriter.Create(itemPath, settings))
                        {
                            itemWriter.WriteStartDocument();
                            WriteNode(itemWriter,
                                      chain,
                                      item);
                            itemWriter.WriteEndDocument();
                        }
                    }
                }

                writer.WriteEndDocument();
            }
        }

        public static bool IsSuitableForLibraryMultiExport(BinaryObjectFile bof)
        {
            return bof.Root.Fields.Count == 0 &&
                   bof.Root.NameHash == LibHash &&
                   bof.Root.Children.Any(c => c.NameHash != LibItemHash ||
                                              c.Fields.ContainsKey(TextHidNameHash) == false) == false;
        }

        public static void MultiExportNomadObjectTemplates(string basePath,
                                                           string outputPath,
                                                           BinaryObjectFile bof)
        {
            var settings = new XmlWriterSettings
            {
                Encoding = Encoding.UTF8,
                Indent = true,
                CheckCharacters = false,
                OmitXmlDeclaration = false
            };

            using (var writer = XmlWriter.Create(outputPath, settings))
            {
                writer.WriteStartDocument();

                var root = bof.Root;
                var chain = new[] { root };
                {
                    writer.WriteStartElement("object");
                    writer.WriteAttributeString("name", "NomadObjectTemplates");
                    Directory.CreateDirectory(basePath);

                    var itemNames = new Dictionary<string, int>();

                    foreach (var item in root.Children)
                    {
                        var itemName = FieldHandling.Deserialize<string>(FieldType.String, item.Fields[NameHash]);
                        itemName = itemName.Replace('/', Path.DirectorySeparatorChar);
                        itemName = itemName.Replace('\\', Path.DirectorySeparatorChar);
                        itemName = itemName.Replace('"', '_').Replace(':', '_').Replace('*', '_').Replace('?', '_').Replace('<', '_').Replace('>', '_').Replace('|', '_');

                        if (itemNames.ContainsKey(itemName) == false)
                        {
                            itemNames.Add(itemName, 1);
                        }
                        else
                        {
                            itemName = string.Format("{0} ({1})", itemName, ++itemNames[itemName]);
                        }

                        var itemPath = itemName + ".xml";

                        writer.WriteStartElement("object");
                        writer.WriteAttributeString("external", itemPath);
                        writer.WriteEndElement();

                        itemPath = Path.Combine(basePath, itemPath);

                        var itemParentPath = Path.GetDirectoryName(itemPath);
                        if (string.IsNullOrEmpty(itemParentPath) == false)
                        {
                            Directory.CreateDirectory(itemParentPath);
                        }

                        using (var itemWriter = XmlWriter.Create(itemPath, settings))
                        {
                            itemWriter.WriteStartDocument();
                            WriteNode(itemWriter,
                                      chain,
                                      item);
                            itemWriter.WriteEndDocument();
                        }
                    }
                }

                writer.WriteEndDocument();
            }
        }

        public static bool IsSuitableForNomadObjectTemplatesMultiExport(BinaryObjectFile bof)
        {
            if (bof.Root.Fields.Count != 0 ||
                bof.Root.NameHash != NomadObjectTemplatesHash ||
                bof.Root.Children.Any(c => c.NameHash != NomadObjectTemplateHash) == true)
            {
                return false;
            }

            var nameSeq = new[] { NameHash };

            foreach (var library in bof.Root.Children)
            {
                if (library.Fields.Keys.SequenceEqual(nameSeq) == false)
                {
                    return false;
                }

                if (library.Children.Any(sc => sc.NameHash != TemplateHash) == true)
                {
                    return false;
                }
            }

            return true;
        }

        private static void WriteRootNode(XmlWriter writer,
                                          IEnumerable<BinaryObject> parentChain,
                                          BinaryObjectFile bof)
        {
            BinaryObject node = bof.Root;

            var chain = parentChain.Concat(new[] { node });

            writer.WriteStartElement("object");

            // get object name from object hash via string list
            var objectHashInput = (int)node.NameHash;

            if (StringHasher.CanResolveHash(objectHashInput))
            {
                var objectHashOutput = StringHasher.ResolveHash(objectHashInput);

                writer.WriteAttributeString("name", objectHashOutput);
            }
            else
            {
                writer.WriteAttributeString("hash", node.NameHash.ToString("X8"));
            }

            // add version attribute to root object
            string bofVersion = bof.Version.ToString();

            if (bofVersion != null)
            {
                writer.WriteAttributeString("version", bofVersion);
            }

            // add header attribute to root object
            string bofHeader = bof.Header;

            if (bofHeader.Length > 0)
            {
                writer.WriteAttributeString("header", bofHeader);
            }

            if (node.Fields != null)
            {
                foreach (var kv in node.Fields)
                {
                    writer.WriteStartElement("field");

                    // get field name from field hash via string list
                    var fieldHashInput = (int)kv.Key;

                    if (StringHasher.CanResolveHash(fieldHashInput))
                    {
                        var fieldHashOutput = StringHasher.ResolveHash(fieldHashInput);

                        writer.WriteAttributeString("name", fieldHashOutput);
                    }
                    else
                    {
                        writer.WriteAttributeString("hash", kv.Key.ToString("X8"));
                    }

                    writer.WriteAttributeString("type", FieldHandling.GetTypeName(FieldType.BinHex));
                    writer.WriteBinHex(kv.Value, 0, kv.Value.Length);

                    writer.WriteEndElement();
                }
            }

            foreach (var childNode in node.Children)
            {
                WriteNode(writer, chain, childNode);
            }

            writer.WriteEndElement();
        }

        private static void WriteNode(XmlWriter writer,
                                      IEnumerable<BinaryObject> parentChain,
                                      BinaryObject node)
        {
            var chain = parentChain.Concat(new[] { node });

            writer.WriteStartElement("object");

            // get object name from object hash via string list
            var objectHashInput = (int) node.NameHash;

            if (StringHasher.CanResolveHash(objectHashInput))
            {
                var objectHashOutput = StringHasher.ResolveHash(objectHashInput);

                writer.WriteAttributeString("name", objectHashOutput);
            }
            else
            {
                writer.WriteAttributeString("hash", node.NameHash.ToString("X8"));
            }

            if (node.Fields != null)
            {
                foreach (var kv in node.Fields)
                {
                    writer.WriteStartElement("field");

                    // get field name from field hash via string list
                    var fieldHashInput = (int) kv.Key;

                    if (StringHasher.CanResolveHash(fieldHashInput))
                    {
                        var fieldHashOutput = StringHasher.ResolveHash(fieldHashInput);

                        writer.WriteAttributeString("name", fieldHashOutput);
                    }
                    else
                    {
                        writer.WriteAttributeString("hash", kv.Key.ToString("X8"));
                    }

                    writer.WriteAttributeString("type", FieldHandling.GetTypeName(FieldType.BinHex));
                    writer.WriteBinHex(kv.Value, 0, kv.Value.Length);

                    writer.WriteEndElement();
                }
            }

            foreach (var childNode in node.Children)
            {
                WriteNode(writer, chain, childNode);
            }

            writer.WriteEndElement();
        }
    }
}
