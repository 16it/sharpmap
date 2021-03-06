using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;

namespace DelftTools.Utils.Collections.Generic
{
    using DelftTools.Utils.Aop;
    using log4net;

    /// <summary>
    /// A list that supports IEventedList so the outside world
    /// can tell when changes occur
    /// </summary>
    /// <typeparam name="T">The type of element being stored in the list</typeparam>
    [Serializable]
    public class EventedList<T> : IEventedList<T>, IList, INotifyPropertyChange // TODO: move INotifyPropertyChanged to interface and fix aspect
    {
        private static readonly ILog eventsLog = LogManager.GetLogger("Events");

        // WARNING: any change here should be mirrored in >>>>>>>>> PersistentEventedList <<<<<<<<<< !!!!!!!
        // TODO: make PersistentEventedList<T> and EventedList<T> use the same impl
        // TODO: make it work on Ranges and send events on ranges

        /// <summary>
        /// The underlying storage
        /// </summary>
        private readonly List<T> list;

        #region Constructors

        /// <summary>
        /// Construct me
        /// </summary>
        public EventedList(): this(null)
        {
        }

        /// <summary>
        /// Construct me
        /// </summary>
        /// <param name="initialData">The initialization data</param>
        /// <param name="bubbleChildEvents">Specifies whether property/collection changed events on the items themselves are bubbled</param>
        public EventedList(IEnumerable<T> initialData)
        {
            list = initialData == null ? new List<T>() : new List<T>(initialData);

            // subscribe to existing items
            foreach (var o in list)
            {
                SubscribeEvents(o);
            }
        }

        private void InitializeDelegates()
        {
            Item_PropertyChangedDelegate = Item_PropertyChanged;
            Item_PropertyChangingDelegate = Item_PropertyChanging;
            Item_CollectionChangingDelegate = Item_CollectionChanging;
            Item_CollectionChangedDelegate = Item_CollectionChanged;
        }

        #endregion

        // re-use event delegates, for performance reasons (10x speedup at add/remove)
        private PropertyChangingEventHandler Item_PropertyChangingDelegate;
        private PropertyChangedEventHandler Item_PropertyChangedDelegate;
        private NotifyCollectionChangingEventHandler Item_CollectionChangingDelegate;
        private NotifyCollectionChangedEventHandler Item_CollectionChangedDelegate;

        #region IList<T> Members

        public int IndexOf(T item)
        {
            return list.IndexOf(item);
        }

        public void Insert(int index, T item)
        {
            CheckReadOnly();
            if(!OnAdding(item, index))
            {
                return;
            }
            list.Insert(index, item);
            OnAdded(item, index);
        }

