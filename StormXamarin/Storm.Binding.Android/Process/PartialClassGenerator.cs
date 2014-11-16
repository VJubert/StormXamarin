﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.CSharp;
using Storm.Binding.Android.Data;
using System.CodeDom;

namespace Storm.Binding.Android.Process
{
	class PartialClassGenerator
	{
		public void Generate(ActivityInfo activityInformations, List<XmlAttribute> bindingInformations, List<XmlResource> resourceCollection)
		{
			CodeCompileUnit codeUnit = new CodeCompileUnit();
			CodeNamespace codeNamespace = new CodeNamespace(activityInformations.NamespaceName);
			codeUnit.Namespaces.Add(codeNamespace);

			CodeTypeDeclaration classDeclaration = new CodeTypeDeclaration(activityInformations.ClassName)
			{
				IsClass = true,
				IsPartial = true,
				TypeAttributes = TypeAttributes.Public,
			};

			codeNamespace.Types.Add(classDeclaration);
			codeNamespace.Imports.Add(new CodeNamespaceImport("System.Collections.Generic"));
			codeNamespace.Imports.Add(new CodeNamespaceImport("Storm.Mvvm.Android.Bindings"));

			CodeMemberMethod overrideMethod = new CodeMemberMethod
			{
				Attributes = MemberAttributes.Family | MemberAttributes.Override,
				Name = "GetBindingPaths",
				ReturnType = new CodeTypeReference("List<BindingObject>")
			};

			GenerateMethodContent(overrideMethod, bindingInformations, resourceCollection);
			classDeclaration.Members.Add(overrideMethod);


			CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
			CodeGeneratorOptions options = new CodeGeneratorOptions
			{
				BlankLinesBetweenMembers = true,
				BracingStyle = "C",
				IndentString = "\t"
			};
			using (StreamWriter writer = new StreamWriter(activityInformations.OutputFile))
			{
				provider.GenerateCodeFromCompileUnit(codeUnit, writer, options);
			}
		}

		private void GenerateMethodContent(CodeMemberMethod method, IEnumerable<XmlAttribute> bindings, List<XmlResource> resourceCollection)
		{
			CodeObjectCreateExpression resultCollectionInitializer = new CodeObjectCreateExpression("List<BindingObject>");
			method.Statements.Add(new CodeVariableDeclarationStatement("List<BindingObject>", "result", resultCollectionInitializer));

			CodeVariableReferenceExpression resultReference = new CodeVariableReferenceExpression("result");

			int objectCounter = 0;
			int expressionCounter = 0;

			Dictionary<string, CodeVariableReferenceExpression> resources = GenerateResourceCode(method, resourceCollection);
			List<BindingExpression> expressions = bindings.Select(x => EvaluateBindingExpression(x, resources)).Where(x => x != null).ToList();
			// group by binding objects
			foreach (IGrouping<string, BindingExpression> bindingExpressions in expressions.GroupBy(x => x.TargetObjectId))
			{
				//create binding objects and get a code reference on it
				CodeObjectCreateExpression objectCreateExpression = new CodeObjectCreateExpression("BindingObject", new CodePrimitiveExpression(bindingExpressions.Key));
				string objectName = string.Format("o{0}", objectCounter++);
				method.Statements.Add(new CodeVariableDeclarationStatement("BindingObject", objectName, objectCreateExpression));

				CodeVariableReferenceExpression objectReference = new CodeVariableReferenceExpression(objectName);

				//add the object to the result list
				method.Statements.Add(new CodeMethodInvokeExpression(resultReference, "Add", objectReference));

				//add all expressions
				foreach (BindingExpression expr in expressions)
				{
					CodeObjectCreateExpression exprCreateExpression = new CodeObjectCreateExpression("BindingExpression", new CodePrimitiveExpression(expr.TargetFieldId), new CodePrimitiveExpression(expr.SourcePath));
					string exprName = string.Format("e{0}", expressionCounter++);
					method.Statements.Add(new CodeVariableDeclarationStatement("BindingExpression", exprName, exprCreateExpression));
					CodeVariableReferenceExpression exprReference = new CodeVariableReferenceExpression(exprName);

					if (expr.ConverterReference != null)
					{
						method.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(exprReference, "Converter"), expr.ConverterReference));
					}

					if (expr.ConverterParameter != null)
					{
						method.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(exprReference, "ConverterParameter"), new CodePrimitiveExpression(expr.ConverterParameter)));
					}

