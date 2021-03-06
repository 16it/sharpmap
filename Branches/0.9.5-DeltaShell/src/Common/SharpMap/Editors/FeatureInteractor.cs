﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using DelftTools.Utils.Editing;
using GeoAPI.Extensions.Networks;
using GeoAPI.Geometries;
using GeoAPI.Extensions.Feature;
using GisSharpBlog.NetTopologySuite.Geometries;
using NetTopologySuite.Extensions.Geometries;
using SharpMap.Api;
using SharpMap.Api.Delegates;
using SharpMap.Api.Editors;
using SharpMap.Editors.FallOff;
using SharpMap.Editors.Snapping;
using SharpMap.Layers;
using SharpMap.Rendering;
using SharpMap.Styles;
using System.Windows.Forms;

namespace SharpMap.Editors
{
    public abstract class FeatureInteractor : IFeatureInteractor
    {
        private readonly List<TrackerFeature> trackers = new List<TrackerFeature>();

        public FeatureInteractor(ILayer layer, IFeature feature, VectorStyle vectorStyle, IEditableObject editableObject)
        {
            Layer = layer;
            SourceFeature = feature;
            VectorStyle = vectorStyle;
            FeatureRelationEditors = new List<IFeatureRelationInteractor>();
            EditableObject = editableObject;
            CreateTrackers();
        }

        /// <summary>
        /// original feature
        /// </summary>
        public IFeature SourceFeature { get; protected set; }

        /// <summary>
        /// a clone of the original feature used during the editing process
        /// </summary>
        public IFeature TargetFeature { get; protected set; }

        /// <summary>
        /// tolerance in world coordinates used by the interactor when no CoordinateConverter is available
        /// </summary>
        public double Tolerance { get; set; }

        public ILayer Layer { get; protected set; }

        public VectorStyle VectorStyle { get; protected set; }

        public virtual IFallOffPolicy FallOffPolicy { get; set; }
        
        public virtual IList<ISnapRule> SnapRules { get; set; }

        protected IList<IFeatureRelationInteractor> FeatureRelationEditors { get; set; }

        public virtual IEditableObject EditableObject  { get; set; }

        protected abstract void CreateTrackers();
        
        public virtual IList<TrackerFeature> Trackers
        {
            get { return trackers; }
        }

        protected IEnumerable<int> TrackerIndices
        {
            get { return Trackers.Select(t => t.Index); }
        }

        protected IEnumerable<int> SelectedTrackerIndices
        {
            get { return Trackers.Where(t => t.Selected).Select(t => t.Index); }
        }

        public virtual bool MoveTracker(TrackerFeature trackerFeature, double deltaX, double deltaY,
                                        SnapResult snapResult = null)
        {
            if (trackerFeature.Index == -1)
            {
                throw new ArgumentException("Can not find tracker; can not move.");
            }

            var handles = SelectedTrackerIndices.ToList();

            if (handles.Count == 0)
            {
                return false;
                    // Do not throw exception, can occur in special cases when moving with CTRL toggle selection
            }

            if (FallOffPolicy != null)
            {
                FallOffPolicy.Move(TargetFeature.Geometry, trackers.Select(t => t.Geometry).ToList(), handles,
                                   trackerFeature.Index, deltaX, deltaY);
            }
            else
            {
                GeometryHelper.MoveCoordinate(TargetFeature.Geometry, trackerFeature.Index, deltaX, deltaY);
                TargetFeature.Geometry = TargetFeature.Geometry; // fire event

                GeometryHelper.MoveCoordinate(trackerFeature.Geometry, 0, deltaX, deltaY);
                trackerFeature.Geometry = trackerFeature.Geometry; // fire event
            }

            foreach (var topologyRule in FeatureRelationEditors)
            {
                topologyRule.UpdateRelatedFeatures(SourceFeature, TargetFeature.Geometry, handles);
            }

            return true;
        }

        public virtual bool RemoveTracker(TrackerFeature trackerFeature)
        {
            if (trackerFeature.Index == -1)
            {
                return false;
            }

            var newGeometry = GeometryHelper.RemoveCurvePoint(TargetFeature.Geometry, trackerFeature.Index,
                                                              TargetFeature is IBranch);

            if (newGeometry == null)
            {
                return false;
            }

            TargetFeature.Geometry = newGeometry;

            foreach (var topologyRule in FeatureRelationEditors)
            {
                topologyRule.UpdateRelatedFeatures(SourceFeature, TargetFeature.Geometry, SelectedTrackerIndices.ToList());
            }

            return true;
        }

        public virtual bool InsertTracker(ICoordinate coordinate, int index)
        {
            Trackers.Insert(index, new TrackerFeature(this, new Point(coordinate), index, null));

            TargetFeature.Geometry = GeometryHelper.InsertCurvePoint(TargetFeature.Geometry, coordinate, index);
            
            foreach (var topologyRule in FeatureRelationEditors)
            {
                topologyRule.UpdateRelatedFeatures(SourceFeature, TargetFeature.Geometry, SelectedTrackerIndices.ToList());
            }

            return true;
        }

