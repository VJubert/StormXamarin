﻿using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Storm.Binding.AndroidTarget.Data;

namespace Storm.Binding.AndroidTarget.Process
{
	// ReSharper disable BitwiseOperatorOnEnumWithoutFlags
	public abstract class AbstractClassGenerator
	{
		private const string FIELD_FORMAT = "_autogeneratedfield_{0}";
		private const string RESOURCE_FORMAT = "RSX_{0}";
		private const string VIEW_SELECTOR_INTERNAL_NAME = "viewSelector";
		private const string ADAPTER_INTERNAL_NAME = "adapter";
		private const string OBJECT_FORMAT = "bindingObject_{0}";
		private const string EXPRESSION_FORMAT = "bindingExpression_{0}";


		private int _fieldCounter;
		private int _resourceCounter;
		private int _objectCounter;
		private int _expressionCounter;


		public bool IsPartialClass { get; set; }

		public IEnumerable<string> Namespaces { get; set; }

		public string NamespaceName { get; set; }

		public string ClassName { get; set; }

		public CodeTypeReference BaseClass { get; set; }

		public IEnumerable<IdViewObject> ViewElements { get; set; }

		public IEnumerable<XmlAttribute> BindingAttributes { get; set; }

		public IEnumerable<XmlResource> Resources { get; set; } 

		protected AbstractClassGenerator(string namespaceName, string className, CodeTypeReference baseClass)
		{
			NamespaceName = namespaceName;
			ClassName = className;
			BaseClass = baseClass;
		}

