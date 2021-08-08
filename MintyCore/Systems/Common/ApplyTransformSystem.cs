﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MintyCore.Components;
using MintyCore.Components.Common;
using MintyCore.ECS;
using MintyCore.Identifications;
using MintyCore.SystemGroups;
using MintyCore.Utils;
using System.Numerics;

namespace MintyCore.Systems.Common
{
	[ExecuteInSystemGroup(typeof(FinalizationSystemGroup))]
	partial class ApplyTransformSystem : ASystem
	{
		[ComponentQuery]
		private TestComponentQuery<Transform, (Position, Rotation, Scale)> _componentQuery = new();

		public override Identification Identification => SystemIDs.ApplyTransform;

		public override void Dispose() { }

		public override void Execute()
		{
			foreach (var entity in _componentQuery)
			{
				Position position = entity.GetPosition();
				Rotation rotation = entity.GetRotation();
				Scale scale = entity.GetScale();
				ref Transform transform = ref entity.GetTransform();


				transform.Value = Matrix4x4.CreateFromQuaternion(rotation.Value) * Matrix4x4.CreateTranslation(position.Value) * Matrix4x4.CreateScale(scale.Value);
				transform.Dirty = 1;
			}
		}

		public override void Setup()
		{
			_componentQuery.Setup(this);
		}
	}
}