        public virtual void SetTrackerSelection(TrackerFeature trackerFeature, bool select)
        {
        }

        public virtual Cursor GetCursor(TrackerFeature trackerFeature)
        {
            return new Cursor(Assembly.GetExecutingAssembly().GetManifestResourceStream("SharpMap.Editors.Cursors.Move.cur"));
        }

        public virtual TrackerFeature GetTrackerAtCoordinate(ICoordinate worldPos)
        {
            foreach (var trackerFeature in trackers)
            {
                ICoordinate size;

                if (trackerFeature.Bitmap != null)
                {
                    size = MapHelper.ImageToWorld(Layer.Map, trackerFeature.Bitmap.Width, trackerFeature.Bitmap.Height);   
                }
                else
                {
                    // hack for RegularGridCoverageLayer
                    size = MapHelper.ImageToWorld(Layer.Map, 6, 6);
                }

                var boundingBox = MapHelper.GetEnvelope(worldPos, size.X, size.Y);

                if (trackerFeature.Geometry.EnvelopeInternal.Intersects(boundingBox))
                    return trackerFeature;
            }
            return null;
        }

        protected IFeature CreateTargetFeature()
        {
            return (IFeature) SourceFeature.Clone();
        }

        public virtual void Start()
        {
            TargetFeature = CreateTargetFeature();
            foreach (var featureRelationInteractor in GetFeatureRelationInteractors(SourceFeature))
            {
                var activeRule = featureRelationInteractor.Activate(SourceFeature, TargetFeature, AddRelatedFeature, 0,
                                                                    FallOffPolicy);
                if (activeRule != null)
                {
                    FeatureRelationEditors.Add(activeRule);
                }
            }
        }

        private void AddRelatedFeature(IList<IFeatureRelationInteractor> childTopologyRules, IFeature sourceFeature,
                                       IFeature cloneFeature, int level)
        {
            OnWorkerFeatureCreated(sourceFeature, cloneFeature);

            foreach (var topologyRule in GetFeatureRelationInteractors(sourceFeature))
            {
                var activeRule = topologyRule.Activate(sourceFeature, cloneFeature, AddRelatedFeature, ++level,
                                                       FallOffPolicy);
                if (activeRule != null)
                    childTopologyRules.Add(activeRule);
            }
        }

        public virtual void Delete()
        {
            Layer.DataSource.Features.Remove(SourceFeature);
        }

        public virtual void Stop()
        {
            if (null == TargetFeature) 
                return;
            
            foreach (var topologyRule in FeatureRelationEditors)
            {
                topologyRule.StoreRelatedFeatures(SourceFeature, TargetFeature.Geometry, new List<int> { 0 });
            }

            SourceFeature.Geometry = (IGeometry) TargetFeature.Geometry.Clone();
          
            FeatureRelationEditors.Clear();
        }

        public virtual void Stop(SnapResult snapResult)
        {
            Stop();
        }

        public virtual void UpdateTracker(IGeometry geometry)
        {
        }

        /// <summary>
        /// Default implementation for moving feature is set to false. IFeatureProvider is not required to
        /// return the same objects for each request. For example the IFeatureProvider for shape files 
        /// constructs them on the fly in each GetGeometriesInView call. To support deletion and moving of
        /// shapes local caching and writing of shape files has to be implemented.
        /// </summary>
        /// <returns></returns>
        protected virtual bool AllowMoveCore()
        {
            return false;
        }

        /// <summary>
        /// Default set to false. See AllowMove.
        /// </summary>
        /// <returns></returns>
        protected virtual bool AllowDeletionCore()
        {
            return false;
        }

        public bool AllowMove()
        {
            return !IsLayerReadOnly() && AllowMoveCore();
        }

        public bool AllowDeletion()
        {
            return !IsLayerReadOnly() && AllowDeletionCore();
        }

        private bool IsLayerReadOnly()
        {
            var layer = Layer;

            while (layer != null)
            {
                if (layer.ReadOnly)
                {
                    return true;
                }

                layer = Layer.Map != null ? Layer.Map.GetGroupLayerContainingLayer(layer) : null;
            } 

            return false;
        }

        /// <summary>
        /// Default set to false. See AllowMove.
        /// Typically set to true for IPoint based geometries where there is only 1 tracker.
        /// </summary>
        /// <returns></returns>
        public virtual bool AllowSingleClickAndMove()
        {
            return false;
        }

        public event WorkerFeatureCreated WorkerFeatureCreated;

        protected virtual void OnWorkerFeatureCreated(IFeature sourceFeature, IFeature workFeature)
        {
            if (null != WorkerFeatureCreated)
            {
                WorkerFeatureCreated(sourceFeature, workFeature);
            }
        }

        public virtual IEnumerable<IFeatureRelationInteractor> GetFeatureRelationInteractors(IFeature feature)
        {
            yield break;
        }

        public virtual void Add(IFeature feature)
        {
            Start();
            Stop();
        }
    }
}