		public void Generate(string outputFile)
		{
			CodeCompileUnit codeUnit = new CodeCompileUnit();

			CodeNamespace globalNamespace = new CodeNamespace("");
			codeUnit.Namespaces.Add(globalNamespace);

			CodeNamespace codeNamespace = new CodeNamespace(NamespaceName);
			codeUnit.Namespaces.Add(codeNamespace);

			List<string> usings = new List<string>
			{
				"System",
				"System.Collections.Generic",
				"Android.App",
				"Android.Content",
				"Android.Runtime",
				"Android.Views",
				"Android.Widget",
				"Android.OS",
				"Storm.Mvvm",
				"Storm.Mvvm.Bindings",
				"Storm.Mvvm.ViewSelectors",
			};
			if (Namespaces != null && Namespaces.Any())
			{
				usings.AddRange(Namespaces);
			}

			foreach (string name in usings)
			{
				globalNamespace.Imports.Add(new CodeNamespaceImport(name));
			}

			CodeTypeDeclaration classDeclaration = new CodeTypeDeclaration(ClassName)
			{
				IsClass = true,
				IsPartial = IsPartialClass,
				TypeAttributes = TypeAttributes.Public,
			};
			if (BaseClass != null)
			{
				classDeclaration.BaseTypes.Add(BaseClass);
			}
			codeNamespace.Types.Add(classDeclaration);

			Dictionary<string, CodePropertyReferenceExpression> viewElementReferences = GenerateViewElementProperty(classDeclaration);
			Dictionary<string, CodePropertyReferenceExpression> resourceReferences = GenerateResourceProperty(classDeclaration);
			List<BindingExpression> bindingExpressions = BindingAttributes.Select(x => ClassGeneratorHelper.EvaluateBindingExpression(x, resourceReferences)).Where(x => x != null).ToList();

			GenerateAdapterProperty(classDeclaration, bindingExpressions, viewElementReferences);


			CodeMemberMethod overrideMethod = new CodeMemberMethod
			{
				Attributes = GetOverrideMethodVisibility() | MemberAttributes.Override,
				Name = GetOverrideMethodName(),
				ReturnType = new CodeTypeReference("List<BindingObject>")
			};

			GenerateMethodContent(overrideMethod, bindingExpressions, viewElementReferences);
			classDeclaration.Members.Add(overrideMethod);


			#region File writing

			CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
			CodeGeneratorOptions options = new CodeGeneratorOptions
			{
				BlankLinesBetweenMembers = true,
				BracingStyle = "C",
				IndentString = "\t"
			};

			string contentString;
			using (StringWriter stringWriter = new StringWriter())
			{
				provider.GenerateCodeFromCompileUnit(codeUnit, stringWriter, options);

				string content = stringWriter.GetStringBuilder().ToString();

				Regex commentRegex = new Regex("<auto-generated>.*</auto-generated>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
				contentString = commentRegex.Replace(content, "This file was generated by binding preprocessing system for Android");
			}

			if (File.Exists(outputFile))
			{
				using (StreamReader reader = new StreamReader(outputFile))
				{
					string actualContent = reader.ReadToEnd();
					if (actualContent == contentString)
					{
						return;
					}
				}
				File.Delete(outputFile);
			}

			using (StreamWriter writer = new StreamWriter(File.OpenWrite(outputFile)))
			{
				writer.Write(contentString);
			}

			#endregion
		}

		private void GenerateMethodContent(CodeMemberMethod method, IEnumerable<BindingExpression> expressions, Dictionary<string, CodePropertyReferenceExpression> viewElementReferences)
		{
			CodeObjectCreateExpression resultCollectionInitializer = new CodeObjectCreateExpression("List<BindingObject>");
			method.Statements.Add(new CodeVariableDeclarationStatement("List<BindingObject>", "result", resultCollectionInitializer));

			CodeVariableReferenceExpression resultReference = new CodeVariableReferenceExpression("result");

			// group by binding objects
			foreach (IGrouping<string, BindingExpression> bindingExpressions in expressions.GroupBy(x => x.TargetObjectId))
			{
				//create binding objects and get a code reference on it
				string objectName = string.Format(OBJECT_FORMAT, _objectCounter++);
				CodeObjectCreateExpression objectCreateExpression = new CodeObjectCreateExpression("BindingObject", viewElementReferences[bindingExpressions.Key]);
				method.Statements.Add(new CodeVariableDeclarationStatement("BindingObject", objectName, objectCreateExpression));

				CodeVariableReferenceExpression objectReference = new CodeVariableReferenceExpression(objectName);

				//add the object to the result list
				method.Statements.Add(new CodeMethodInvokeExpression(resultReference, "Add", objectReference));

				//add all expressions
				foreach (BindingExpression expr in bindingExpressions)
				{
					CodeObjectCreateExpression exprCreateExpression = new CodeObjectCreateExpression("BindingExpression", new CodePrimitiveExpression(expr.TargetFieldId), new CodePrimitiveExpression(expr.SourcePath));
					string exprName = string.Format(EXPRESSION_FORMAT, _expressionCounter++);
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

		private void GenerateAdapterProperty(CodeTypeDeclaration classDeclaration, IEnumerable<BindingExpression> expressions, Dictionary<string, CodePropertyReferenceExpression> viewElementReferences)
		{
			foreach (BindingExpression expression in expressions.Where(x => x.ViewSelectorReference != null))
			{
				// Generate property for adapter
				string adapterName = string.Format(RESOURCE_FORMAT, _resourceCounter++);
				List<CodeStatement> statements = new List<CodeStatement>();
				//Create adapter
				CodeObjectCreateExpression createAdapterExpression = new CodeObjectCreateExpression("BindableAdapter", expression.ViewSelectorReference);
				statements.Add(new CodeVariableDeclarationStatement("BindableAdapter", ADAPTER_INTERNAL_NAME, createAdapterExpression));

				CodeVariableReferenceExpression adapterReference = new CodeVariableReferenceExpression(ADAPTER_INTERNAL_NAME);
				//Execute MyViewObject.MyAdapterProperty = MyNewAdapter
				statements.Add(new CodeAssignStatement(
					new CodePropertyReferenceExpression(new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), expression.TargetObjectId), expression.TargetFieldId),
					adapterReference
					));

				viewElementReferences.Add(adapterName, GenerateProxyProperty(classDeclaration, adapterName, "BindableAdapter", adapterReference, statements));
				

				//Change target of binding expression
				expression.TargetObjectId = adapterName;
				expression.TargetFieldId = "Collection";
			}
		}

		protected Dictionary<string, CodePropertyReferenceExpression> GenerateViewElementProperty(CodeTypeDeclaration classDeclaration)
		{
			Dictionary<string, CodePropertyReferenceExpression> result = new Dictionary<string, CodePropertyReferenceExpression>();
			if (ViewElements != null)
			{
				foreach (IdViewObject viewItem in ViewElements)
				{
					CodeMethodInvokeExpression findViewExpression = new CodeMethodInvokeExpression(
						GetFindViewReferenceExpression(viewItem.TypeName),
						new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("Resource.Id"), viewItem.Id)
					);
					
					result.Add(viewItem.Id, GenerateProxyProperty(classDeclaration, viewItem.Id, viewItem.TypeName, findViewExpression));
				}
			}

			return result;
		}

		protected Dictionary<string, CodePropertyReferenceExpression> GenerateResourceProperty(CodeTypeDeclaration classDeclaration)
		{
			Dictionary<string, CodePropertyReferenceExpression> result = new Dictionary<string, CodePropertyReferenceExpression>();

			if (Resources != null)
			{
				foreach (XmlResource resource in Resources)
				{
					string resourceName = string.Format(RESOURCE_FORMAT, _resourceCounter++);
					if (resource is ResourceConverter)
					{
						ResourceConverter converter = resource as ResourceConverter;

						CodeObjectCreateExpression objectCreateExpression = new CodeObjectCreateExpression(converter.ClassName);
						
						result.Add(converter.Key, GenerateProxyProperty(classDeclaration, resourceName, converter.ClassName, objectCreateExpression));
					}
					else if (resource is ResourceDataTemplate)
					{
						ResourceDataTemplate dataTemplate = resource as ResourceDataTemplate;

						List<CodeStatement> getStatements = new List<CodeStatement>();
						CodeFieldReferenceExpression rIdReference = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("Resource.Layout"), dataTemplate.ResourceId);
						getStatements.Add(new CodeMethodReturnStatement(rIdReference));
						
						result.Add(dataTemplate.Key, GenerateProperty(classDeclaration, resourceName, "int", getStatements, null));
					}
					else if (resource is ResourceViewSelector)
					{
						ResourceViewSelector viewSelector = resource as ResourceViewSelector;

						List<CodeStatement> beforeAssignStatements = new List<CodeStatement>();

						
						//Create view selector item
						CodeObjectCreateExpression objectCreateExpression = new CodeObjectCreateExpression(viewSelector.ClassName, GetLayoutInflaterReferenceExpression());
						beforeAssignStatements.Add(new CodeVariableDeclarationStatement(viewSelector.ClassName, VIEW_SELECTOR_INTERNAL_NAME, objectCreateExpression));

						//Get reference on new object
						CodeVariableReferenceExpression resourceReference = new CodeVariableReferenceExpression(VIEW_SELECTOR_INTERNAL_NAME);
						
						//Affect property to view selector
						foreach (KeyValuePair<string, string> property in viewSelector.Properties)
						{
							// First, extract value of the property
							// Could be {Resource ...} or a simple string

							CodeExpression propertyValueExpression;
							if (ClassGeneratorHelper.IsResourceReferenceExpression(property.Value))
							{
								string resourceKey = ClassGeneratorHelper.ExtractResourceKey(property.Value);

								if (result.ContainsKey(resourceKey))
								{
									propertyValueExpression = result[resourceKey];
								}
								else
								{
									BindingPreprocess.Logger.LogError("Error, Resource with key = {0} does not exists", resourceKey);
									continue;
								}
							}
							else
							{
								propertyValueExpression = new CodePrimitiveExpression(property.Value);
							}

							beforeAssignStatements.Add(new CodeAssignStatement(
								new CodePropertyReferenceExpression(resourceReference, property.Key),
								propertyValueExpression
								));
						}

						result.Add(viewSelector.Key, GenerateProxyProperty(classDeclaration, resourceName, viewSelector.ClassName, resourceReference, beforeAssignStatements));
					}
					else
					{
						BindingPreprocess.Logger.LogError("Resource type {0} is not supported", resource.GetType().FullName);
					}
				}
			}

			return result;
		}

