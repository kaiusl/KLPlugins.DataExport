using System;
using System.Collections.Generic;
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace KLPlugins.DataExport.Helpers
{

    /// <summary>
    /// Helper class for mapping enums to custom values.
    /// 
    /// It's based on an array and uses the discriminants of the enum as indices.
    /// Thus it assumes couple of things about the enum.
    ///   * It's values are distinct (otherwise two variants map to same index).
    ///   * It's values preferably start at 0 and are contiguous (otherwise we waste space).
    /// </summary>
    internal class EnumMap<E, T> : IEnumerable<T> where E : Enum
    {
        private readonly int _dataLen;
        private readonly T[] _data;
        public Func<E, T> Generator { get; private set; }
#if DEBUG
        private static bool _hasWarned = false;
#endif
        public EnumMap(T defValue) : this(_ => defValue) { }

        public EnumMap(Func<E, T> generator)
        {
            var values = Enum.GetValues(typeof(E)).Cast<E>().Select(e => Convert.ToInt32(e));
            var maxValue = values.Max();
#if DEBUG
            if (!EnumMap<E, T>._hasWarned)
            {
                var minValue = values.Min();
                var numValues = values.Count();
                var distinctValues = values.Distinct().Count();
                if (minValue != 0 || numValues != distinctValues || maxValue != numValues - 1)
                {
                    SimHub.Logging.Current.Warn($"KLPlugins.DynLeaderboards:\n    EnumMap<{typeof(E)}, {typeof(T)}> uses not an ideal enum:\n    min={minValue}, max={maxValue}, values={numValues}, distinctValues={distinctValues}");
                }
                EnumMap<E, T>._hasWarned = true;
            }
#endif
            // +1 to account for 0 value
            this._dataLen = maxValue + 1;
            this._data = new T[this._dataLen];
            this.Generator = generator;

            foreach (var v in this.GetEnumValues())
            {
                int index = Convert.ToInt32(v);
                this._data[index] = generator(v);
            }
        }

        public T this[E key]
        {
            get => this._data[Convert.ToInt32(key)];
            set => this._data[Convert.ToInt32(key)] = value;
        }

        public void Reset()
        {
            foreach (var v in this.GetEnumValues())
            {
                int index = Convert.ToInt32(v);
                this._data[index] = this.Generator(v);
            }
        }

        public IEnumerator<T> GetEnumerator()
        {
            foreach (var index in this.GetEnumIndices())
            {
                yield return this._data[index];
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private IEnumerable<E> GetEnumValues()
        {
            return Enum.GetValues(typeof(E)).Cast<E>();
        }

        private IEnumerable<int> GetEnumIndices()
        {
            return Enum.GetValues(typeof(E)).Cast<E>().Select(v => Convert.ToInt32(v));
        }
    }
}