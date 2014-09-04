#region Using directives
using System;
using System.Collections;
#endregion

namespace Cross.Drawing
{
    /// <summary>
    /// Strongly typed collection of <see cref="ColorStop"/>.
    /// </summary>
    public class ColorRamp : IList, ICollection, IEnumerable
    {
        #region const
        /// <summary>
        /// saving number of element in dest array need to calculate color
        /// </summary>
        const int NumberOfElementInDestArray = 256;


        /// <summary>
        /// Number of element in dest array that saving 
        /// </summary>
        const int NumberOfElementInDestArrayForDownColorInRepeatMode = 8;
        /// <summary>
        /// saving number of element in dest array need to calculate color
        /// when in repeat mode
        /// </summary>
        const int NumberOfElementInDestArrayInRepeatMode = NumberOfElementInDestArray - NumberOfElementInDestArrayForDownColorInRepeatMode;

        #endregion

        #region EmptyColors
        /// <summary>
        /// Saving empty color, include 512 zero element
        /// </summary>
        internal static uint[] EmptyColors = new uint[512];
        #endregion

        #region Convenient Methods

        #region Add
        /// <summary>
        /// Adds a new color stop
        /// </summary>
        /// <param name="color">The color value of stop</param>
        /// <param name="stop">Position of stop (must be in range [0..1]</param>
        public int Add(Color color, double stop)
        {
            ColorStop cs = new ColorStop(color, stop);
            return Add(cs);
        }
        #endregion

        #endregion

        #region Fields
        /// <summary>
        /// internal data
        /// </summary>
        internal ColorStop[] mItems = null;
        /// <summary>
        /// Number of items currently in list
        /// </summary>
        internal int mCount = 0;

        //[NonSerialized]
        private object mSynchRoot;
        #endregion

        #region Capacity
        private int mCapacity;
        /// <summary>
        /// Gets current maximal capacity
        /// </summary>
        public int Capacity
        {
            get { return mCapacity; }
        }
        #endregion

        #region IList Members
        #region Add
        /// <summary>
        /// Adds an item to the list
        /// </summary>
        public int Add(ColorStop value)
        {
            //check if not enough space then allocate more
            if (mCount >= mCapacity)
            {
                //calculate new capacity
                int newCapacity;
                if (mCapacity < 1000) newCapacity = (int)(mCapacity * 2);
                else if (mCapacity < 5000) newCapacity = (int)(mCapacity * 1.5);
                else newCapacity = (int)(mCapacity * 1.2);

                //allocate new buffer
                ColorStop[] newItems = new ColorStop[newCapacity];

                //copy old values to new buffer
                mItems.CopyTo(newItems, 0);

                //assign the new bufer to current buffer
                mItems = newItems;
                mCapacity = newCapacity;
            }

            //assign the new value to the end of current buffer
            mItems[mCount] = value;
            mCount++;
            IsChangeStops = true;
            return mCount - 1;
        }

        /// <summary>
        /// Adds an item to the list
        /// </summary>
        int IList.Add(object value)
        {
            return Add((ColorStop)value);
        }
        #endregion

        #region Clear
        /// <summary>
        /// Removes all items
        /// </summary>
        public void Clear()
        {
            Array.Clear(mItems, 0, mCount);
            mCount = 0;
            IsChangeStops = true;
        }
        #endregion

        #region Contains
        /// <summary>
        /// Determines whether the list contains a specific value
        /// </summary>
        public bool Contains(object value)
        {
            if (value == null)
            {
                //search for the first occurence of null item
                for (int i = 0; i < mCount; i++)
                {
                    if (mItems[i] == null) return true;
                }
            }
            else
            {
                //search for the first match
                for (int i = 0; i < mCount; i++)
                {
                    if (value.Equals(mItems[i])) return true;
                }
            }

            return false;
        }
        #endregion

