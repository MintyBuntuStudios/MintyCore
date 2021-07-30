﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using Veldrid;
using Vulkan;

namespace MintyCore.Render
{
	struct GpuCameraData
	{
		public Matrix4x4 View;
		public Matrix4x4 Projection;
		public Matrix4x4 ViewProjection;
	}
	
}