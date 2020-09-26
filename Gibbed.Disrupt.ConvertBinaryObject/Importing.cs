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

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.XPath;
using Gibbed.Disrupt.BinaryObjectInfo;
using Gibbed.Disrupt.FileFormats;

namespace Gibbed.Disrupt.ConvertBinaryObject
{
    internal class Importing
    {
        public BinaryObject Import(string basePath,
                                   XPathNavigator nav)
        {
            var root = new BinaryObject();
            ReadNode(root, new BinaryObject[0], basePath, nav, null);
            return root;
        }

        string HexToString(string hex)
        {
            string ascii = "";

            for (int i = 0; i < hex.Length / 2; i++)
            {
                string chars = $"{hex[i * 2]}{hex[(i * 2) + 1]}";

                if (chars != "00")
                {
                    ascii += (char) Convert.ToInt32(chars, 16);
                }
            }

            return ascii;
        }

        private void ReadNode(BinaryObject node,
                              IEnumerable<BinaryObject> parentChain,
                              string basePath,
                              XPathNavigator nav,
                              string currentFileName)
        {
            var chain = parentChain.Concat(new[] { node });

            string className;
            uint classNameHash;

            LoadNameAndHash(nav, out className, out classNameHash);

            //Console.WriteLine($"currentFileName = {currentFileName}");
            Console.WriteLine($"{classNameHash:X8} = {className}");

            node.NameHash = classNameHash;

            var fields = nav.Select("field");
            while (fields.MoveNext() == true)
            {
                if (fields.Current == null)
                {
                    throw new InvalidOperationException();
                }

                LoadNameAndHash(fields.Current, out string fieldName, out uint fieldNameHash);

                if (fieldName != null && fieldNameHash == 0x9D8873F8 && currentFileName != null) // crc32(text_hidName)
                {
                    var specifiedName = HexToString(fields.Current.Value);
                    specifiedName = specifiedName.Replace('"', '_').Replace(':', '_').Replace('*', '_').Replace('?', '_').Replace('<', '_').Replace('>', '_').Replace('|', '_');

                    if (!currentFileName.Equals(specifiedName))
                    {
                        throw new ArgumentException(string.Format("Specified file name \"{0}\" does not match actual file name \"{1}\"", specifiedName, currentFileName), "text_hidName");
                    }
                }

                FieldType fieldType;
                var fieldTypeName = fields.Current.GetAttribute("type", "");
                if (Enum.TryParse(fieldTypeName, true, out fieldType) == false)
                {
                    throw new InvalidOperationException();
                }

                var arrayFieldType = FieldType.Invalid;
                var arrayFieldTypeName = fields.Current.GetAttribute("array_type", "");
                if (string.IsNullOrEmpty(arrayFieldTypeName) == false)
                {
                    if (Enum.TryParse(arrayFieldTypeName, true, out arrayFieldType) == false)
                    {
                        throw new InvalidOperationException();
                    }
                }

                var data = FieldHandling.Import(fieldType, arrayFieldType, fields.Current);
                node.Fields.Add(fieldNameHash, data);
            }

            var children = nav.Select("object");
            while (children.MoveNext() == true)
            {
                var child = new BinaryObject();
                LoadChildNode(child, chain, basePath, children.Current);
                node.Children.Add(child);
            }
        }

        private void HandleChildNode(BinaryObject node,
                                     IEnumerable<BinaryObject> chain,
                                     string basePath,
                                     XPathNavigator nav,
                                     string currentFileName)
        {
            string className;
            uint classNameHash;

            LoadNameAndHash(nav, out className, out classNameHash);

            ReadNode(node, chain, basePath, nav, currentFileName);
            return;

            throw new InvalidOperationException();
        }

        private void LoadChildNode(BinaryObject node,
                                   IEnumerable<BinaryObject> chain,
                                   string basePath,
                                   XPathNavigator nav)
        {
            var external = nav.GetAttribute("external", "");
            if (string.IsNullOrWhiteSpace(external) == true)
            {
                HandleChildNode(node, chain, basePath, nav, null);
                return;
            }

            var inputPath = Path.Combine(basePath, external);
            using (var input = File.OpenRead(inputPath))
            {
                var nestedDoc = new XPathDocument(input);
                var nestedNav = nestedDoc.CreateNavigator();

                var root = nestedNav.SelectSingleNode("/object");
                if (root == null)
                {
                    throw new InvalidOperationException();
                }

                HandleChildNode(node, chain, Path.GetDirectoryName(inputPath), root, external.Substring(0, external.LastIndexOf('.')).Replace('\\', '/'));
            }
        }

        private static uint? GetClassDefinitionByField(string classFieldName, uint? classFieldHash, XPathNavigator nav)
        {
            uint? hash = null;

            if (string.IsNullOrEmpty(classFieldName) == false)
            {
                var fieldByName = nav.SelectSingleNode("field[@name=\"" + classFieldName + "\"]");
                if (fieldByName != null)
                {
                    uint temp;
                    if (uint.TryParse(fieldByName.Value,
                                      NumberStyles.AllowHexSpecifier,
                                      CultureInfo.InvariantCulture,
                                      out temp) == false)
                    {
                        throw new InvalidOperationException();
                    }
                    hash = temp;
                }
            }

            if (hash.HasValue == false &&
                classFieldHash.HasValue == true)
            {
                var fieldByHash =
                    nav.SelectSingleNode("field[@hash=\"" +
                                         classFieldHash.Value.ToString("X8", CultureInfo.InvariantCulture) +
                                         "\"]");
                if (fieldByHash != null)
                {
                    uint temp;
                    if (uint.TryParse(fieldByHash.Value,
                                      NumberStyles.AllowHexSpecifier,
                                      CultureInfo.InvariantCulture,
                                      out temp) == false)
                    {
                        throw new InvalidOperationException();
                    }
                    hash = temp;
                }
            }

            return hash;
        }

        private static void LoadNameAndHash(XPathNavigator nav, out string name, out uint hash)
        {
            var nameAttribute = nav.GetAttribute("name", "");
            var hashAttribute = nav.GetAttribute("hash", "");

            if (string.IsNullOrWhiteSpace(nameAttribute) == true && string.IsNullOrWhiteSpace(hashAttribute) == true)
            {
                throw new FormatException();
            }

            name = string.IsNullOrWhiteSpace(nameAttribute) == false ? nameAttribute : null;
            hash = name != null
                       ? FileFormats.Hashing.CRC32.Compute(name)
                       : uint.Parse(hashAttribute, NumberStyles.AllowHexSpecifier);
        }
    }
}
