// 
// DictionaryIndex.cs
//  
// Author:
//       Henning Rauch <Henning@RauchEntwicklung.biz>
// 
// Copyright (c) 2012 Henning Rauch
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using System.Linq;
using System.Collections.Generic;
using Fallen8.Model;
using Fallen8.API.Plugin;
using System.Collections.Concurrent;

namespace Fallen8.API.Index
{
    /// <summary>
    /// Dictionary index.
    /// </summary>
    public sealed class DictionaryIndex : IIndex
    {
        #region Data
        
        /// <summary>
        /// The description.
        /// </summary>
        private PluginDescription _description;
        
        /// <summary>
        /// The lock object.
        /// </summary>
        private readonly Object _lockObject;
        
        /// <summary>
        /// The index dictionary.
        /// </summary>
        private Dictionary<IComparable, HashSet<IGraphElementModel>> _idx;
        
        #endregion
  
        #region Constructor
        
        /// <summary>
        /// Initializes a new instance of the <see cref="Fallen8.API.Index.DictionaryIndex"/> class.
        /// </summary>
        public DictionaryIndex ()
        {
            _lockObject = new object ();
            _description = new PluginDescription (
                "DictionaryIndex",
                typeof(DictionaryIndex),
                typeof(IIndex),
                "A very conservative directory index",
                "Henning Rauch");
        }
        
        #endregion
        
        #region IIndex implementation
        public long CountOfKeys ()
        {
            long result;
            lock (_lockObject) {
                result = _idx.Keys.Count;
            }
            return result;
        }

        public long CountOfValues ()
        {
            long result;
            lock (_lockObject) {
                result = _idx.Values.SelectMany (_ => _).Count ();
            }
            return result;
        }

        public void AddOrUpdate (IComparable key, IGraphElementModel graphElement)
        {
            lock (_lockObject) {
                
                HashSet<IGraphElementModel> values;
                if (_idx.TryGetValue (key, out values)) {
                
                    values.Add (graphElement);
                
                } else {
                    
                    values = new HashSet<IGraphElementModel> ();
                    values.Add (graphElement);
                    _idx.Add (key, values);
                }
            }
        }

        public bool TryRemoveKey (IComparable key)
        {
            Boolean removedSomething;
            
            lock (_lockObject) {
                removedSomething = _idx.Remove (key);
            }
            return removedSomething;
        }

        public void RemoveValue (IGraphElementModel graphElement)
        {
            lock (_lockObject) {
                foreach (var aKV in _idx) {
                    aKV.Value.Remove (graphElement);
                }
            }
        }
        
        public void Wipe ()
        {
            lock (_lockObject) {
                _idx.Clear ();
            }
        }

        public IEnumerable<IComparable> GetKeys ()
        {
            List<IComparable> result;
            
            lock (_lockObject) {
                result = new List<IComparable> (_idx.Keys);
            }
            
            return result;
        }


        public IEnumerable<KeyValuePair<IComparable, IEnumerable<IGraphElementModel>>> GetKeyValues ()
        {
            lock (_lockObject) {
                foreach (var aKV in _idx) 
                    yield return new KeyValuePair<IComparable, IEnumerable<IGraphElementModel>>(aKV.Key, new List<IGraphElementModel>(aKV.Value));
            }
            
            yield break;
        }

        public bool GetValue (out IEnumerable<IGraphElementModel> result, IComparable key)
        {
            Boolean foundSth;
            
            lock (_lockObject) {
                HashSet<IGraphElementModel> graphElements;
                
                foundSth = _idx.TryGetValue (key, out graphElements);
                
                if (foundSth) {
                    result = new List<IGraphElementModel>(graphElements);
                } else {
                    result = null;
                }
            }
            
            return foundSth;
        }
        #endregion

        #region IFallen8Plugin implementation
        public IFallen8Plugin Initialize (IFallen8Session fallen8Session, IDictionary<string, object> parameter)
        {
            _idx = new Dictionary<IComparable, HashSet<IGraphElementModel>> ();
            
            return this;
        }

        public PluginDescription Description {
            get {
                return _description;
            }
        }
        #endregion
    }
}
