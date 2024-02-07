using System;
using System.Collections.Generic;
using System.Numerics;
using System.Reflection;
using System.Xml.Serialization;
using MintyCore.FontStashSharp;
using MintyCore.Myra.Attributes;
using MintyCore.Myra.Graphics2D;
using MintyCore.Myra.Utility;

namespace MintyCore.Myra.MML
{
	internal class BaseContext
	{
		public const string IdName = "Id";

		public static ITypeSerializer FindSerializer(Type type)
		{
			if (type.IsNullablePrimitive())
			{
				type = type.GetNullableType();
			}

			if (Serialization._serializers.TryGetValue(type, out var result))
			{
				return result;
			}

			return null;
		}

		protected static void ParseProperties(Type type, bool checkSkipSave,
			out List<PropertyInfo> complexProperties, 
			out List<PropertyInfo> simpleProperties)
		{
			complexProperties = new List<PropertyInfo>();
			simpleProperties = new List<PropertyInfo>();

			var allProperties = type.GetRuntimeProperties();
			foreach (var property in allProperties)
			{
				if (property.GetMethod is null ||
					!property.GetMethod.IsPublic ||
					property.GetMethod.IsStatic)
				{
					continue;
				}

				Attribute attr = property.FindAttribute<XmlIgnoreAttribute>();
				if (attr is not null)
				{
					continue;
				}

				if (checkSkipSave)
				{
					attr = property.FindAttribute<SkipSaveAttribute>();
					if (attr is not null)
					{
						continue;
					}
				}

				var propertyType = property.PropertyType;
				if (propertyType.IsPrimitive || 
					propertyType.IsNullablePrimitive() ||
					propertyType.IsEnum || 
					propertyType.IsNullableEnum() ||
					propertyType == typeof(string) ||
					propertyType == typeof(Vector2) ||
					propertyType == typeof(FSColor) ||
					propertyType == typeof(FSColor?) ||
					typeof(IBrush).IsAssignableFrom(propertyType) ||
					propertyType == typeof(SpriteFontBase) ||
					propertyType == typeof(Thickness))
				{
					simpleProperties.Add(property);
				} else
				{
					complexProperties.Add(property);
				}
			}
		}
	}
}
