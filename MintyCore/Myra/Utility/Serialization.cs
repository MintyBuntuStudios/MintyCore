using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Numerics;
using System.Reflection;
using MintyCore.FontStashSharp;
using MintyCore.FontStashSharp.RichText;
using MintyCore.Myra.Graphics2D;
using MintyCore.Myra.MML;

namespace MintyCore.Myra.Utility
{
	internal static class Serialization
	{
		public static readonly Dictionary<Type, ITypeSerializer> _serializers = new()
		{
			{typeof(Vector2), new Vector2Serializer()},
			{typeof(Thickness), new ThicknessSerializer()},
		};

		public static bool HasDefaultValue(this PropertyInfo property, object value)
		{
			if (property.PropertyType == typeof(Thickness) &&
				value.Equals(Thickness.Zero))
			{
				// Skip empty Thickness
				return true;
			}

			var defaultAttribute = property.FindAttribute<DefaultValueAttribute>();
			if (defaultAttribute is not null)
			{
				object defaultAttributeValue = defaultAttribute.Value;
				// If property is of Color type, than DefaultValueAttribute should contain its name or hex
				if (property.PropertyType == typeof(FSColor))
				{
					defaultAttributeValue = ColorStorage.FromName(defaultAttributeValue.ToString()).Value;
				}

				if (property.PropertyType == typeof(string) && 
					string.IsNullOrEmpty((string)defaultAttributeValue) && 
					string.IsNullOrEmpty((string)value))
				{
					// Skip empty/null string
					return true;
				}

				if (Equals(value, defaultAttributeValue))
				{
					// Skip default
					return true;
				}

				if (defaultAttributeValue is not null &&
					defaultAttributeValue.GetType() == typeof(string) && 
					_serializers.TryGetValue(property.PropertyType, out ITypeSerializer typeSerializer) &&
					(string)defaultAttributeValue == typeSerializer.Serialize(value))
				{
					return true;
				}
			}

			return false;
		}

		public static Rectangle ParseRectangle(this string s)
		{
			var parts = s.Split(',');
			if (parts.Length != 4)
			{
				throw new Exception("Rectangle should consist of 4 numbers");
			}

			Rectangle result = new()
			{
				X = int.Parse(parts[0].Trim()),
				Y = int.Parse(parts[1].Trim()),
				Width = int.Parse(parts[2].Trim()),
				Height = int.Parse(parts[3].Trim())
			};

			return result;
		}
	}
}