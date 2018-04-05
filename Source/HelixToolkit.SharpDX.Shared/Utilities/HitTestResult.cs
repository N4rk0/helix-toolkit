﻿/*
The MIT License (MIT)
Copyright (c) 2018 Helix Toolkit contributors
*/
using SharpDX;
#if NETFX_CORE
namespace HelixToolkit.UWP
#else
namespace HelixToolkit.Wpf.SharpDX
#endif
{
    /// <summary>
    /// Provides a hit test result.
    /// </summary>
    public class HitTestResult
    {
        /// <summary>
        /// Gets the distance between the hit intersection and the inner coordinate space
        /// of the System.Windows.Media.Media3D.Visual3D which initiated the hit test.
        /// 
        /// Double that indicates the distance between the hit intersection and the inner
        /// coordinate space of the System.Windows.Media.Media3D.Visual3D which initiated
        /// the hit test.
        /// </summary>
        public double Distance { get; set; }

        /// <summary>
        /// Gets the Model3D intersected by the ray along which the hit test was performed.
        /// Model3D intersected by the ray.
        /// </summary>        
        public object ModelHit { get; set; }

        /// <summary>
        /// Gets the Point at the intersection between the ray along which the hit
        /// test was performed and the hit object.
        /// Point at which the hit object was intersected by the ray.
        /// </summary>
        public Vector3 PointHit { get; set; }

        /// <summary>
        /// The normal vector of the triangle hit.
        /// </summary>
        public Vector3 NormalAtHit { get; set; }

        /// <summary>
        /// Indicates if this Result has data from a valid hit.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// This is a tag to add additional data.
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// The hitted triangle vertex indices.
        /// </summary>
        public System.Tuple<int, int, int> TriangleIndices { set; get; }
    }

    /// <summary>
    /// A specialized line hit test result.
    /// </summary>
    public class LineHitTestResult : HitTestResult
    {
        /// <summary>
        /// Gets or sets the index of the line segment that was hit.
        /// </summary>
        public int LineIndex { get; set; }

        /// <summary>
        /// Gets or sets the shortest distance between the hit test ray and the line that was hit.
        /// </summary>
        public double RayToLineDistance { get; set; }

        /// <summary>
        /// Gets or sets the scalar of the closest point on the hit test ray.
        /// </summary>
        public double RayHitPointScalar { get; set; }

        /// <summary>
        /// Gets or sets the scalar of the closest point on the line that was hit.
        /// </summary>
        public double LineHitPointScalar { get; set; }
    }

    public class BillboardHitResult : HitTestResult
    {
        public int TextInfoIndex { set; get; } = 0;
        public TextInfo TextInfo { set; get; } = null;
    }

    /// <summary>
    /// 
    /// </summary>
    public class HitTest2DResult
    {
        /// <summary>
        /// Gets or sets the model hit.
        /// </summary>
        /// <value>
        /// The model hit.
        /// </value>
        public object ModelHit { private set; get; }
        /// <summary>
        /// Initializes a new instance of the <see cref="HitTest2DResult"/> class.
        /// </summary>
        /// <param name="model">The model.</param>
        public HitTest2DResult(object model)
        {
            ModelHit = model;
        }
    }
}