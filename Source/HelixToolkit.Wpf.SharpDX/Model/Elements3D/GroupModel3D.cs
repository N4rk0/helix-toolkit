﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GroupModel3D.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace HelixToolkit.Wpf.SharpDX
{
    using System.Collections.Generic;
    using System.Linq;

    using global::SharpDX;
    using Render;

    public class GroupModel3D : GroupElement3D, IHitable, IVisible
    {
        protected override void OnRender(IRenderContext renderContext, DeviceContextProxy deviceContext)
        {
            foreach (var c in this.Items)
            {
                c.Render(renderContext, deviceContext);
            }
        }      

        protected override bool OnHitTest(IRenderContext context, Matrix totalModelMatrix, ref Ray ray, ref List<HitTestResult> hits)
        {
            bool hit = false;
            foreach (var c in this.Items)
            {
                if (c is IHitable h)
                {
                    if (h.HitTest(context, ray, ref hits))
                    {
                        hit = true;
                    }
                }
            }
            if (hit)
            {
                var pos = ray.Position;
                hits = hits.OrderBy(x => Vector3.DistanceSquared(pos, x.PointHit)).ToList();
            }            
            return hit;
        }
    }
}