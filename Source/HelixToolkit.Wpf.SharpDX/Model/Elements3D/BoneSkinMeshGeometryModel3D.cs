﻿using HelixToolkit.Wpf.SharpDX.Core;
using SharpDX;
using System.Collections.Generic;
using System.Windows;

namespace HelixToolkit.Wpf.SharpDX
{
    public class BoneSkinMeshGeometryModel3D : MeshGeometryModel3D
    {
        public static DependencyProperty VertexBoneIdsProperty = DependencyProperty.Register("VertexBoneIds", typeof(IList<BoneIds>), typeof(BoneSkinMeshGeometryModel3D), 
            new PropertyMetadata(null, (d,e)=>
            {
                (d as BoneSkinMeshGeometryModel3D).bonesBufferModel.Elements = e.NewValue as IList<BoneIds>;
            }));

        public IList<BoneIds> VertexBoneIds
        {
            set
            {
                SetValue(VertexBoneIdsProperty, value);
            }
            get
            {
                return (IList<BoneIds>)GetValue(VertexBoneIdsProperty);
            }
        }

        public static DependencyProperty BoneMatricesProperty = DependencyProperty.Register("BoneMatrices", typeof(BoneMatricesStruct), typeof(BoneSkinMeshGeometryModel3D),
            new PropertyMetadata(new BoneMatricesStruct() { Bones = new Matrix[BoneMatricesStruct.NumberOfBones] }, 
                (d, e) =>
                {
                    (d as BoneSkinMeshGeometryModel3D).boneSkinRenderCore.BoneMatrices = (BoneMatricesStruct)e.NewValue;
                }));

        public BoneMatricesStruct BoneMatrices
        {
            set
            {
                SetValue(BoneMatricesProperty, value);
            }
            get
            {
                return (BoneMatricesStruct)GetValue(BoneMatricesProperty);
            }
        }

        protected readonly IElementsBufferModel<BoneIds> bonesBufferModel = new VertexBoneIdBufferModel<BoneIds>(BoneIds.SizeInBytes);
        private IBoneSkinRenderParams boneSkinRenderCore
        {
            get { return (IBoneSkinRenderParams)RenderCore; }
        }

        protected override IRenderTechnique OnCreateRenderTechnique(IRenderHost host)
        {
            return host.EffectsManager[DefaultRenderTechniqueNames.BoneSkinBlinn];
        }

        protected override RenderCore OnCreateRenderCore()
        {         
            return new BoneSkinRenderCore();
        }

        protected override void AssignDefaultValuesToCore(RenderCore core)
        {
            base.AssignDefaultValuesToCore(core);
            boneSkinRenderCore.BoneMatrices = BoneMatrices;
        }

        protected override bool OnAttach(IRenderHost host)
        {
            if (base.OnAttach(host))
            {
                bonesBufferModel.Initialize();
                boneSkinRenderCore.VertexBoneIdBuffer = bonesBufferModel;
                return true;
            }
            else
            {
                return false;
            }
        }

        protected override void OnDetach()
        {
            bonesBufferModel.DisposeAndClear();
            base.OnDetach();
        }

        protected override bool CheckBoundingFrustum(BoundingFrustum boundingFrustum)
        {
            return true;
        }

        protected override bool CanHitTest(IRenderContext context)
        {
            return false;//return base.CanHitTest(context) && !hasBoneParameter;
        }
    }
}
