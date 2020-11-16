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
using System.Globalization;

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

        private static void WriteNodeField(XmlWriter writer, BinaryObject node)
        {
            if (node.Fields != null)
            {
                KeyValuePair<uint, byte[]>[] kvArray = new KeyValuePair<uint, byte[]>[node.Fields.Count];

                FieldInfo overrideFieldInfo = new FieldInfo();
                FieldInfo r_overrideFieldInfo = overrideFieldInfo;

                int kvIterator = 0;

                foreach (var kv in node.Fields)
                {
                    kvArray[kvIterator] = kv;

                    kvIterator++;
                }

                for (int a = 0; a < kvArray.Length; a++)
                {
                    var kvCurrent = kvArray[a];
                    var kvNext = new KeyValuePair<uint, byte[]>();

                    FieldInfo currentFieldInfo = new FieldInfo();
                    FieldInfo nextFieldInfo = new FieldInfo();

                    uint currentKey = kvCurrent.Key;
                    byte[] currentValue = kvCurrent.Value;

                    if (overrideFieldInfo != currentFieldInfo && overrideFieldInfo != new FieldInfo())
                    {
                        currentFieldInfo = overrideFieldInfo;
                        overrideFieldInfo = new FieldInfo();
                    }
                    else
                    {
                        currentFieldInfo = ResolveField(currentKey, currentValue);
                    }

                    if (a + 1 < kvArray.Length)
                    {
                        kvNext = kvArray[a + 1];

                        uint nextKey = kvNext.Key;
                        byte[] nextValue = kvNext.Value;

                        nextFieldInfo = ResolveField(nextKey, nextValue);
                    }

                    if (ValidateField(kvCurrent, kvNext, out r_overrideFieldInfo) == false)
                    {
                        overrideFieldInfo = r_overrideFieldInfo;
                    }
                    else
                    {
                        writer.WriteStartElement("field");
                        writer.WriteAttributeString(currentFieldInfo.key.type, currentFieldInfo.key.name);
                        writer.WriteAttributeString("type", currentFieldInfo.value.type);
                        writer.WriteString(currentFieldInfo.value.value);
                        writer.WriteEndElement();
                    }
                }
            }
        }

        private static void WriteRootNode(XmlWriter writer,
                                          IEnumerable<BinaryObject> parentChain,
                                          BinaryObjectFile bof)
        {
            BinaryObject node = bof.Root;

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

            WriteNodeField(writer, node);

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

            WriteNodeField(writer, node);

            foreach (var childNode in node.Children)
            {
                WriteNode(writer, chain, childNode);
            }

            writer.WriteEndElement();
        }

        private static void ResolveField(XmlWriter writer, KeyValuePair<uint, byte[]> kv)
        {
            string fieldType = FieldHandling.GetTypeName(FieldType.BinHex);
            string fieldValue = BitConverter.ToString(kv.Value).Replace("-", "");

            string r_fieldTypeString = fieldType;
            string r_fieldValueString = fieldValue;

            if (kv.Key == TextHidNameHash)
            {
                FieldType fileNameFieldType = FieldType.String;
                string fileNameFieldValue = FieldHandling.Deserialize<string>(fileNameFieldType, kv.Value);

                writer.WriteAttributeString("type", FieldHandling.GetTypeName(fileNameFieldType));
                writer.WriteString(fileNameFieldValue);

                Console.WriteLine($"Resolved name field in file \"{fileNameFieldValue}\"");
            }
            else
            {
                if (TypeGuesser.FieldHandler(kv.Value, out r_fieldTypeString, out r_fieldValueString) == true)
                {
                    if (r_fieldTypeString != FieldHandling.GetTypeName(FieldType.BinHex))
                    {
                        fieldType = r_fieldTypeString;
                        fieldValue = r_fieldValueString;
                    }

                    writer.WriteAttributeString($"value-{r_fieldTypeString}", fieldValue);
                }

                //writer.WriteAttributeString("type", r_fieldTypeString);
                //writer.WriteString(fieldValue);

                writer.WriteAttributeString("type", FieldHandling.GetTypeName(FieldType.BinHex));
                writer.WriteBinHex(kv.Value, 0, kv.Value.Length);
            }

            //Utility.Log($"Field Value\n\tfrom: {BitConverter.ToString(kv.Value).Replace(" ", "")}\n\tto: {fieldValue}");
        }

        private static FieldInfo ResolveField(uint key, byte[] value)
        {
            FieldInfo field = new FieldInfo();

            // key
            string keyTypeString = "hash";
            string keyNameString = $"{key:X8}";

            if (StringHasher.CanResolveHash((int) key))
            {
                keyTypeString = "name";
                keyNameString = StringHasher.ResolveHash((int) key);
            }

            // value
            string valueTypeString = FieldHandling.GetTypeName(FieldType.BinHex);
            string valueValueString = BitConverter.ToString(value).Replace("-", "");

            string r_valueTypeString = valueTypeString;
            string r_valueValueString = valueValueString;

            if (TypeGuesser.FieldHandler(value, out r_valueTypeString, out r_valueValueString) == true)
            {
                valueTypeString = r_valueTypeString;
                valueValueString = r_valueValueString;
            }

            // fill structure
            field.key.type = keyTypeString;
            field.key.name = keyNameString;

            field.value.type = valueTypeString;
            field.value.value = valueValueString;

            return field;
        }

        private static bool ValidateField(KeyValuePair<uint, byte[]> kvCurrent, KeyValuePair<uint, byte[]> kvNext, out FieldInfo overrideFieldInfo)
        {
            FieldInfo fieldInfo = new FieldInfo();
            overrideFieldInfo = fieldInfo;

            uint currentKey = kvCurrent.Key;
            byte[] currentValue = kvCurrent.Value;

            //string currentKeyName = StringHasher.ResolveHash((int)currentKey) ?? $"{currentKey:X8}";
            string currentKeyName = "";
            string currentValueString = BitConverter.ToString(currentValue).Replace("-", "");

            string r_currentKeyName;

            if (StringHasher.TryResolveHash((int) currentKey, out r_currentKeyName))
            {
                currentKeyName = r_currentKeyName;
            }

            if (kvNext.Value != null && kvNext.Value.Length > 0)
            {
                uint nextKey = kvNext.Key;
                byte[] nextValue = kvNext.Value;

                //string nextKeyName = StringHasher.ResolveHash((int) nextKey);
                string nextKeyName = "";
                string nextValueString = BitConverter.ToString(nextValue).Replace("-", "");

                string r_nextKeyName;

                if (StringHasher.TryResolveHash((int) nextKey, out r_nextKeyName))
                {
                    nextKeyName = r_nextKeyName;
                }

                /*
                var textPrefix = "text_";
                var currentKeyGuessName = $"{textPrefix}{nextKeyName}";
                uint currentKeyGuessHash = FileFormats.Hashing.CRC32.Compute(currentKeyGuessName);

                if (currentKeyGuessHash == currentKey)
                {
                    // current key is text of next key

                    //Console.WriteLine($"{currentKeyGuessHash:X8} == {currentKey:X8}");

                    currentValueString = FieldHandling.Deserialize<string>(FieldType.String, currentValue);
                }
                */

                FieldType r_currentFieldType;
                string r_currentValueString;

                if (currentKeyName == "text_hidName")
                {
                    currentValueString = FieldHandling.Deserialize<string>(FieldType.String, currentValue);

                    Utility.Log("Exporting " + currentValueString);
                }
                else if (TypeGuesser.TryParse(currentValue, out r_currentFieldType, out r_currentValueString))
                {
                    if (r_currentFieldType == FieldType.String)
                    {
                        currentValueString = r_currentValueString;
                    }
                }

                uint r_nextValueHash32 = 0;
                ulong r_nextValueHash64 = 0;
                ulong nextValueHash = 0;

                if (uint.TryParse(nextValueString, NumberStyles.HexNumber, null, out r_nextValueHash32))
                {
                    nextValueHash = r_nextValueHash32;

                    //Console.WriteLine($"Parsing uint: {nextValueHash:X8}");
                }
                else if (ulong.TryParse(nextValueString, NumberStyles.HexNumber, null, out r_nextValueHash64))
                {
                    nextValueHash = r_nextValueHash64;

                    //Console.WriteLine($"Parsing ulong: {nextValueHash:X16}");
                }

                ulong[] hashValues = HashHandler.CollectHashes(currentValueString);

                var hashTypeIterator = 0;

                for (int b = 0; b < hashValues.Length; b++)
                {
                    ulong hash = hashValues[b];

                    //Console.WriteLine($"Iterating hash: {hash:X}");

                    if (hash > 0 && hash == nextValueHash)
                    {
                        hashTypeIterator = b;

                        //Console.WriteLine($"{hash:X} == {nextValueHash:X}");

                        break;
                    }
                }

                ulong resolvedHash = hashValues[hashTypeIterator];

                if (hashTypeIterator > 0 && resolvedHash == nextValueHash)
                {
                    // current value is text of next value
                    //string fieldName = nextKeyName ?? $"{nextValueHash:X8}";

                    if (string.IsNullOrEmpty(nextKeyName))
                    {
                        fieldInfo.key.type = "hash";
                        fieldInfo.key.name = $"{nextKey:X8}";
                    }
                    else
                    {
                        fieldInfo.key.type = "name";
                        fieldInfo.key.name = nextKeyName;
                    }

                    fieldInfo.value.type = Convert.ToString((HashType) hashTypeIterator);
                    fieldInfo.value.value = currentValueString;

                    Console.WriteLine($"{fieldInfo.key.name} = {currentValueString}");

                    overrideFieldInfo = fieldInfo;

                    //Utility.Log($"{fieldName} = \"{currentValueString}\"");
                    //Utility.Log($"Found text value of field \"{fieldName}\": \"{currentValueString}\"");
                    //Utility.Log($"Element:\n\tcurrent:\n\t\tname: {currentKey:X8} -> {currentKeyName}\n\t\tvalue: {currentValueString} -> {resolvedHash:X8}\n\tnext:\n\t\tname: {nextKeyName} -> {nextKey:X8}\n\t\tvalue: {nextValueString}");

                    return false;
                }
            }

            return true;
        }
    }
}
