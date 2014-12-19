﻿using System;
using System.CodeDom;
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
	//public class PartialClassGenerator
	//{
	//	public void Generate(ActivityInfo activityInformations, List<string> additionalNamespaces, List<IdViewObject> views, List<XmlAttribute> bindingInformations, List<XmlResource> resourceCollection)
	//	{
	//		CodeCompileUnit codeUnit = new CodeCompileUnit();

	//		CodeNamespace globalNamespace = new CodeNamespace("");
	//		codeUnit.Namespaces.Add(globalNamespace);

	//		CodeNamespace codeNamespace = new CodeNamespace(activityInformations.NamespaceName);
	//		codeUnit.Namespaces.Add(codeNamespace);

	//		List<string> namespaces = new List<string>()
	//		{
	//			"System",
	//			"System.Collections.Generic",
	//			"Android.App",
	//			"Android.Content",
	//			"Android.Runtime",
	//			"Android.Views",
	//			"Android.Widget",
	//			"Android.OS",
	//			"Storm.Mvvm",
	//			"Storm.Mvvm.Bindings"
	//		};
	//		if (additionalNamespaces != null && additionalNamespaces.Count > 0)
	//		{
	//			namespaces.AddRange(additionalNamespaces);
	//		}

	//		foreach (string name in namespaces)
	//		{
	//			globalNamespace.Imports.Add(new CodeNamespaceImport(name));
	//		}

	//		CodeTypeDeclaration classDeclaration = new CodeTypeDeclaration(activityInformations.ClassName)
	//		{
	//			IsClass = true,
	//			IsPartial = true,
	//			TypeAttributes = TypeAttributes.Public,
	//		};
	//		codeNamespace.Types.Add(classDeclaration);

	//		GenerateClassProperty(classDeclaration, views, activityInformations);

	//		CodeMemberMethod overrideMethod = new CodeMemberMethod
	//		{
	//			Attributes = MemberAttributes.Family | MemberAttributes.Override,
	//			Name = "GetBindingPaths",
	//			ReturnType = new CodeTypeReference("List<BindingObject>")
	//		};

	//		GenerateMethodContent(overrideMethod, classDeclaration, bindingInformations, resourceCollection, activityInformations);
	//		classDeclaration.Members.Add(overrideMethod);


	//		CodeDomProvider provider = CodeDomProvider.CreateProvider("CSharp");
	//		CodeGeneratorOptions options = new CodeGeneratorOptions
	//		{
	//			BlankLinesBetweenMembers = true,
	//			BracingStyle = "C",
	//			IndentString = "\t"
	//		};

	//		string contentString;
	//		using (StringWriter stringWriter = new StringWriter())
	//		{
	//			provider.GenerateCodeFromCompileUnit(codeUnit, stringWriter, options);

	//			string content = stringWriter.GetStringBuilder().ToString();

	//			Regex commentRegex = new Regex("<auto-generated>.*</auto-generated>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
	//			contentString = commentRegex.Replace(content, "This file was generated by binding preprocessing system for Android");
	//		}

	//		if (File.Exists(activityInformations.OutputFile))
	//		{
	//			using (StreamReader reader = new StreamReader(activityInformations.OutputFile))
	//			{
	//				string actualContent = reader.ReadToEnd();
	//				if (actualContent == contentString)
	//				{
	//					return;
	//				}
	//			}
	//			File.Delete(activityInformations.OutputFile);
	//		}

	//		using (StreamWriter writer = new StreamWriter(File.OpenWrite(activityInformations.OutputFile)))
	//		{
	//			writer.Write(contentString);
	//		}
	//	}

	//	/// <summary>
	//	/// This method generate all property to access view element with id just as Xaml do
	//	/// </summary>
	//	/// <param name="classDeclaration">The class container</param>
	//	/// <param name="views">The list of view elements</param>
	//	/// <param name="activityInformations"></param>
	//	private void GenerateClassProperty(CodeTypeDeclaration classDeclaration, IEnumerable<IdViewObject> views, ActivityInfo activityInformations)
	//	{
	//		foreach (IdViewObject viewItem in views)
	//		{
	//			//Console.WriteLine("Generating property for field => " + viewItem.Id + " / " + viewItem.TypeName);
	//			//generate a field with _id as name
	//			//generate a readonly property with Id as name
	//			// get { _id ?? (_id = findViewById(Resources.Id.<ID>))

	//			string fieldName = string.Format("_{0}", viewItem.Id);
	//			CodeMemberField field = new CodeMemberField(viewItem.TypeName, fieldName)
	//			{
	//				Attributes = MemberAttributes.Private
	//			};

	//			classDeclaration.Members.Add(field);

	//			CodeMemberProperty property = new CodeMemberProperty
	//			{
	//				Attributes = MemberAttributes.Family | MemberAttributes.Final, 
	//				Name = viewItem.Id,
	//				Type = new CodeTypeReference(viewItem.TypeName)
	//			};

	//			CodeMethodReferenceExpression findViewReferenceExpression;
	//			if (activityInformations.IsFragment)
	//			{
	//				findViewReferenceExpression = new CodeMethodReferenceExpression(
	//					new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "RootView"),
	//					"FindViewById",
	//					new CodeTypeReference(viewItem.TypeName)
	//					);
	//			}
	//			else
	//			{
	//				findViewReferenceExpression = new CodeMethodReferenceExpression(
	//					new CodeThisReferenceExpression(), 
	//					"FindViewById", 
	//					new CodeTypeReference(viewItem.TypeName));
	//			}

	//			CodeMethodInvokeExpression findViewExpression = new CodeMethodInvokeExpression(
	//				findViewReferenceExpression,
	//				new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("Resource.Id"), viewItem.Id)
	//			);

	//			CodeFieldReferenceExpression fieldReference = new CodeFieldReferenceExpression(new CodeThisReferenceExpression(), fieldName);
	//			CodeConditionStatement ifStatement = new CodeConditionStatement(
	//				new CodeBinaryOperatorExpression(fieldReference, CodeBinaryOperatorType.IdentityEquality, new CodePrimitiveExpression(null)),
	//				new CodeAssignStatement(fieldReference, findViewExpression)
	//			);

	//			property.GetStatements.Add(ifStatement);
	//			property.GetStatements.Add(new CodeMethodReturnStatement(fieldReference));

	//			classDeclaration.Members.Add(property);
	//		}
	//	}

	//	private void GenerateMethodContent(CodeMemberMethod method, CodeTypeDeclaration classDeclaration, IEnumerable<XmlAttribute> bindings, IEnumerable<XmlResource> resourceCollection, ActivityInfo activityInformations)
	//	{
	//		CodeObjectCreateExpression resultCollectionInitializer = new CodeObjectCreateExpression("List<BindingObject>");
	//		method.Statements.Add(new CodeVariableDeclarationStatement("List<BindingObject>", "result", resultCollectionInitializer));

	//		CodeVariableReferenceExpression resultReference = new CodeVariableReferenceExpression("result");

	//		int objectCounter = 0;
	//		int expressionCounter = 0;

	//		Dictionary<string, CodeVariableReferenceExpression> resources = GenerateResourceCode(method, resourceCollection.ToList(), activityInformations);
	//		List<BindingExpression> expressions = bindings.Select(x => ClassGeneratorHelper.EvaluateBindingExpression(x, resources)).Where(x => x != null).ToList();

	//		GenerateAdapterSystem(method, classDeclaration, expressions);

	//		// group by binding objects
	//		foreach (IGrouping<string, BindingExpression> bindingExpressions in expressions.GroupBy(x => x.TargetObjectId))
	//		{
	//			//create binding objects and get a code reference on it
	//			CodeObjectCreateExpression objectCreateExpression = new CodeObjectCreateExpression("BindingObject", new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), bindingExpressions.Key));
	//			string objectName = string.Format("o{0}", objectCounter++);
	//			method.Statements.Add(new CodeVariableDeclarationStatement("BindingObject", objectName, objectCreateExpression));

	//			CodeVariableReferenceExpression objectReference = new CodeVariableReferenceExpression(objectName);

	//			//add the object to the result list
	//			method.Statements.Add(new CodeMethodInvokeExpression(resultReference, "Add", objectReference));

	//			//add all expressions
	//			foreach (BindingExpression expr in bindingExpressions)
	//			{
	//				CodeObjectCreateExpression exprCreateExpression = new CodeObjectCreateExpression("BindingExpression", new CodePrimitiveExpression(expr.TargetFieldId), new CodePrimitiveExpression(expr.SourcePath));
	//				string exprName = string.Format("e{0}", expressionCounter++);
	//				method.Statements.Add(new CodeVariableDeclarationStatement("BindingExpression", exprName, exprCreateExpression));
	//				CodeVariableReferenceExpression exprReference = new CodeVariableReferenceExpression(exprName);

	//				if (expr.ConverterReference != null)
	//				{
	//					method.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(exprReference, "Converter"), expr.ConverterReference));
	//				}

	//				if (expr.ConverterParameter != null)
	//				{
	//					method.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(exprReference, "ConverterParameter"), new CodePrimitiveExpression(expr.ConverterParameter)));
	//				}

	//				if (expr.Mode != BindingExpression.BindingModes.OneWay)
	//				{
	//					method.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(exprReference, "Mode"), new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("BindingMode"), expr.Mode.ToString())));
	//				}

	//				if (!string.IsNullOrWhiteSpace(expr.UpdateEvent))
	//				{
	//					method.Statements.Add(new CodeAssignStatement(new CodePropertyReferenceExpression(exprReference, "UpdateEvent"), new CodePrimitiveExpression(expr.UpdateEvent)));
	//				}

	//				method.Statements.Add(new CodeMethodInvokeExpression(objectReference, "AddExpression", exprReference));
	//			}
	//		}


	//		method.Statements.Add(new CodeMethodReturnStatement(resultReference));
	//	}

	//	private void GenerateAdapterSystem(CodeMemberMethod method, CodeTypeDeclaration classDeclaration, IEnumerable<BindingExpression> expressions)
	//	{
	//		int adapterIndex = 0;
	//		const string ADAPTER_FORMAT = "AutoGenerated_Adapter{0}";
	//		foreach (BindingExpression expression in expressions.Where(x => x.ViewSelectorReference != null))
	//		{
	//			// Generate property for adapter
	//			string adapterName = string.Format(ADAPTER_FORMAT, adapterIndex++);
	//			CodeSnippetTypeMember propertySnippet = new CodeSnippetTypeMember(string.Format("public BindableAdapter {0} {1}", adapterName, "{get;set;}"));
	//			classDeclaration.Members.Add(propertySnippet);

	//			CodePropertyReferenceExpression propertyReference = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), adapterName);
	//			//Create Adapter and affect to property
	//			CodeObjectCreateExpression createAdapterExpression = new CodeObjectCreateExpression("BindableAdapter", expression.ViewSelectorReference);
	//			method.Statements.Add(new CodeAssignStatement(propertyReference, createAdapterExpression));

	//			//Execute MyViewObject.MyAdapterProperty = MyNewAdapter
	//			method.Statements.Add(new CodeAssignStatement(
	//				new CodePropertyReferenceExpression(new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), expression.TargetObjectId), expression.TargetFieldId),
	//				propertyReference
	//				));

	//			//Change target of binding expression
	//			expression.TargetObjectId = adapterName;
	//			expression.TargetFieldId = "Collection";
	//		}
	//	}

	//	private Dictionary<string, CodeVariableReferenceExpression> GenerateResourceCode(CodeMemberMethod method, 
	//		List<XmlResource> resourceCollection, ActivityInfo activityInformations)
	//	{
	//		const string RESOURCE_NAME_FORMAT = "rsx_{0}";

	//		int resourceIndex = 0;
	//		Dictionary<string, CodeVariableReferenceExpression> res = new Dictionary<string, CodeVariableReferenceExpression>();
	//		foreach (ResourceConverter converter in resourceCollection.OfType<ResourceConverter>())
	//		{
	//			string resourceName = string.Format(RESOURCE_NAME_FORMAT, resourceIndex++);

	//			CodeObjectCreateExpression objectCreateExpression = new CodeObjectCreateExpression(converter.ClassName);
	//			method.Statements.Add(new CodeVariableDeclarationStatement(converter.ClassName, resourceName, objectCreateExpression));

	//			CodeVariableReferenceExpression resourceReference = new CodeVariableReferenceExpression(resourceName);
	//			res.Add(converter.Key, resourceReference);
	//		}

	//		foreach (ResourceDataTemplate dataTemplate in resourceCollection.OfType<ResourceDataTemplate>())
	//		{
	//			string resourceName = string.Format(RESOURCE_NAME_FORMAT, resourceIndex++);

	//			CodeFieldReferenceExpression rIdReference = new CodeFieldReferenceExpression(new CodeTypeReferenceExpression("Resource.Layout"), dataTemplate.ResourceId);
	//			method.Statements.Add(new CodeVariableDeclarationStatement(typeof(int), resourceName, rIdReference));

	//			CodeVariableReferenceExpression resourceReference = new CodeVariableReferenceExpression(resourceName);
	//			res.Add(dataTemplate.Key, resourceReference);
	//		}

	//		CodePropertyReferenceExpression layoutInflaterReference;
	//		if (activityInformations.IsFragment)
	//		{
	//			layoutInflaterReference = new CodePropertyReferenceExpression(new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "Activity"), "LayoutInflater");
	//		}
	//		else
	//		{
	//			layoutInflaterReference = new CodePropertyReferenceExpression(new CodeThisReferenceExpression(), "LayoutInflater");
	//		}

	//		foreach (ResourceViewSelector viewSelector in resourceCollection.OfType<ResourceViewSelector>())
	//		{
	//			string resourceName = string.Format(RESOURCE_NAME_FORMAT, resourceIndex++);

	//			//Create view selector item
	//			CodeObjectCreateExpression objectCreateExpression = new CodeObjectCreateExpression(viewSelector.ClassName, layoutInflaterReference);
	//			method.Statements.Add(new CodeVariableDeclarationStatement(viewSelector.ClassName, resourceName, objectCreateExpression));

	//			//Get reference on new object
	//			CodeVariableReferenceExpression resourceReference = new CodeVariableReferenceExpression(resourceName);
	//			res.Add(viewSelector.Key, resourceReference);

	//			//Affect property to view selector
	//			foreach (KeyValuePair<string, string> property in viewSelector.Properties)
	//			{
	//				// First, extract value of the property
	//				// Could be {Resource ...} or a simple string

	//				CodeExpression propertyValueExpression;
	//				if (ClassGeneratorHelper.IsResourceReferenceExpression(property.Value))
	//				{
	//					string resourceKey = ClassGeneratorHelper.ExtractResourceKey(property.Value);

	//					if (res.ContainsKey(resourceKey))
	//					{
	//						propertyValueExpression = res[resourceKey];
	//					}
	//					else
	//					{
	//						BindingPreprocess.Logger.LogError("Error, Resource with key = {0} does not exists", resourceKey);
	//						continue;
	//					}
	//				}
	//				else
	//				{
	//					propertyValueExpression = new CodePrimitiveExpression(property.Value);
	//				}

	//				method.Statements.Add(new CodeAssignStatement(
	//					new CodePropertyReferenceExpression(resourceReference, property.Key),
	//					propertyValueExpression
	//					));
	//			}
				
	//		}

	//		return res;
	//	}

	//}

	// ReSharper restore BitwiseOperatorOnEnumWithoutFlags
}
