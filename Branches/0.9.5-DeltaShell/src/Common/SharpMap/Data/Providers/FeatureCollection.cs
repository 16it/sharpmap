﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using DelftTools.Utils;
using DelftTools.Utils.Collections;
using DelftTools.Utils.Data;
using DelftTools.Utils.Reflection;
using GeoAPI.CoordinateSystems;
using GeoAPI.Extensions.Feature;
using GeoAPI.Geometries;
using GisSharpBlog.NetTopologySuite.Geometries;
using SharpMap.Api;

namespace SharpMap.Data.Providers
{
    //TODO: get this class generic FeatureCollection<F>:where F: is IFeature. This will speed up and prettify the code :)
    //Note: See the WFDExplorer plugin for an implementation of FeatureCollection<T>!! It is not fully covered with tests 
    //yet because not all requirements are clear
    
    public class FeatureCollection : Unique<long>, IFeatureProvider, ITimeNavigatable /* TODO: remove ITimeNavigatable, should be used only on UI facade classes */
    {
        private Type featureType;
        private IList features;
        private IEnumerable<DateTime> times;
        public FeatureCollection() 
        {
            features = new List<IFeature>();
            FilterFeaturesByTime();
        }

        public FeatureCollection(IList features, Type featureType)
        {
            if (!featureType.IsClass)
            {
                // We only accept a class because we want to use Activator to create object
                throw new ArgumentException("Can only instantiate FeatureCollection with class");
            }
            if (!typeof(IFeature).IsAssignableFrom((featureType)))
            {
                throw new ArgumentException("Feature type should be IFeature");
            }
            Features = features;
            FeatureType = featureType;
        }

        public virtual int GetFeatureCount()
        {
            return Features.Count;
        }

        public virtual IFeature GetFeature(int index)
        {
            return (IFeature) Features[index];
        }

        public virtual bool Contains(IFeature feature)
        {
            if (Features.Count == 0)
            {
                return false;
            }
            // Since Features can be strongly collection typed we must prevent searching objects of an invalid type
            if (FeatureType != null)
            {
                // test if feature we are looking for is derived from FeatureType
                if (!FeatureType.IsAssignableFrom(feature.GetType()))
                {
                    return false;
                }
            }
            else
            {
                // if FeatureType is not set use type of first object in collection.
                if (Features[0].GetType() != feature.GetType())
                {
                    return false;
                }
            }
            return Features.Contains(feature);
        }

        public virtual int IndexOf(IFeature feature)
        {
            if (Features.Count == 0 || Features[0].GetType() != feature.GetType())
            {
                return -1;
            }
            return Features.IndexOf(feature);
        }

        public virtual string SrsWkt { get; set; }

        public virtual IEnvelope GetBounds(int recordIndex)
        {
            return GetFeature(recordIndex).Geometry.EnvelopeInternal;
        }

        public virtual ICoordinateSystem CoordinateSystem { get; set; }

        protected IEnvelope envelope; // TODO: make it private, currently used to improve performance and still remain robust (clear envelope on change)

        public virtual IEnvelope GetExtents()
        {
            // TODO: cache envelope, but make sure it is updated after changes occur
            if (Features == null || Features.Count == 0) return null;

            if (this.envelope != null)
            {
                return this.envelope;
            }

            IEnvelope envelope = new Envelope();

            foreach (IFeature feature in Features)
            {
                if(feature.Geometry == null)
                {
                    continue;
                }

                // HACK: probably we should not use EnvelopeInternal here but Envelope

                if (envelope.IsNull)
                {
                    envelope = (IEnvelope)feature.Geometry.EnvelopeInternal.Clone();
                }

                envelope.ExpandToInclude(feature.Geometry.EnvelopeInternal);
            }

            this.envelope = envelope;

            return envelope;
        }

        public virtual event EventHandler FeaturesChanged;
        
        protected void FireFeaturesChanged()
        {
            this.envelope = null;

            if (FeaturesChanged != null)
            {
                FeaturesChanged(this, EventArgs.Empty);
            }
        }

        public virtual IGeometry GetGeometryByID(int oid)
        {
            return ((IFeature) Features[oid]).Geometry;
        }