					if (expr.Mode != BindingExpression.BindingModes.OneWay)
					{
						method.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(exprReference, "Mode"), new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("BindingMode"), expr.Mode.ToString())));
					}

					if (!string.IsNullOrWhiteSpace(expr.UpdateEvent))
					{
						method.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(exprReference, "UpdateEvent"), new CodePrimitiveExpression(expr.UpdateEvent)));
					}

					method.Statements.Add(new CodeMethodInvokeExpression(objectReference, "AddExpression", exprReference));
				}
			}


			method.Statements.Add(new CodeMethodReturnStatement(resultReference));
		}

		private Dictionary<string, CodeVariableReferenceExpression> GenerateResourceCode(CodeMemberMethod method, IEnumerable<XmlResource> resourceCollection)
		{
			const string RESOURCE_NAME_FORMAT = "rsx_{0}";

			int resourceIndex = 0;
			Dictionary<string, CodeVariableReferenceExpression> res = new Dictionary<string, CodeVariableReferenceExpression>();
			foreach (ResourceConverter converter in resourceCollection.OfType<ResourceConverter>())
			{
				string resourceName = string.Format(RESOURCE_NAME_FORMAT, resourceIndex++);

				CodeObjectCreateExpression objectCreateExpression = new CodeObjectCreateExpression(converter.ClassName);
				method.Statements.Add(new CodeVariableDeclarationStatement(converter.ClassName, resourceName, objectCreateExpression));

				CodeVariableReferenceExpression resourceReference = new CodeVariableReferenceExpression(resourceName);
				res.Add(converter.Key, resourceReference);
			}
			return res;
		}

		private BindingExpression EvaluateBindingExpression(XmlAttribute attribute, Dictionary<string, CodeVariableReferenceExpression> resources)
		{
			const string BINDING_EXPRESSION_START = "{Binding ";
			const string BINDING_EXPRESSION_END = "}";

			const string RESOURCE_ACCESS_START = "{Resource ";
			const string RESOURCE_ACCESS_END = "}";

			string bindingValue = attribute.Value;
			if (bindingValue.StartsWith(BINDING_EXPRESSION_START) && bindingValue.EndsWith(BINDING_EXPRESSION_END))
			{
				bindingValue = bindingValue.Substring(BINDING_EXPRESSION_START.Length);
				bindingValue = bindingValue.Substring(0, bindingValue.Length - 1).Trim();

				//Format des bindings expression
				/*{Binding [Path=]Path[, Converter={Resource ConverterKey}[, ConverterParameter=(VALUE|'SPACED VALUE')]][, Mode=(OneTime|OneWay|TwoWay)[, UpdateEvent=EventName]]
				 * Le nom de l'attribut Path est optionel
				 * ConverterParameter n'est autorisé que si il y a un converter
				 * UpdateEvent n'est autorisé qu'avec un mode "TwoWay"
				 */
				BindingExpression expression = new BindingExpression
				{
					TargetFieldId = attribute.LocalName,
					TargetObjectId = attribute.AttachedId
				};
				bool pathFound = false;
				Regex pattern = new Regex("^([a-zA-Z0-9]+) ?= ?(.+)$");
				foreach (string attr in bindingValue.Split(',').Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()))
				{
					string attributeName = "";
					string attributeValue = "";

					if (pattern.IsMatch(attr))
					{
						Match matchResult = pattern.Match(attr);
						attributeName = matchResult.Groups[1].Value;
						attributeValue = matchResult.Groups[2].Value;
					}
					else //on suppose que c'est le path
					{
						attributeName = "Path";
						attributeValue = attr;
					}

					if (attributeName == "Path")
					{
						if (pathFound)
						{
							Console.WriteLine("Binding error : missing attribute name in " + bindingValue);
							return null;
						}
						pathFound = true;

						expression.SourcePath = attributeValue;
					}
					else if (attributeName == "Converter")
					{
						// need to parse {Resource ... }
						if (attributeValue.StartsWith(RESOURCE_ACCESS_START) && attributeValue.EndsWith(RESOURCE_ACCESS_END))
						{
							attributeValue = attributeValue.Substring(BINDING_EXPRESSION_START.Length);
							attributeValue = attributeValue.Substring(0, attributeValue.Length - 1).Trim();

							if (resources.ContainsKey(attributeValue))
							{
								expression.ConverterReference = resources[attributeValue];
							}
							else
							{
								Console.WriteLine("Binding error : no converter in ressource for " + bindingValue);
							}
						}
						else
						{
							Console.WriteLine("Binding Error : invalid converter " + bindingValue);
							return null;
						}
					}
					else if (attributeName == "ConverterParameter")
					{
						if (attributeValue.StartsWith("'") && attributeValue.EndsWith("'"))
						{
							attributeValue = attributeValue.Substring(1, attributeValue.Length - 2).Replace("\\\\", "\\").Replace("\\'", "'");
						}
						expression.ConverterParameter = attributeValue;
					}
					else if (attributeName == "Mode")
					{
						BindingExpression.BindingModes enumResult;
						if (Enum.TryParse(attributeValue, false, out enumResult))
						{
							expression.Mode = enumResult;
						}
						else
						{
							Console.WriteLine("Binding error : unrecognized Binding Mode " + attributeValue);
							return null;
						}
					}
					else if (attributeName == "UpdateEvent")
					{
						expression.UpdateEvent = attributeValue;
					}
				}

				if (!pathFound)
				{
					Console.WriteLine("Binding Error : no binding path in " + bindingValue);
					return null;
				}

				if (expression.ConverterReference == null && !string.IsNullOrWhiteSpace(expression.ConverterParameter))
				{
					Console.WriteLine("Binding Error : ConverterParameter is not authorized if no converter is specified " + bindingValue);
					return null;
				}

				if (expression.Mode != BindingExpression.BindingModes.TwoWay && !string.IsNullOrWhiteSpace(expression.UpdateEvent))
				{
					Console.WriteLine("Binding Error : UpdateEvent is not authorized if Mode is not TwoWay " + bindingValue);
					return null;
				}

				return expression;
			}
			Console.WriteLine("Binding error : " + bindingValue);
			return null;
		}

		class BindingExpression
		{
			public enum BindingModes
			{
				OneTime,
				OneWay,
				TwoWay
			}

			/* Mandatory fields */
			public string TargetObjectId { get; set; }

			public string TargetFieldId { get; set; }

			public string SourcePath { get; set; }

			/* Optional fields */
			public CodeVariableReferenceExpression ConverterReference { get; set; }

			// only if converter specified
			public string ConverterParameter { get; set; }

			// default value : OneWay
			public BindingModes Mode { get; set; }

			// only if Mode = TwoWay
			public string UpdateEvent { get; set; }

			public BindingExpression()
			{
				Mode = BindingModes.OneWay;
			}
		}
	}
}