		protected CodePropertyReferenceExpression GenerateProxyProperty(CodeTypeDeclaration classDeclaration, string propertyName, string propertyType, CodeExpression rightAssignExpression)
		{
			CodeFieldReferenceExpression fieldReference = GenerateField(classDeclaration, propertyType);

			List<CodeStatement> getStatements = new List<CodeStatement>();

			CodeConditionStatement ifStatement = new CodeConditionStatement(
				new CodeBinaryOperatorExpression(fieldReference, CodeBinaryOperatorType.IdentityEquality, new CodePrimitiveExpression(null)),
				new CodeAssignStatement(fieldReference, rightAssignExpression)
			);

			getStatements.Add(ifStatement);
			getStatements.Add(new CodeMethodReturnStatement(fieldReference));

			return GenerateProperty(classDeclaration, propertyName, propertyType, getStatements, null);
		}

		protected CodePropertyReferenceExpression GenerateProxyProperty(CodeTypeDeclaration classDeclaration, string propertyName, string propertyType, CodeExpression rightAssignExpression, List<CodeStatement> assignStatements)
		{
			CodeFieldReferenceExpression fieldReference = GenerateField(classDeclaration, propertyType);

			List<CodeStatement> getStatements = new List<CodeStatement>();

			assignStatements.Add(new CodeAssignStatement(fieldReference, rightAssignExpression));

			CodeConditionStatement ifStatement = new CodeConditionStatement(
				new CodeBinaryOperatorExpression(fieldReference, CodeBinaryOperatorType.IdentityEquality, new CodePrimitiveExpression(null)),
				assignStatements.ToArray()
			);

			getStatements.Add(ifStatement);
			getStatements.Add(new CodeMethodReturnStatement(fieldReference));

			return GenerateProperty(classDeclaration, propertyName, propertyType, getStatements, null);
		}


