
using System.Numerics;

namespace MintyCore.Myra.Graphics2D.UI
{
	internal interface ITransformable
	{
		Vector2 ToLocal(Vector2 source);
		Vector2 ToGlobal(Vector2 pos);
	}
}
