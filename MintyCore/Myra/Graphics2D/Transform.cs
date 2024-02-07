using System;
using System.Drawing;
using System.Numerics;
using MintyCore.Myra.Utility;
using Matrix = System.Numerics.Matrix3x2;

namespace MintyCore.Myra.Graphics2D
{
	public struct Transform
	{
		public Vector2 Scale { get; private set; }
		public float Rotation { get; private set; }

		public Matrix Matrix;

		public Transform(Vector2 offset, Vector2 origin, Vector2 scale, float rotation)
		{
			BuildTransform(offset, origin, scale, rotation, out var newMatrix);
			Matrix = newMatrix;

			Scale = scale;
			Rotation = rotation;
		}

		private static void BuildTransform(Vector2 position, Vector2 origin, Vector2 scale, float rotation, out Matrix result)
		{
			// This code had been borrowed from MonoGame's SpriteBatch.DrawString
			result = Matrix.Identity;

			float offsetX, offsetY;
			if (rotation == 0)
			{
				result.M11 = scale.X;
				result.M22 = scale.Y;
				offsetX = position.X - (origin.X * result.M11);
				offsetY = position.Y - (origin.Y * result.M22);
			}
			else
			{
				var cos = (float)Math.Cos(rotation);
				var sin = (float)Math.Sin(rotation);
				result.M11 = scale.X * cos;
				result.M12 = scale.X * sin;
				result.M21 = scale.Y * -sin;
				result.M22 = scale.Y * cos;
				offsetX = position.X - (origin.X * result.M11) - (origin.Y * result.M21);
				offsetY = position.Y - (origin.X * result.M12) - (origin.Y * result.M22);
			}

			offsetX += origin.X;
			offsetY += origin.Y;


			result.M31 = offsetX;
			result.M32 = offsetY;
		}

		public void AddTransform(ref Transform newTransform)
		{
			Matrix = newTransform.Matrix * Matrix;

			Scale *= newTransform.Scale;
			Rotation += newTransform.Rotation;
		}

		public Vector2 Apply(Vector2 source) => source.Transform(ref Matrix);

		public Point Apply(Point source) => Apply(new Vector2(source.X, source.Y)).ToPoint();

		public Rectangle Apply(Rectangle source) => source.Transform(ref Matrix);
	}
}