		protected CodeFieldReferenceExpression GenerateField(CodeTypeDeclaration classDeclaration, string fieldType)
		{
			string fieldName = string.Format(FIELD_FORMAT, _fieldCounter++);
			CodeMemberField field = new CodeMemberField(fieldType, fieldName)
			{
				Attributes = MemberAttributes.Private
			};

			classDeclaration.Members.Add(field);
			return new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName);
		}

		protected CodePropertyReferenceExpression GenerateProperty(CodeTypeDeclaration classDeclaration, string propertyName, string propertyType, List<CodeStatement> getStatements, List<CodeStatement> setStatements)
		{
			CodeTypeReference type = ("int" == propertyType) ? new CodeTypeReference(typeof (int)) : new CodeTypeReference(propertyType);
			CodeMemberProperty property = new CodeMemberProperty
			{

				Attributes = MemberAttributes.Family | MemberAttributes.Final,
				Name = propertyName,
				Type = type, 
				HasGet = (getStatements != null && getStatements.Any()), 
				HasSet = (setStatements != null && setStatements.Any())
			};

			if (property.HasGet && getStatements != null)
			{
				foreach (CodeStatement statement in getStatements)
				{
					property.GetStatements.Add(statement);
				}
			}

			if (property.HasSet && setStatements != null)
			{
				foreach (CodeStatement statement in setStatements)
				{
					property.SetStatements.Add(statement);
				}
			}

			classDeclaration.Members.Add(property);
			return new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), propertyName);
		}

		protected abstract CodeMethodReferenceExpression GetFindViewReferenceExpression(string type);

		protected abstract CodeExpression GetLayoutInflaterReferenceExpression();

		protected abstract string GetOverrideMethodName();

		protected abstract MemberAttributes GetOverrideMethodVisibility();
	}
	// ReSharper restore BitwiseOperatorOnEnumWithoutFlags
}
