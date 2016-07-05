﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace Storm.Localization.Build.iOS
{
	public class BaseResToLocalizable : Task
	{
		[Required]
		public ITaskItem[] InputFiles { get; set; }

		[Output]
		public ITaskItem[] GeneratedStrings { get; private set; }

		[Output]
		public ITaskItem[] GeneratedCSharp { get; private set; }

		public override bool Execute()
		{
			Log.LogMessage(MessageImportance.High, "===> Preprocessing strings files <===");
			List<string> generatedFiles = new List<string>();
			foreach (ITaskItem inputFile in InputFiles)
			{
				string filePath = inputFile.ItemSpec;

				string realPath = filePath;
				foreach (string name in inputFile.MetadataNames)
				{
					if ("Link".Equals(name, StringComparison.InvariantCultureIgnoreCase))
					{
						realPath = inputFile.GetMetadata(name);
						break;
					}
				}

				string directory = Path.GetDirectoryName(realPath) ?? "";
				string outputFile = Path.Combine(directory, "Localizable.strings");

				Log.LogMessage(MessageImportance.High, "\t=> Processing {0}", realPath);
				GenerateStrings(filePath, outputFile);
				Log.LogMessage(MessageImportance.High, "\t\t=> Generated {0}", outputFile);

				generatedFiles.Add(outputFile);
			}

			GeneratedStrings = generatedFiles.Select(x => (ITaskItem)new TaskItem(x)).ToArray();
			Log.LogMessage(MessageImportance.High, "===> End preprocessing strings files, generate {0} Localizable.strings", GeneratedStrings.Length);

			return true;
		}

		private void GenerateStrings(string inputPath, string outputFile)
		{
			XElement rootElement = XElement.Load(inputPath);
			IEnumerable<Tuple<string, string>> items = from dataElement in rootElement.Descendants("data")
													   let key = dataElement.Attribute("name").Value
													   let valueElement = dataElement.Element("value")
													   where valueElement != null
													   let value = valueElement.Value
													   select new Tuple<string, string>(key, value);

			File.WriteAllLines(outputFile, 
				items.Select(item => 
					string.Format("\"{0}\" = \"{1}\";", ProcessKey(item.Item1), ProcessValue(item.Item2))
				));
		}

		private string ProcessKey(string key)
		{
			return key.Replace(".", "__");
		}

		private string ProcessValue(string value)
		{
			return value.Replace("\\", "\\\\")
				.Replace("\"", "\\\"")
				.Replace("\n", "\\n")
				;
		}
	}
}
