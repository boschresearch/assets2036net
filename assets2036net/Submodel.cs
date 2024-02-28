// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace assets2036net
{
    /// <summary>
    /// Class Submodel. 
    /// In Software terms the Submodel is an interface, which is implemented by assets. 
    /// This submodel is used for serialization but also for accessing implementations, 
    /// both when accessing a remote asset via proxy and during the implementation of a local asset. 
    /// </summary>
    public class Submodel
    {
        /// <summary>
        /// The submodel's name. Used as third part in the mqtt topic structure. 
        /// </summary>
        [JsonPropertyName("name")]
        public string Name
        {
            get; set;
        }

        /// <summary>
        /// The submodel's revision number
        /// </summary>
        [JsonPropertyName("rev")]
        public string Revision
        {
            get; set;
        }

        /// <summary>
        /// The URL the submodel definition was loaded from
        /// </summary>
        [JsonIgnore]
        public string SubmodelUrl
        {
            get; internal set;
        }

        [JsonIgnore]
        public SubmodelProperty MetaProperty
        {
            get
            {
                return Property(StringConstants.PropertyNameMeta);
            }
        }

        /// <summary>
        /// Convinience method to get the Submodel meta-Property 
        /// </summary>
        [JsonIgnore]
        public MetaPropertyValue MetaPropertyValue
        {
            get
            {
                var p = Property(StringConstants.PropertyNameMeta);
                if (p == null)
                    return null;

                var mpv = p.GetValueAs<MetaPropertyValue>();
                return mpv; 
            }
        }


        public Submodel()
        {
            _properties = new Dictionary<string, SubmodelProperty>();
            _operations = new Dictionary<string, SubmodelOperation>();
            _events = new Dictionary<string, SubmodelEvent>();
        }

        // JObject repr. of the schema
        // internal JObject _schema;

        /// <summary>
        /// Convinience method to get one container with all submodel elements. The container is copy, 
        /// its elements are references to the actual elements. 
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, SubmodelElement> Elements
        {
            get
            {
                Dictionary<string, SubmodelElement> result = new Dictionary<string, SubmodelElement>();

                foreach (var kvp in Properties)
                {
                    result.Add(kvp.Key, kvp.Value);
                }

                foreach (var kvp in Operations)
                {
                    result.Add(kvp.Key, kvp.Value);
                }

                foreach (var kvp in Events)
                {
                    result.Add(kvp.Key, kvp.Value);
                }

                return result;
            }
        }

        /// <summary>
        /// Returns the submodel's property named <paramref name="name"/>
        /// </summary>
        /// <param name="name">the property name to return</param>
        /// <returns>the property named <paramref name="name"/> or null, if not present</returns>
        public SubmodelProperty Property(string name)
        {
            _properties.TryGetValue(name, out SubmodelProperty p);
            return p; 
        }

        private Dictionary<string, SubmodelProperty> _properties;

        [JsonPropertyName("properties")]
        public Dictionary<string, SubmodelProperty> Properties
        {
            get
            {
                return _properties; 
            }
            set
            {
                _properties = value; 

                foreach (var kvp in _properties)
                {
                    kvp.Value.Name = kvp.Key; 
                }
            }
        }

        /// <summary>
        /// get the operation names <paramref name="name"/>
        /// </summary>
        /// <param name="name">the name of the operation to return</param>
        /// <returns>the operation named <paramref name="name"/> or null, if not present
        /// operation was found</returns>
        public SubmodelOperation Operation(string name)
        {
            _operations.TryGetValue(name, out SubmodelOperation op);
            return op; 
        }

        private Dictionary<string, SubmodelOperation> _operations;
        [JsonPropertyName("operations")]
        public Dictionary<string, SubmodelOperation> Operations
        {
            get
            {
                return _operations;
            }
            set
            {
                _operations = value; 
                foreach(var kvp in _operations)
                {
                    kvp.Value.Name = kvp.Key; 
                }
            }
        }

        /// <summary>
        /// get the event named <paramref name="name"/>
        /// </summary>
        /// <param name="name">name of the event to be returned</param>
        /// <returns>the event named <paramref name="name"/> or null, if not present</returns>
        public SubmodelEvent Event(string name)
        {
            _events.TryGetValue(name, out SubmodelEvent ev);
            return ev; 
        }
        internal Dictionary<string, SubmodelEvent> _events;

        [JsonPropertyName("events")]
        public Dictionary<string, SubmodelEvent> Events
        {
            get
            {
                return _events; 
            }
            set
            {
                _events = value;

                foreach (var kvp in _events)
                {
                    kvp.Value.Name = kvp.Key; 
                }
            }
        }

        
        /// <summary>
        /// For inspection issues: Return the submodel's properties
        /// </summary>
        /// <returns>The submodel's properties as List</returns>
        public List<SubmodelProperty> GetProperties()
        {
            List<SubmodelProperty> result = new List<SubmodelProperty>();
            result.AddRange(_properties.Values);
            return result;
        }

        /// <summary>
        /// For inspection issues: Return the submodel's events
        /// </summary>
        /// <returns>The submodel's event as List</returns>
        public List<SubmodelEvent> GetEvents()
        {
            List<SubmodelEvent> result = new List<SubmodelEvent>();
            result.AddRange(_events.Values);
            return result;
        }

        /// <summary>
        /// For inspection issues: Return the submodel's operations
        /// </summary>
        /// <returns>The submodel's operations as List</returns>
        public List<SubmodelOperation> GetOperations()
        {
            List<SubmodelOperation> result = new List<SubmodelOperation>();
            result.AddRange(_operations.Values);
            return result;
        }



        internal void AddElement(SubmodelElement el)
        {
            if (el.GetType() == typeof(SubmodelProperty))
            {
                _properties.Add(el.Name, el as SubmodelProperty); 
            }
            else if (el.GetType() == typeof(SubmodelOperation))
            {
                _operations.Add(el.Name, el as SubmodelOperation);
            }
            else if (el.GetType() == typeof(SubmodelEvent))
            {
                _events.Add(el.Name, el as SubmodelEvent);
            }
            else throw new Exception("Unknown type of submodel element!"); 
        }

        internal AssetMgr AssetMgr { get; set; }
        internal Asset Asset { get; set; }

        internal void populateElements(AssetMgr assetMgr, Asset asset)
        {
            AssetMgr = assetMgr;
            Asset = asset;

            foreach (var el in Elements)
            {
                el.Value.Populate(assetMgr, asset, this);
                el.Value.Name = el.Key; 
            }
        }
    }

    /// <summary>
    /// Convinience class to encapsulate the elements of the mandatory "_meta" 
    /// property of each sbmodel instance
    /// </summary>
    public class MetaPropertyValue
    {
        /// <summary>
        /// source is the asset providing this submodel
        /// </summary>
        [JsonPropertyName(StringConstants.PropertyNameMetaSource)]
        public string Source { get; set; }

        /// <summary>
        /// the URL from where the submodel definition was originally read
        /// </summary>
        [JsonPropertyName(StringConstants.PropertyNameMetaSubmodelUrl)]
        public string Url { get; set; }

        /// <summary>
        /// the complete submodel definition
        /// </summary>
        [JsonPropertyName(StringConstants.PropertyNameMetaSubmodelSchema)]
        public Submodel SubmodelDefinition { get; set; }
    }


}
