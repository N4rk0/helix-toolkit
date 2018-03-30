﻿// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GroupElement3D.cs" company="Helix Toolkit">
//   Copyright (c) 2014 Helix Toolkit contributors
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace HelixToolkit.Wpf.SharpDX
{
    using global::SharpDX;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Linq;
    using System.Windows;
    using System.Windows.Markup;
    using Render;
    using HelixToolkit.Wpf.SharpDX.Core;
    using System.Collections.ObjectModel;

    /// <summary>
    /// Supports both ItemsSource binding and Xaml children. Binds with ObservableElement3DCollection 
    /// </summary>
    [ContentProperty("Children")]
    public abstract class GroupElement3D : Element3D
    {
        private IList<Element3D> itemsSourceInternal;
        /// <summary>
        /// ItemsSource for binding to collection. Please use ObservableElement3DCollection for observable, otherwise may cause memory leak.
        /// </summary>
        public IList<Element3D> ItemsSource
        {
            get { return (IList<Element3D>)this.GetValue(ItemsSourceProperty); }
            set { this.SetValue(ItemsSourceProperty, value); }
        }
        /// <summary>
        /// ItemsSource for binding to collection. Please use ObservableElement3DCollection for observable, otherwise may cause memory leak.
        /// </summary>
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register("ItemsSource", typeof(IList<Element3D>), typeof(GroupElement3D),
                new PropertyMetadata(null, 
                    (d, e) => {
                        (d as GroupElement3D).OnItemsSourceChanged(e.NewValue as IList<Element3D>);
                    }));

        public override IList<IRenderable> Items
        {
            get
            {
                return Children;
            }
        }

        public ObservableCollection<IRenderable> Children
        {
            get;
        } = new ObservableCollection<IRenderable>();


        public GroupElement3D()
        {
            Children.CollectionChanged += Items_CollectionChanged;
        }

        private void Items_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                DetachChildren(e.OldItems);
            }
            if (IsAttached)
            {               
                if(e.Action == NotifyCollectionChangedAction.Reset)
                {
                    AttachChildren(sender as IEnumerable);
                }
                else if(e.NewItems != null)
                {
                    AttachChildren(e.NewItems);
                }
            }
            forceUpdateTransform = true;
        }

        protected void AttachChildren(IEnumerable children)
        {
            foreach (Element3DCore c in children)
            {
                if (c.Parent == null)
                {
                    this.AddLogicalChild(c);
                }

                c.Attach(RenderHost);
            }
        }

        protected void DetachChildren(IEnumerable children)
        {
            foreach (Element3DCore c in children)
            {
                c.Detach();
                if (c.Parent == this)
                {
                    this.RemoveLogicalChild(c);
                }
            }
        }

        private void OnItemsSourceChanged(IList<Element3D> itemsSource)
        {
            if (itemsSourceInternal != null)
            {
                if (itemsSourceInternal is INotifyCollectionChanged s)
                {
                    s.CollectionChanged -= S_CollectionChanged;
                }
                foreach(var child in itemsSourceInternal)
                {
                    Children.Remove(child);
                }
            }
            itemsSourceInternal = itemsSource;
            if (itemsSourceInternal != null)
            {
                if (itemsSourceInternal is INotifyCollectionChanged s)
                {
                    s.CollectionChanged += S_CollectionChanged;
                }
                foreach(var child in itemsSourceInternal)
                {
                    Children.Add(child);
                }    
            }
        }

        private void S_CollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.OldItems != null)
            {
                foreach(Element3DCore item in e.OldItems)
                {
                    Children.Remove(item);
                }
            }
            if (e.NewItems != null)
            {
                foreach(Element3DCore item in e.NewItems)
                {
                    Children.Add(item);
                }
            }
        }

        protected override bool OnAttach(IRenderHost host)
        {
            AttachChildren(Items);
            return true;
        }

        protected override void OnDetach()
        {
            DetachChildren(Items);
            base.OnDetach();
        }        

        protected override void OnRender(IRenderContext context, DeviceContextProxy deviceContext)
        {
            foreach (var c in this.Items)
            {
                c.Render(context, deviceContext);
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