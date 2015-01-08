﻿using System;

namespace Storm.Mvvm.Wrapper
{
	public static class ConverterHelper
	{
		private static readonly Type _javaObjectType = typeof (Java.Lang.Object);
		public static object ChangeType(object value, Type conversionType)
		{
			if (value == null)
			{
				return null;
			}
			Type valueType = value.GetType();
			bool valueIsJava = valueType.IsSubclassOf(_javaObjectType) || valueType == _javaObjectType;
			bool expectedIsJava = conversionType.IsSubclassOf(_javaObjectType) || conversionType == _javaObjectType;

			if (expectedIsJava)
			{
				if (valueIsJava)
				{
					return value;
				}
				return value.WrapIntoJava();
			}
			if (valueIsJava)
			{
				value = (value as Java.Lang.Object).ToManaged();
			}
			return Convert.ChangeType(value, conversionType);
		}
	}
}