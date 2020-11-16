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
using System.IO;
using System.Text.RegularExpressions;
using System.Xml.XPath;
using Gibbed.Disrupt.FileFormats;
using NDesk.Options;
using CBR;

namespace Gibbed.Disrupt.ConvertBinaryObject
{
    internal class Program
    {
        static readonly AppDomain m_domain = AppDomain.CurrentDomain;
        static readonly string m_directory = m_domain.BaseDirectory;

        private static string GetExecutableName()
        {
            return Path.GetFileName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        static void Prepare()
        {
            StringHasher.Initialize(m_directory);
            FileLogger.Initialize(m_directory);

            Utility.TestUtility();
        }

        private static void Main(string[] args)
        {
            var mode = Mode.Unknown;
            string baseName = "";
            bool showHelp = false;
            bool verbose = false;
            bool useMultiExporting = true;

            var options = new OptionSet()
            {
                { "i|import|fcb", "convert XML to FCB", v => mode = v != null ? Mode.Import : mode },
                { "e|export|xml", "convert FCB to XML", v => mode = v != null ? Mode.Export : mode },
                { "b|base-name=", "when converting, use specified base name instead of file name", v => baseName = v },
                {
                    "nme|no-multi-export", "when exporting, disable multi-exporting of entitylibrary and lib files",
                    v => useMultiExporting = v == null
                },
                { "v|verbose", "be verbose", v => verbose = v != null },
                { "h|help", "show this message and exit", v => showHelp = v != null },
            };

            List<string> extras;

            try
            {
                extras = options.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Write("{0}: ", GetExecutableName());
                Console.WriteLine(e.Message);
                Console.WriteLine("Try `{0} --help' for more information.", GetExecutableName());
                return;
            }

            if (mode == Mode.Unknown && extras.Count >= 1)
            {
                var extension = Path.GetExtension(extras[0]);

                if (!string.IsNullOrEmpty(extension))
                {
                    extension = extension.ToLowerInvariant();
                }

                if (extension == ".xml")
                {
                    mode = Mode.Import;
                }
                else
                {
                    mode = Mode.Export;
                }
            }

            if (showHelp || mode == Mode.Unknown || extras.Count < 1 || extras.Count > 2)
            {
                Console.WriteLine("Usage: {0} [OPTIONS]+ input [output]", GetExecutableName());
                Console.WriteLine();
                Console.WriteLine("Options:");
                options.WriteOptionDescriptions(Console.Out);
                return;
            }

            // custom
            Prepare();

            if (mode == Mode.Import)
            {
                string inputPath = extras[0];
                string outputPath;

                if (extras.Count > 1)
                {
                    outputPath = extras[1];
                }
                else
                {
                    outputPath = RemoveConverted(Path.ChangeExtension(inputPath, null));

                    if (string.IsNullOrEmpty(Path.GetExtension(outputPath)))
                    {
                        var filename = Path.GetFileName(outputPath);
                        if (new Regex(@".+_[0-9a-fA-F]{8}").IsMatch(filename))
                        {
                            outputPath += ".obj";
                        }
                        else
                        {
                            outputPath += ".lib";
                        }
                    }
                }

                // Twice to remove *.lib.xml
                var basePath = Path.ChangeExtension(Path.ChangeExtension(inputPath, null), null);

                inputPath = Path.GetFullPath(inputPath);
                outputPath = Path.GetFullPath(outputPath);
                basePath = Path.GetFullPath(basePath);

                var bof = new BinaryObjectFile();

                using (var input = File.OpenRead(inputPath))
                {
                    var header = "";
                    var version = "";

                    var doc = new XPathDocument(input);
                    var nav = doc.CreateNavigator();

                    var root = nav.SelectSingleNode("/object");
                    if (root == null)
                    {
                        throw new FormatException();
                    }

                    version = root.GetAttribute("version", "");
                    bof.Version = (ushort) Convert.ToInt32(version);
                    Utility.Log($"Imported version = {version}");

                    header = root.GetAttribute("header", "");
                    bof.Header = header;
                    Utility.Log($"Imported header = {header}");

                    baseName = GetBaseNameFromPath(inputPath);

                    if (verbose)
                    {
                        Console.WriteLine("Reading XML...");
                    }

                    var importing = new Importing();

                    bof.Root = importing.Import(basePath, root);
                }

                if (verbose)
                {
                    Console.WriteLine("Writing FCB...");
                }

                using (var output = File.Create(outputPath))
                {
                    bof.Serialize(output);
                }
            }
            else if (mode == Mode.Export)
            {
                string inputPath = extras[0];
                string outputPath;
                string basePath;

                if (extras.Count > 1)
                {
                    outputPath = extras[1];
                    basePath = Path.ChangeExtension(outputPath, null);
                }
                else
                {
                    basePath = Path.ChangeExtension(inputPath, null);
                    outputPath = inputPath + ".xml";
                }

                if (string.IsNullOrEmpty(baseName))
                {
                    baseName = GetBaseNameFromPath(inputPath);
                }

                if (string.IsNullOrEmpty(baseName))
                {
                    throw new InvalidOperationException();
                }

                inputPath = Path.GetFullPath(inputPath);
                outputPath = Path.GetFullPath(outputPath);
                basePath = Path.GetFullPath(basePath);

                if (verbose)
                {
                    Console.WriteLine("Reading FCB...");
                }

                var bof = new BinaryObjectFile();
                using (var input = File.OpenRead(inputPath))
                {
                    bof.Deserialize(input);
                }

                if (verbose)
                {
                    Console.WriteLine("Writing XML...");
                }

                if (useMultiExporting && Exporting.IsSuitableForEntityLibraryMultiExport(bof))
                {
                    Exporting.MultiExportEntityLibrary(basePath, outputPath, bof);
                }
                else if (useMultiExporting && Exporting.IsSuitableForLibraryMultiExport(bof))
                {
                    Exporting.MultiExportLibrary(basePath, outputPath, bof);
                }
                else if (useMultiExporting && Exporting.IsSuitableForNomadObjectTemplatesMultiExport(bof))
                {
                    Exporting.MultiExportNomadObjectTemplates(basePath, outputPath, bof);
                }
                else
                {
                    Exporting.Export(outputPath, bof);
                }
            }
        }

        private static string GetBaseNameFromPath(string inputPath)
        {
            var baseName = Path.GetFileNameWithoutExtension(inputPath);
            baseName = RemoveConverted(baseName);
            return baseName;
        }

        private static string RemoveConverted(string input)
        {
            if (!string.IsNullOrEmpty(input) && input.EndsWith("_converted"))
            {
                input = input.Substring(0, input.Length - 10);
            }
            return input;
        }
    }
}