        public virtual IList Features
        {
            get
            {
                if (times != null)
                {
                    return timeFilteredFeatures;
                }

                return features;
            }
            set
            {
                if (value == null)
                {
                    throw new InvalidOperationException("Features cannot be null in a feature collection.");
                }
                
                var featuresCollectionChanged = features as INotifyCollectionChanged;
                if (featuresCollectionChanged != null)
                {
                    featuresCollectionChanged.CollectionChanged -= FeaturesCollectionChanged;
                }
                var featuresPropertyChanged = features as INotifyPropertyChange;
                if (featuresPropertyChanged != null)
                {
                    featuresPropertyChanged.PropertyChanged -= FeaturesPropertyChanged;
                }

                features = value;
                GuessFeatureType();

                if (features.Count > 0 && features[0] is ITimeDependent)
                {
                    times = features.Cast<ITimeDependent>().Select(f => f.Time).Distinct().OrderBy(t => t);
                }
                FilterFeaturesByTime();

                featuresCollectionChanged = features as INotifyCollectionChanged;
                if (featuresCollectionChanged != null)
                {
                    featuresCollectionChanged.CollectionChanged += FeaturesCollectionChanged;
                }

                featuresPropertyChanged = features as INotifyPropertyChange;
                if (featuresPropertyChanged != null)
                {
                    featuresPropertyChanged.PropertyChanged += FeaturesPropertyChanged;
                }
            }
        }

        public virtual bool IsReadOnly { get { return false; } }

        private void FeaturesPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            envelope = null;
        }

        void FeaturesCollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            FireFeaturesChanged();
        }

        private void GuessFeatureType()
        {
            if(featureType != null)
            {
                return;
            }

            // try to obtain feature type from given collection of features
            Type featuresCollectionType = Features.GetType();
            if (featuresCollectionType.IsGenericType && !featuresCollectionType.IsInterface)
            {
                featureType = featuresCollectionType.GetGenericArguments()[0];
            }

            // guess feature type from the first feature
            if (featureType == null && Features.Count > 0)
            {
                featureType = Features[0].GetType();
            }
        }

        public virtual void Dispose()
        {
            var featuresCollectionChanged = features as INotifyCollectionChanged;
            if (featuresCollectionChanged != null)
            {
                featuresCollectionChanged.CollectionChanged -= FeaturesCollectionChanged;
            }
        }

        public virtual Type FeatureType
        {
            get { return featureType; }
            set
            {
                featureType = value;
                if (!value.Implements(typeof(IFeature)))
                {
                    throw new ArgumentException(string.Format("Type '{0}' is not a IFeature.", value));
                }
            }
        }

        public virtual Func<IFeatureProvider,IGeometry,IFeature> AddNewFeatureFromGeometryDelegate { get; set; }

        public virtual IFeature Add(IGeometry geometry)
        {
            if (featureType == null)
            {
                GuessFeatureType();
                if (featureType == null)
                {
                    throw new NotSupportedException("FeatureType must be set in order to add a new feature geometry");
                }
            }

            IFeature newFeature;
            if (AddNewFeatureFromGeometryDelegate != null)
            {
                newFeature = AddNewFeatureFromGeometryDelegate(this, geometry);
            }
            else
            {
                newFeature = (IFeature) Activator.CreateInstance(featureType);
                newFeature.Geometry = geometry;
                Features.Add(newFeature);
            }

            return newFeature;
        }

        public virtual bool Add(IFeature feature)
        {
            if (featureType == null)
            {
                GuessFeatureType();
                if (featureType == null || featureType != feature.GetType())
                {
                    throw new NotSupportedException("FeatureType must be set in order to add a new feature");
                }
            }

            Features.Add(feature);

            return true;
        }

        public virtual DateTime? TimeSelectionStart
        {
            get { return timeSelectionStart; }
        }

        public virtual DateTime? TimeSelectionEnd
        {
            get { return timeSelectionEnd; }
        }

        public virtual TimeNavigatableLabelFormatProvider CustomDateTimeFormatProvider
        {
            get { return null; }
        }

        private void FilterFeaturesByTime()
        {
            if(TimeSelectionStart == null || features.Count == 0 || !(features[0] is ITimeDependent))
            {
                timeFilteredFeatures = features;
                return;
            }

            timeFilteredFeatures = features.Cast<IFeature>()
                .Where(f =>
                           {
                               var timeDependent = f as ITimeDependent;

                               return timeDependent.Time >= TimeSelectionStart 
                                   && (TimeSelectionEnd == null || timeDependent.Time <= TimeSelectionEnd);
                           })
                .Select(f => f)
                .ToList();
        }

        public virtual void SetCurrentTimeSelection(DateTime? start, DateTime? end)
        {
            timeSelectionStart = start;
            timeSelectionEnd = end;
            FilterFeaturesByTime();

            if (CurrentTimeSelectionChanged != null)
            {
                CurrentTimeSelectionChanged();
            }
        }

        public virtual event Action CurrentTimeSelectionChanged;

        public virtual IEnumerable<DateTime> Times
        {
            get { return times; }
        }

        public virtual event Action TimesChanged;

        public virtual TimeSelectionMode SelectionMode
        {
            get { return TimeSelectionMode.Range; }
        }

        public virtual SnappingMode SnappingMode
        {
            get { return SnappingMode.None; }
        }

        private IList timeFilteredFeatures;
        private DateTime? timeSelectionStart;
        private DateTime? timeSelectionEnd;
    }
}