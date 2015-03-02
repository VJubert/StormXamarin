﻿using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Storm.Binding.AndroidTarget.Helper;

namespace Storm.Binding.AndroidTarget.CodeGenerator
{
	class ActivityGenerator : AbstractBindingHandlerClassGenerator
	{
		protected override CodeMethodReferenceExpression GetFindViewByIdReference(string typeName)
		{
			return new CodeMethodReferenceExpression(
						new CodeThisReferenceExpression(),
						"FindViewById",
						CodeGeneratorHelper.GetTypeReferenceFromName(typeName));
		}

		protected override CodePropertyReferenceExpression GetLayoutInflaterReference()
		{
			return CodeGeneratorHelper.GetPropertyReference("LayoutInflater");
		}
	}
}