        #region Index Of
        /// <summary>
        /// Determines the index of a specific item
        /// </summary>
        public int IndexOf(ColorStop value)
        {
            if (value == null)
            {
                //search for the first occurence of null item
                for (int i = 0; i < mCount; i++)
                {
                    if (mItems[i] == null) return i;
                }
            }
            else
            {
                //search for the first match
                for (int i = 0; i < mCount; i++)
                {
                    if (value.Equals(mItems[i])) return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// Determines the index of a specific item
        /// </summary>
        int IList.IndexOf(object value)
        {
            return IndexOf((ColorStop)value);
        }
        #endregion

        #region Insert
        /// <summary>
        /// Inserts an item to the list at the specified index
        /// </summary>
        public void Insert(int index, ColorStop value)
        {
            //check if not enough space then allocate more
            if (mCount >= mCapacity)
            {
                //calculate new capacity
                int newCapacity;
                if (mCapacity < 1000) newCapacity = (int)(mCapacity * 2);
                else if (mCapacity < 5000) newCapacity = (int)(mCapacity * 1.5);
                else newCapacity = (int)(mCapacity * 1.2);

                //allocate new buffer
                ColorStop[] newItems = new ColorStop[newCapacity];

                //copy old values from left of index to new buffer
                if (index > 0)
                {
                    Array.Copy(mItems, 0, newItems, 0, index);
                }

                //assign the new value to the index position of the new buffer
                newItems[index] = value;

                //copy old values from right of index to new buffer
                if (index < mCount)
                {
                    Array.Copy(mItems, index, newItems, index + 1, mCount - index);
                }

                //assign the new bufer to current buffer
                mItems = newItems;
                mCapacity = newCapacity;
                mCount++;
            }
            else
            {
                //copy old values from right of index to new buffer
                if (index < mCount)
                {
                    Array.Copy(mItems, index, mItems, index + 1, mCount - index);
                }

                //assign the new value to the index position of the new buffer
                mItems[index] = value;
                mCount++;
            }
            IsChangeStops = true;
        }

        /// <summary>
        /// Inserts an item to the list at the specified index
        /// </summary>
        void IList.Insert(int index, object value)
        {
            Insert(index, (ColorStop)value);
        }
        #endregion

        #region Is Fixed Size
        /// <summary>
        /// Gets a value indicating whether the list has a fixed size.
        /// </summary>
        public bool IsFixedSize
        { get { return false; } }
        #endregion

        #region Is Read Only
        /// <summary>
        /// Gets a value indicating whether the list is read-only.
        /// </summary>
        public bool IsReadOnly
        { get { return false; } }
        #endregion

        #region Remove
        /// <summary>
        /// Removes the first occurrence of a specific object from the list.
        /// </summary>
        public void Remove(ColorStop value)
        {
            int index = IndexOf(value);
            if (index >= 0) RemoveAt(index);
            IsChangeStops = true;
        }

        /// <summary>
        /// Removes the first occurrence of a specific object from the list.
        /// </summary>
        void IList.Remove(object value)
        {
            int index = IndexOf((ColorStop)value);
            if (index >= 0) RemoveAt(index);

        }
        #endregion

        #region Remove At
        /// <summary>
        /// Removes the list item at the specified index
        /// </summary>
        public void RemoveAt(int index)
        {
            mCount--;
            if (index < mCount)
            {
                //copy old values from the right of index
                Array.Copy(mItems, index + 1, mItems, index, mCount - index);
            }
            mItems[mCount] = null;
            IsChangeStops = true;
        }
        #endregion

        #region Indexer
        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        public ColorStop this[int index]
        {
            get { return mItems[index]; }
            set { mItems[index] = value; }
        }

        /// <summary>
        /// Gets or sets the element at the specified index.
        /// </summary>
        object IList.this[int index]
        {
            get { return mItems[index]; }
            set { mItems[index] = (ColorStop)value; }
        }
        #endregion

        #endregion

        #region ICollection Members
        #region Copy To
        /// <summary>
        /// Copies the elements of the collection to an array
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from collection. The array must have zero-based indexing.</param>
        public void CopyTo(Array array)
        {
            Array.Copy(mItems, 0, array, 0, mCount);
        }

        /// <summary>
        /// Copies the elements of the collection to an array, starting at a particular array index. 
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from collection. The array must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in array at which copying begins.</param>
        public void CopyTo(Array array, int index)
        {
            Array.Copy(mItems, 0, array, index, mCount);
        }

        /// <summary>
        /// Copies the elements of the collection to an array, starting at a particular array index. 
        /// </summary>
        /// <param name="array">The one-dimensional array that is the destination of the elements copied from collection. The array must have zero-based indexing.</param>
        /// <param name="index">The zero-based index in this collection at which copying begins.</param>
        /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
        /// <param name="count">The number of elements to copy.</param>
        public void CopyTo(int index, Array array, int arrayIndex, int count)
        {
            Array.Copy(mItems, index, array, arrayIndex, count);
        }
        #endregion

        #region Count
        /// <summary>
        /// Gets the number of elements contained in the ICollection.
        /// </summary>
        public int Count
        { get { return mCount; } }
        #endregion

        #region Is Synchronized
        /// <summary>
        /// Gets a value indicating whether access to the ICollection is synchronized (thread safe).
        /// </summary>
        public bool IsSynchronized
        { get { return false; } }
        #endregion

        #region SyncRoot
        /// <summary>
        /// Gets an object that can be used to synchronize access to the ICollection.
        /// </summary>
        public object SyncRoot
        {
            get
            {
                if (mSynchRoot == null)
                {
                    System.Threading.Interlocked.CompareExchange(ref mSynchRoot, new object(), null);
                }
                return mSynchRoot;
            }
        }
        #endregion

        #endregion

        #region IEnumerable Members

        #region Get Enumerator
        /// <summary>
        /// Returns an enumerator that iterates through this collection
        /// </summary>
        public IEnumerator GetEnumerator()
        {
            return new ColorRampEnumerator(this);
        }

        #region Enumerator
        /// <summary>
        /// Strongly typed enumerator of ColorStop.
        /// </summary>
        public class ColorRampEnumerator : object, System.Collections.IEnumerator
        {
            #region privates
            /// <summary>
            /// Current index
            /// </summary>
            private int mIndex;

            /// <summary>
            /// Current element pointed to.
            /// </summary>
            private ColorStop mCurrentElement;

            /// <summary>
            /// Collection to enumerate.
            /// </summary>
            private ColorRamp mCollection;
            #endregion

            #region Constructor
            /// <summary>
            /// Default constructor for enumerator.
            /// </summary>
            /// <param name="collection">Instance of the collection to enumerate.</param>
            internal ColorRampEnumerator(ColorRamp collection)
            {
                mIndex = -1;
                mCollection = collection;
            }
            #endregion

            #region Current
            /// <summary>
            /// Gets the ColorStop object in the enumerated ColorStopCollection currently indexed by this instance.
            /// </summary>
            public ColorStop Current
            { get { return mCurrentElement; } }

            /// <summary>
            /// Gets the current element in the collection.
            /// </summary>
            object System.Collections.IEnumerator.Current
            { get { return mCurrentElement; } }
            #endregion

            #region Reset
            /// <summary>
            /// Reset the cursor, so it points to the beginning of the enumerator.
            /// </summary>
            public void Reset()
            {
                mIndex = -1;
                mCurrentElement = null;
            }
            #endregion

            #region MoveNext
            /// <summary>
            /// Advances the enumerator to the next queue of the enumeration, if one is currently available.
            /// </summary>
            /// <returns>true, if the enumerator was succesfully advanced to the next queue; false, if the enumerator has reached the end of the enumeration.</returns>
            public bool MoveNext()
            {
                mIndex++;
                if (mIndex < mCollection.mCount)
                {
                    mCurrentElement = mCollection.mItems[mIndex];
                    return true;
                }
                mIndex = mCollection.mCount;
                return false;
            }
            #endregion
        }
        #endregion

        #endregion

        #endregion

        #region Constructors
        /// <summary>
        /// Default constructor.
        /// <para>Default capacity is 50</para>
        /// </summary>
        public ColorRamp()
        {
            mCapacity = 50;
            mItems = new ColorStop[mCapacity];
        }

        /// <summary>
        /// Create a new instance with the specified capacity
        /// </summary>
        public ColorRamp(int capacity)
        {
            mCapacity = capacity;
            mItems = new ColorStop[capacity];
        }
        #endregion

        /// <summary>
        /// This boolean true when all color in color ramp
        /// have alpha value is 255.
        /// NOTE: This bool is set after call method Build only
        /// </summary>
        internal bool NoBlendingColor = false;

        #region Build with repeat mode
        /// <summary>
        /// Is current color ramp has been changed
        /// </summary>
        bool IsChangeStops = true;
        /// <summary>
        /// Old gradient style
        /// </summary>
        GradientStyle oldStyleMode = GradientStyle.Reflect;

        /// <summary>
        /// old opacity
        /// </summary>
        uint oldOpacity = 0;

        /// <summary>
        /// Saving current caced repeat color
        /// </summary>
        uint[] CachedRepeatColor = new uint[512];
        /// <summary>
        /// Build uint array (512 elements)
        /// </summary>
        /// <param name="mode">Reflect or Repeat mode.</param>
        internal uint[] Build(GradientStyle mode, uint opacity)
        {
            #region implementation to remove the aliased when repeat mode
            // when change
            if ((IsChangeStops) || (oldStyleMode != mode) || (opacity != oldOpacity))
            {
                if (mCount >= 2)
                {
                    #region build cache

                    uint[] destArray = CachedRepeatColor;
                    int i = 0;

                    int distance = 0;
                    int ik = 0;
                    uint beginColor = 0;
                    uint finishColor = 0;
                    uint beginColorAG = 0;
                    uint beginColorRB = 0;
                    uint finishColorAG = 0;
                    uint finishColorRB = 0;

                    if (mode != GradientStyle.Repeat)
                    {
                        #region when mode are reflect or pad
                        int start = (int)(mItems[0].mStop * NumberOfElementInDestArray + 0.5);
                        int end = 0;

                        // first color of gradient
                        uint color = mItems[0].Color.Data;
                        //apply opacity
                        color = ((((color >> 24) * opacity) >> 8) << 24) | (color & 0x00FFFFFF);

                        NoBlendingColor = true;
                        if ((color >> 24) < 255)
                        {
                            NoBlendingColor = false;
                        }
                        #region 0.0 -> this[0].offset
                        for (i = 0; i < start; i++)
                        {
                            destArray[i] = color;
                        }
                        #endregion
                        #region this[0].mStop -> this[Count-1].mStop
                        for (i = 1; i < Count; i++)
                        {
                            //temp = start;
                            end = (int)(mItems[i].mStop * NumberOfElementInDestArray + 0.5);
                            distance = end - start;

                            beginColor = mItems[i - 1].Color.Data;
                            finishColor = mItems[i].Color.Data;

                            //apply opacity
                            beginColor = ((((beginColor >> 24) * opacity) >> 8) << 24) | (beginColor & 0x00FFFFFF);
                            finishColor = ((((finishColor >> 24) * opacity) >> 8) << 24) | (finishColor & 0x00FFFFFF);

                            beginColorAG = ((beginColor >> 8) & 0x00FF00FF);
                            beginColorRB = (beginColor & 0x00FF00FF);

                            finishColorAG = ((finishColor >> 8) & 0x00FF00FF);
                            finishColorRB = (finishColor & 0x00FF00FF);

                            if ((finishColor >> 24) < 255)
                            {
                                NoBlendingColor = false;
                            }
                            while (start < end)
                            {
                                //result[start] = (this[i - 1].StopColor.Gradient(this[i].StopColor, (double)(start + distance - end) / distance)).Data;

                                ik = (int)(((double)(start + distance - end) / distance) * 256 + 0.5);
                                destArray[start] =
                                   ((((uint)(beginColorAG + (((finishColorAG - beginColorAG) * ik) >> 8)) << 8) & 0xFF00FF00) |
                                   ((uint)(beginColorRB + (((finishColorRB - beginColorRB) * ik) >> 8)) & 0x00FF00FF));

                                ++start;
                            }
                        }
                        #endregion
                        #region this[Count-1].offset -> 1.0
                        color = mItems[Count - 1].Color.Data;
                        //apply opacity
                        color = ((((color >> 24) * opacity) >> 8) << 24) | (color & 0x00FFFFFF);
                        if ((color >> 24) < 255)
                        {
                            NoBlendingColor = false;
                        }
                        for (; end < NumberOfElementInDestArray; end++)
                        {
                            destArray[end] = color;
                        }
                        #endregion
                        #endregion
                    }
                    else
                    {
                        #region when mode is repeat
                        int start = (int)(mItems[0].mStop * NumberOfElementInDestArrayInRepeatMode + 0.5);
                        int end = 0;

                        // first color of gradient
                        uint color = mItems[0].Color.Data;
                        //apply opacity
                        color = ((((color >> 24) * opacity) >> 8) << 24) | (color & 0x00FFFFFF);
                        NoBlendingColor = true;
                        if ((color >> 24) < 255)
                        {
                            NoBlendingColor = false;
                        }
                        #region 0.0 -> this[0].offset
                        for (i = 0; i < start; i++)
                        {
                            destArray[i] = color;
                        }
                        #endregion
                        #region this[0].mStop -> this[Count-1].mStop
                        for (i = 1; i < Count; i++)
                        {
                            //temp = start;
                            end = (int)(mItems[i].mStop * NumberOfElementInDestArrayInRepeatMode + 0.5);
                            distance = end - start;

                            beginColor = mItems[i - 1].Color.Data;
                            finishColor = mItems[i].Color.Data;
                            //apply opacity
                            beginColor = ((((beginColor >> 24) * opacity) >> 8) << 24) | (beginColor & 0x00FFFFFF);
                            finishColor = ((((finishColor >> 24) * opacity) >> 8) << 24) | (finishColor & 0x00FFFFFF);


                            beginColorAG = ((beginColor >> 8) & 0x00FF00FF);
                            beginColorRB = (beginColor & 0x00FF00FF);

                            finishColorAG = ((finishColor >> 8) & 0x00FF00FF);
                            finishColorRB = (finishColor & 0x00FF00FF);

                            if ((finishColor >> 24) < 255)
                            {
                                NoBlendingColor = false;
                            }
                            while (start < end)
                            {
                                //result[start] = (this[i - 1].StopColor.Gradient(this[i].StopColor, (double)(start + distance - end) / distance)).Data;

                                ik = (int)(((double)(start + distance - end) / distance) * 256 + 0.5);
                                destArray[start] =
                                   ((((uint)(beginColorAG + (((finishColorAG - beginColorAG) * ik) >> 8)) << 8) & 0xFF00FF00) |
                                   ((uint)(beginColorRB + (((finishColorRB - beginColorRB) * ik) >> 8)) & 0x00FF00FF));

                                ++start;
                            }
                        }
                        #endregion
                        #region this[Count-1].offset -> 1.0
                        color = mItems[Count - 1].Color.Data;
                        //apply opacity
                        color = ((((color >> 24) * opacity) >> 8) << 24) | (color & 0x00FFFFFF);
                        if ((color >> 24) < 255)
                        {
                            NoBlendingColor = false;
                        }
                        for (; start < NumberOfElementInDestArrayInRepeatMode; start++)
                        {
                            destArray[start] = color;
                        }
                        #endregion

                        #region the rest to make repeat mode more smooth
                        end = NumberOfElementInDestArray;
                        distance = end - start;

                        beginColor = mItems[mCount - 1].Color.Data;
                        finishColor = mItems[0].Color.Data;

                        beginColorAG = ((beginColor >> 8) & 0x00FF00FF);
                        beginColorRB = (beginColor & 0x00FF00FF);

                        finishColorAG = ((finishColor >> 8) & 0x00FF00FF);
                        finishColorRB = (finishColor & 0x00FF00FF);
                        while (start < end)
                        {
                            //result[start] = (this[i - 1].StopColor.Gradient(this[i].StopColor, (double)(start + distance - end) / distance)).Data;

                            ik = (int)(((double)(start + distance - end) / distance) * 256 + 0.5);
                            destArray[start] =
                               ((((uint)(beginColorAG + (((finishColorAG - beginColorAG) * ik) >> 8)) << 8) & 0xFF00FF00) |
                               ((uint)(beginColorRB + (((finishColorRB - beginColorRB) * ik) >> 8)) & 0x00FF00FF));

                            ++start;
                        }
                        #endregion
                        #endregion
                    }
                    #endregion

                    if (mode != GradientStyle.Pad)
                    {
                        // for repeat mode and reflect mode, need copy 256 element
                        Buffer.BlockCopy(CachedRepeatColor, 0, CachedRepeatColor, 256 * 4, 256 * 4);
                    }
                }

                if (opacity < 255)
                {
                    NoBlendingColor = false;
                }
                IsChangeStops = false;
                oldStyleMode = mode;
                oldOpacity = opacity;
                if (mode == GradientStyle.Reflect)
                {
                    //Buffer.BlockCopy(CachedRepeatColor, 0, CachedRepeatColor, 0, 256 * 4);
                    Buffer.BlockCopy(CachedRepeatColor, 0, CachedRepeatColor, 256 * 4, 256 * 4);
                    // reverve the back
                    Array.Reverse(CachedRepeatColor, 256, 256);
                }
            }
            return CachedRepeatColor;
            #endregion
        }
        #endregion
    }
}