        private void CheckReadOnly()
        {
            if (IsReadOnly)
            {
                throw new ReadOnlyException("Collection is read-only");
            }
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <param name="value">The <see cref="T:System.Object" /> to remove from the <see cref="T:System.Collections.IList" />. </param>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList" /> is read-only.-or- The <see cref="T:System.Collections.IList" /> has a fixed size. </exception><filterpriority>2</filterpriority>
        public void Remove(object value)
        {
            Remove((T) value);
        }

        public void RemoveAt(int index)
        {
            CheckReadOnly();
            var item = this[index];
            if(!OnRemoving(item, index))
            {
                return;
            }
            list.RemoveAt(index);
            OnRemoved(item, index);
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        /// <returns>
        /// The element at the specified index.
        /// </returns>
        /// <param name="index">The zero-based index of the element to get or set. </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index" /> is not a valid index in the <see cref="T:System.Collections.IList" />. </exception>
        /// <exception cref="T:System.NotSupportedException">The property is set and the <see cref="T:System.Collections.IList" /> is read-only. </exception><filterpriority>2</filterpriority>
        object IList.this[int index]
        {
            get { return this[index]; }
            set { this[index] = (T)value; }
        }

        public T this[int index]
        {
            get { return list[index]; }
            set
            {
                CheckReadOnly();
                var old = this[index];
                if(!OnReplacing(old, index))
                {
                    return;
                }
                list[index] = value;
                OnReplaced(value, old, index);
            }
        }

        #endregion

        #region ICollection<T> Members
        
        public void Add(T item)
        {
            CheckReadOnly();
            if(!OnAdding(item, Count))
            {
                return;
            }
            list.Add(item);
            OnAdded(item,list.Count - 1);
        }

        /// <summary>
        /// Adds an item to the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <returns>
        /// The position into which the new element was inserted.
        /// </returns>
        /// <param name="value">The <see cref="T:System.Object" /> to add to the <see cref="T:System.Collections.IList" />. </param>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList" /> is read-only.-or- The <see cref="T:System.Collections.IList" /> has a fixed size. </exception><filterpriority>2</filterpriority>
        int IList.Add(object value)
        {
            Add((T) value);
            return Count - 1;
        }

        /// <summary>
        /// Determines whether the <see cref="T:System.Collections.IList" /> contains a specific value.
        /// </summary>
        /// <returns>
        /// true if the <see cref="T:System.Object" /> is found in the <see cref="T:System.Collections.IList" />; otherwise, false.
        /// </returns>
        /// <param name="value">The <see cref="T:System.Object" /> to locate in the <see cref="T:System.Collections.IList" />. </param><filterpriority>2</filterpriority>
        public bool Contains(object value)
        {
            return value is T && Contains((T) value);
        }

        public void Clear()
        {
            CheckReadOnly();

            while(list.Count != 0)
            {
                RemoveAt(list.Count - 1);
            }
        }

        /// <summary>
        /// Determines the index of a specific item in the <see cref="T:System.Collections.IList" />.
        /// </summary>
        /// <returns>
        /// The index of <paramref name="value" /> if found in the list; otherwise, -1.
        /// </returns>
        /// <param name="value">The <see cref="T:System.Object" /> to locate in the <see cref="T:System.Collections.IList" />. </param><filterpriority>2</filterpriority>
        public int IndexOf(object value)
        {
            return IndexOf((T) value);
        }

        /// <summary>
        /// Inserts an item to the <see cref="T:System.Collections.IList" /> at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index at which <paramref name="value" /> should be inserted. </param>
        /// <param name="value">The <see cref="T:System.Object" /> to insert into the <see cref="T:System.Collections.IList" />. </param>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index" /> is not a valid index in the <see cref="T:System.Collections.IList" />. </exception>
        /// <exception cref="T:System.NotSupportedException">The <see cref="T:System.Collections.IList" /> is read-only.-or- The <see cref="T:System.Collections.IList" /> has a fixed size. </exception>
        /// <exception cref="T:System.NullReferenceException"><paramref name="value" /> is null reference in the <see cref="T:System.Collections.IList" />.</exception><filterpriority>2</filterpriority>
        public void Insert(int index, object value)
        {
            Insert(index, (T)value);
        }

        public bool Contains(T item)
        {
            return list.Contains(item);
        }

        public void CopyTo(T[] array, int arrayIndex)
        {
            list.CopyTo(array, arrayIndex);
        }

        /// <summary>
        /// Copies the elements of the <see cref="T:System.Collections.ICollection" /> to an <see cref="T:System.Array" />, starting at a particular <see cref="T:System.Array" /> index.
        /// </summary>
        /// <param name="array">The one-dimensional <see cref="T:System.Array" /> that is the destination of the elements copied from <see cref="T:System.Collections.ICollection" />. The <see cref="T:System.Array" /> must have zero-based indexing. </param>
        /// <param name="index">The zero-based index in <paramref name="array" /> at which copying begins. </param>
        /// <exception cref="T:System.ArgumentNullException"><paramref name="array" /> is null. </exception>
        /// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="index" /> is less than zero. </exception>
        /// <exception cref="T:System.ArgumentException"><paramref name="array" /> is multidimensional.-or- <paramref name="index" /> is equal to or greater than the length of <paramref name="array" />.-or- The number of elements in the source <see cref="T:System.Collections.ICollection" /> is greater than the available space from <paramref name="index" /> to the end of the destination <paramref name="array" />. </exception>
        /// <exception cref="T:System.ArgumentException">The type of the source <see cref="T:System.Collections.ICollection" /> cannot be cast automatically to the type of the destination <paramref name="array" />. </exception><filterpriority>2</filterpriority>
        public void CopyTo(Array array, int index)
        {
            var i = 0;
            foreach (var o in list)
            {
                if (i >= index)
                {
                    array.SetValue(o, i);
                }

                i++;
            }
        }

        public virtual int Count
        {
            get { return list.Count; }
        }

        public object SyncRoot
        {
            get { return syncRoot; }
        }

        public bool IsSynchronized
        {
            get { return isSynchronized; }
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        /// <summary>
        /// Gets a value indicating whether the <see cref="T:System.Collections.IList" /> has a fixed size.
        /// </summary>
        /// <returns>
        /// true if the <see cref="T:System.Collections.IList" /> has a fixed size; otherwise, false.
        /// </returns>
        public bool IsFixedSize
        {
            get { return false; }
        }

        private object syncRoot;
        private bool isSynchronized;

        public bool Remove(T item)
        {
            CheckReadOnly();
            var index = list.IndexOf(item);
            if (index == -1)
            {
                return false;
            }

            if(!OnRemoving(item, index))
            {
                return false;
            }
            if (list.Remove(item))
            {
                OnRemoved(item, index);
                return true;
            }
            return false;
        }

        #endregion

        #region IEnumerable<T> Members

        public void AddRange(IEnumerable<T> enumerable)
        {
            foreach (var o in enumerable)
            {
                Add(o);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            return list.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return list.GetEnumerator();
        }

        #endregion

        #region IEventedList Helpers

        protected virtual bool OnRemoving(T item, int index)
        {
            return FireCollectionChangingEvent(NotifyCollectionChangeAction.Remove, item, index);
        }

        private bool OnReplacing(T item, int index)
        {
            return FireCollectionChangingEvent(NotifyCollectionChangeAction.Replace, item, index);
        }

        protected virtual bool OnAdding(T item, int index)
        {
            return FireCollectionChangingEvent(NotifyCollectionChangeAction.Add, item, index);
        }

        private void OnReplaced(object item, object oldItem, int index)
        {
            UnsubscribeEvents(oldItem);
            SubscribeEvents(item);
            FireCollectionChangedEvent(NotifyCollectionChangeAction.Replace,item,  index,oldItem);
        }

        private void OnAdded(object item, int index)
        {
            SubscribeEvents(item);
            FireCollectionChangedEvent(NotifyCollectionChangeAction.Add, item, index);
        }
        private void OnRemoved(T item, int index)
        {
            UnsubscribeEvents(item);
            FireCollectionChangedEvent(NotifyCollectionChangeAction.Remove, item, index);
        }

        private bool FireCollectionChangingEvent(NotifyCollectionChangeAction action, T item, int index)
        {
            EditActionAttribute.FireBeforeEventCall(this, false);

            if (CollectionChanging != null)
            {
                if(EventSettings.EnableLogging)
                    eventsLog.DebugFormat(EditActionAttribute.BubbleIndent + "CollectionChanging L>> '{0}[{1}]', item:{2}, index:{3}, action:{4} - BEGIN >>>>>>>>>>>>>>", "EventedList", typeof(T).Name, item, index, action);

                var args = new NotifyCollectionChangingEventArgs(action, item, index, -1);

                try
                {
                    CollectionChanging(this, args);
                }
                catch
                {
                    EditActionAttribute.FireAfterEventCall(this, false, args.Cancel);
                    throw;
                }

                if(args.Cancel)
                {
                    EditActionAttribute.FireAfterEventCall(this, false, args.Cancel);
                }

                return !args.Cancel;
            }

            return true;
        }

        private void FireCollectionChangedEvent(NotifyCollectionChangeAction action, object item, int index, object oldItem = null)
        {
            try
            {
                if (CollectionChanged != null)
                {
                    EditActionAttribute.BubbleIndentCounter++;
                    if (EventSettings.EnableLogging)
                        eventsLog.DebugFormat(EditActionAttribute.BubbleIndent + "CollectionChanged L<< '{0}[{1}]', item:{2}, index:{3}, action:{4} - END <<<<<<<<<<<<<<", "EventedList", typeof(T).Name, item, index, action);

                    var args = new NotifyCollectionChangingEventArgs(action, item, index, -1) { OldItem = oldItem };
                    CollectionChanged(this, args);
                    EditActionAttribute.BubbleIndentCounter--;
                }
            }
            finally
            {
                EditActionAttribute.FireAfterEventCall(this, false, false);
            }
        }

        private void Item_CollectionChanging(object sender, NotifyCollectionChangingEventArgs e)
        {
            // forwards event to subscribers of the list
            if (CollectionChanging != null)
            {
                EditActionAttribute.BubbleIndentCounter++;
                if (EventSettings.EnableLogging)
                {
                    if (sender.GetType().Name.Contains("EventedList"))
                    {
                        var senderTypeName = sender.GetType().GetGenericArguments()[0].Name;
                        eventsLog.DebugFormat(EditActionAttribute.BubbleIndent + "CollectionChanging L>> '{0}[{1}]' -> '{5}[{6}]', item:{2}, index:{3}, action:{4}", "EventedList", senderTypeName, e.Item, e.Index, e.Action, "EventedList", typeof(T).Name);
                    }
                    else
                    {
                        eventsLog.DebugFormat(EditActionAttribute.BubbleIndent + "CollectionChanging L>> '{0}[{1}]' -> '{5}[{6}]', item:{2}, index:{3}, action:{4}", sender, sender.GetType().Name, e.Item, e.Index, e.Action, "EventedList", typeof(T).Name);
                    }
                }

                CollectionChanging(sender, e);
                EditActionAttribute.BubbleIndentCounter--;
            }
        }

        void Item_CollectionChanged(object sender, NotifyCollectionChangingEventArgs e)
        {
            // forwards event to subscribers of the list
            if (CollectionChanged != null)
            {
                EditActionAttribute.BubbleIndentCounter++;
                if (EventSettings.EnableLogging)
                {
                    if (sender.GetType().Name.Contains("EventedList"))
                    {
                        var senderTypeName = sender.GetType().GetGenericArguments()[0].Name;
                        eventsLog.DebugFormat(EditActionAttribute.BubbleIndent + "CollectionChanged L<< '{0}[{1}]' -> '{5}[{6}]', item:{2}, index:{3}, action:{4}", "EventedList", senderTypeName, e.Item, e.Index, e.Action, "EventedList", typeof(T).Name);
                    }
                    else
                    {
                        eventsLog.DebugFormat(EditActionAttribute.BubbleIndent + "CollectionChanged L<< '{0}[{1}]' -> '{5}[{6}]', item:{2}, index:{3}, action:{4}", sender, sender.GetType().Name, e.Item, e.Index, e.Action, "EventedList", typeof(T).Name);
                    }
                }

                CollectionChanged(sender, e);
                EditActionAttribute.BubbleIndentCounter--;
            }
        }

        void Item_PropertyChanging(object sender, PropertyChangingEventArgs e)
        {
            // forwards event to subscribers of the list
            if (PropertyChanging != null)
            {
                EditActionAttribute.BubbleIndentCounter++;
                if (EventSettings.EnableLogging)
                {
                    eventsLog.DebugFormat(EditActionAttribute.BubbleIndent + "PropertyChanging L>> '{0}.{1}': '{2}[{3}]' -> '{4}[{5}]'", sender.GetType().Name, e.PropertyName, sender, sender.GetType().Name, "EventedList", typeof(T).Name);
                }

                PropertyChanging(sender, e);

                EditActionAttribute.BubbleIndentCounter--;
            }
        }

        void Item_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {

            // forwards event to subscribers of the list
            if (PropertyChanged != null)
            {
                EditActionAttribute.BubbleIndentCounter++;
                if (EventSettings.EnableLogging)
                {
                    eventsLog.DebugFormat(EditActionAttribute.BubbleIndent + "PropertyChanged L<< '{0}.{1}': '{2}[{3}]' -> '{4}[{5}]'", sender.GetType().Name, e.PropertyName, sender, sender.GetType().Name, "EventedList", typeof(T).Name);
                }

                PropertyChanged(sender, e);

                EditActionAttribute.BubbleIndentCounter--;
            } 
        }

        /// <summary>
        /// Unsubscribes PropertyChanged and CollectionChanged handlers it item supports them.
        /// </summary>
        /// <param name="item">The item to detach the handlers from</param>
        private void UnsubscribeEvents(object item)
        {
            var notifyPropertyChange = item as INotifyPropertyChange;
            if (notifyPropertyChange != null)
            {
//                if (((INotifyCollectionChange)this).HasParentIsCheckedInItems)
//                {
//                    EntityAttribute.UnsetParent(notifyPropertyChange);
//                }

                notifyPropertyChange.PropertyChanging -= Item_PropertyChangingDelegate;
                notifyPropertyChange.PropertyChanged -= Item_PropertyChangedDelegate;
            }

            var notifyCollectionChange = item as INotifyCollectionChange;
            if (notifyCollectionChange != null)
            {
                notifyCollectionChange.CollectionChanged -= Item_CollectionChangedDelegate;
                notifyCollectionChange.CollectionChanging -= Item_CollectionChangingDelegate;
            }
        }

        /// <summary>
        /// Subscribes PropertyChanged and CollectionChanged it item supports them
        /// </summary>
        /// <param name="item">The item to detach the handlers from</param>
        private void SubscribeEvents(object item)
        {
            if(((INotifyCollectionChange)this).SkipChildItemEventBubbling)
            {
                return;
            }
            
            var notifyPropertyChange = item as INotifyPropertyChange;
            if (notifyPropertyChange != null)
            {
//                if (((INotifyCollectionChange)this).HasParentIsCheckedInItems)
//                {
//                    EntityAttribute.SetParent(notifyPropertyChange);
//                }

                if (Item_PropertyChangedDelegate == null)
                {
                    InitializeDelegates();
                }
                notifyPropertyChange.PropertyChanged += Item_PropertyChangedDelegate;
                notifyPropertyChange.PropertyChanging += Item_PropertyChangingDelegate;
            }

            var notifyCollectionChanged = item as INotifyCollectionChange;
            if (notifyCollectionChanged != null)
            {
                if (Item_PropertyChangedDelegate == null)
                {
                    InitializeDelegates();
                }
                notifyCollectionChanged.CollectionChanged += Item_CollectionChangedDelegate;
                notifyCollectionChanged.CollectionChanging += Item_CollectionChangingDelegate;
            }
        }

        #endregion

        public event NotifyCollectionChangingEventHandler CollectionChanging;
        public event NotifyCollectionChangedEventHandler CollectionChanged;
        
        public event PropertyChangingEventHandler PropertyChanging;
        public event PropertyChangedEventHandler PropertyChanged;

        public bool HasParent { get; set; }

        bool INotifyCollectionChange.HasParentIsCheckedInItems { get; set; }

        private bool skipChildItemEventBubbling;

        bool INotifyCollectionChange.SkipChildItemEventBubbling
        {
            get { return skipChildItemEventBubbling; }
            set
            {
                if (skipChildItemEventBubbling == value)
                    return;

                skipChildItemEventBubbling = value;

                if (Count > 0)
                {
                    foreach (var item in list)
                    {
                        if (value)
                            UnsubscribeEvents(item);
                        else
                            SubscribeEvents(item);
                    }
                }
            }
        }
    }
}