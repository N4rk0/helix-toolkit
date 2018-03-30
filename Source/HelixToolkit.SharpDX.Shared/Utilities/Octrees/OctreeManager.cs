﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="OctreeManager.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

using SharpDX;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;

#if NETFX_CORE
namespace HelixToolkit.UWP.Utilities
#else
namespace HelixToolkit.Wpf.SharpDX.Utilities
#endif
{
    using Model;
    using System.Collections.Concurrent;
    /// <summary>
    /// 
    /// </summary>
    public abstract class OctreeManagerBase : ObservableObject, IOctreeManager
    {
        /// <summary>
        /// Occurs when [on octree created].
        /// </summary>
        public event EventHandler<OctreeArgs> OnOctreeCreated;

        private IOctree octree;
        /// <summary>
        /// Gets or sets the octree.
        /// </summary>
        /// <value>
        /// The octree.
        /// </value>
        public IOctree Octree
        {
            protected set
            {
                if(Set(ref octree, value))
                {
                    OnOctreeCreated?.Invoke(this, new OctreeArgs(value));
                }
            }
            get
            { return octree; }
        }
        /// <summary>
        /// The m octree
        /// </summary>
        protected RenderableBoundingOctree mOctree = null;
        /// <summary>
        /// Gets or sets the parameter.
        /// </summary>
        /// <value>
        /// The parameter.
        /// </value>
        public OctreeBuildParameter Parameter { set; get; } = new OctreeBuildParameter();

        private bool mEnabled = true;
        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="OctreeManagerBase"/> is enabled.
        /// </summary>
        /// <value>
        ///   <c>true</c> if enabled; otherwise, <c>false</c>.
        /// </value>
        public bool Enabled
        {
            set
            {
                mEnabled = value;
                if (!mEnabled)
                {
                    Clear();
                }
            }
            get
            {
                return mEnabled;
            }
        }
        /// <summary>
        /// Gets or sets a value indicating whether [request update octree].
        /// </summary>
        /// <value>
        ///   <c>true</c> if [request update octree]; otherwise, <c>false</c>.
        /// </value>
        public bool RequestUpdateOctree { get { return mRequestUpdateOctree; } protected set { mRequestUpdateOctree = value; } }
        private volatile bool mRequestUpdateOctree = false;
        /// <summary>
        /// Adds the pending item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public abstract bool AddPendingItem(IRenderable item);
        /// <summary>
        /// Clears this instance.
        /// </summary>
        public abstract void Clear();
        /// <summary>
        /// Rebuilds the tree.
        /// </summary>
        /// <param name="items">The items.</param>
        public abstract void RebuildTree(IEnumerable<IRenderable> items);
        /// <summary>
        /// Removes the item.
        /// </summary>
        /// <param name="item">The item.</param>
        public abstract void RemoveItem(IRenderable item);
        /// <summary>
        /// Requests the rebuild.
        /// </summary>
        public abstract void RequestRebuild();
        /// <summary>
        /// Processes the pending items.
        /// </summary>
        public abstract void ProcessPendingItems();
    }

    /// <summary>
    /// Use to create geometryModel3D octree for groups. Each ItemsModel3D must has its own manager, do not share between two ItemsModel3D
    /// </summary>
    public sealed class GroupRenderableBoundOctreeManager : OctreeManagerBase
    {
        private object lockObj = new object();

        private void UpdateOctree(RenderableBoundingOctree tree)
        {
            Octree = tree;
            mOctree = tree;
        }
        /// <summary>
        /// Rebuilds the tree.
        /// </summary>
        /// <param name="items">The items.</param>
        public override void RebuildTree(IEnumerable<IRenderable> items)
        {
            lock (lockObj)
            {
                RequestUpdateOctree = false;
                if (Enabled)
                {
                    UpdateOctree(RebuildOctree(items));
                    if (Octree == null)
                    {
                        RequestRebuild();
                    }
                }
                else
                {
                    Clear();
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SubscribeBoundChangeEvent(IRenderable item)
        {
            item.OnTransformBoundChanged -= Item_OnBoundChanged;
            item.OnTransformBoundChanged += Item_OnBoundChanged;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void UnsubscribeBoundChangeEvent(IRenderable item)
        {
            item.OnTransformBoundChanged -= Item_OnBoundChanged;
        }

        private readonly HashSet<IRenderable> pendingItems
            = new HashSet<IRenderable>();

        private void Item_OnBoundChanged(object sender,  BoundChangeArgs<BoundingBox> args)
        {
            var item = sender as IRenderable;
            if (item == null)
            { return; }
            else
            {
                pendingItems.Add(item);
                return;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public override void ProcessPendingItems()
        {
            lock (lockObj)
            {
                foreach(var item in pendingItems)
                {
                    if (mOctree == null || !item.IsAttached)
                    {
                        UnsubscribeBoundChangeEvent(item);
                        continue;
                    }
                    int index;
                    var node = mOctree.FindItemByGuid(item.GUID, item, out index);
                    bool rootAdd = true;
                    if (node != null)
                    {
                        var tree = mOctree;
                        UpdateOctree(null);
                        var geoNode = node as RenderableBoundingOctree;
                        if (geoNode.Bound.Contains(item.BoundsWithTransform) == ContainmentType.Contains)
                        {
                            if (geoNode.PushExistingToChild(index))
                            {
                                tree = tree.Shrink() as RenderableBoundingOctree;
                            }
                            rootAdd = false;
                        }
                        else
                        {
                            geoNode.RemoveAt(index, tree);
                        }
                        UpdateOctree(tree);
                    }
                    else
                    {
                        mOctree.RemoveByGuid(item.GUID, item, mOctree);
                    }
                    if (rootAdd)
                    {
                        AddItem(item);
                    }
                }
                pendingItems.Clear();
            }
        }

        private RenderableBoundingOctree RebuildOctree(IEnumerable<IRenderable> items)
        {
            Clear();
            if (items == null)
            {
                return null;
            }
            var tree = new RenderableBoundingOctree(items.ToList(), Parameter);
            tree.BuildTree();
            if (tree.TreeBuilt)
            {
                foreach (var item in items)
                {
                    SubscribeBoundChangeEvent(item);
                }
            }
            return tree.TreeBuilt ? tree : null;
        }

        private static readonly BoundingBox ZeroBound = new BoundingBox();

        /// <summary>
        /// Adds the pending item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        public override bool AddPendingItem(IRenderable item)
        {
            lock (lockObj)
            {
                if (Enabled && item != null)
                {
                    item.OnTransformBoundChanged -= GeometryModel3DOctreeManager_OnBoundInitialized;
                    item.OnTransformBoundChanged += GeometryModel3DOctreeManager_OnBoundInitialized;
                    if (item.Bounds != ZeroBound)
                    {
                        AddItem(item);
                    }
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        private void GeometryModel3DOctreeManager_OnBoundInitialized(object sender, BoundChangeArgs<BoundingBox> args)
        {
            var item = sender as IRenderable;
            item.OnTransformBoundChanged -= GeometryModel3DOctreeManager_OnBoundInitialized;
            AddItem(item);
        }

        private void AddItem(IRenderable item)
        {
            if (Enabled && item != null)
            {
                var tree = mOctree;
                UpdateOctree(null);
                if (tree == null)
                {
                    RequestRebuild();
                }
                else
                {
                    bool succeed = true;
                    int counter = 0;
                    while (!tree.Add(item))
                    {
                        var direction = (item.Bounds.Minimum + item.Bounds.Maximum)
                            - (tree.Bound.Minimum + tree.Bound.Maximum);
                        tree = tree.Expand(ref direction) as RenderableBoundingOctree;
                        ++counter;
                        if (counter > 10)
                        {
#if DEBUG
                            throw new Exception("Expand tree failed");
#else
                            succeed = false;
                            break;
#endif
                        }
                    }
                    if (succeed)
                    {
                        UpdateOctree(tree);
                        SubscribeBoundChangeEvent(item);
                    }
                    else
                    {
                        RequestRebuild();
                    }
                }
            }
        }
        /// <summary>
        /// Removes the item.
        /// </summary>
        /// <param name="item">The item.</param>
        public override void RemoveItem(IRenderable item)
        {           
            if (Enabled && Octree != null && item != null)
            {
                lock (lockObj)
                {
                    var tree = mOctree;
                    UpdateOctree(null);
                    item.OnTransformBoundChanged -= GeometryModel3DOctreeManager_OnBoundInitialized;
                    UnsubscribeBoundChangeEvent(item);
                    if (!tree.RemoveByBound(item))
                    {
                        //Console.WriteLine("Remove failed.");
                    }
                    else
                    {
                        tree = tree.Shrink() as RenderableBoundingOctree;
                    }
                    UpdateOctree(tree);
                }
            }
        }
        /// <summary>
        /// Clears this instance.
        /// </summary>
        public override void Clear()
        {
            lock (lockObj)
            {
                RequestUpdateOctree = false;
                UpdateOctree(null);
            }
        }
        /// <summary>
        /// Requests the rebuild.
        /// </summary>
        public override void RequestRebuild()
        {
            lock (lockObj)
            {
                Clear();
                RequestUpdateOctree = true;
            }
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public sealed class InstancingRenderableOctreeManager : OctreeManagerBase
    {
        /// <summary>
        /// Adds the pending item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public override bool AddPendingItem(IRenderable item)
        {
            return false;
        }
        /// <summary>
        /// Clears this instance.
        /// </summary>
        public override void Clear()
        {
            Octree = null;
        }
        /// <summary>
        /// Processes the pending items.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public override void ProcessPendingItems()
        {
            
        }
        /// <summary>
        /// Rebuilds the tree.
        /// </summary>
        /// <param name="items">The items.</param>
        public override void RebuildTree(IEnumerable<IRenderable> items)
        {
            Clear();
            if (items == null)
            { return; }
            if(items.FirstOrDefault() is IInstancing inst)
            {
                var instMatrix = inst.InstanceBuffer.Elements;
                var octree = new InstancingModel3DOctree(instMatrix, (inst as IRenderable).Bounds, this.Parameter, new Stack<KeyValuePair<int, IOctree[]>>(10));
                octree.BuildTree();
                Octree = octree;
            }
        }
        /// <summary>
        /// Removes the item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <exception cref="NotImplementedException"></exception>
        public override void RemoveItem(IRenderable item)
        {
        }
        /// <summary>
        /// Requests the rebuild.
        /// </summary>
        /// <exception cref="NotImplementedException"></exception>
        public override void RequestRebuild()
        {
        }
    }
}
