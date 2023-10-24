// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;

namespace assets2036net
{
    /// <summary>
    /// Asset is the central access point to work with assets2036-Assets. You do not create instances 
    /// by yourself - instead use the factory methods 
    /// <seealso cref="AssetMgr.CreateAsset(string, string, Uri[])"/>, 
    /// <seealso cref="AssetMgr.CreateAsset(string, Uri[])"/>, 
    /// <seealso cref="AssetMgr.CreateAssetProxy(string, string, Uri[])"/>, 
    /// <seealso cref="AssetMgr.CreateAssetProxy(string, Uri[])"/>, 
    /// <seealso cref="AssetMgr.CreateFullAssetProxy(string, string)"/>
    /// </summary>
    public class Asset
    {
        private readonly static log4net.ILog log = Config.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName);

        /// <summary>
        /// this' asset's namespace. Set during creation. First part of asset's mqtt messages' topic. 
        /// </summary>
        public string Namespace { get; private set; }

        /// <summary>
        /// this' asset's name. Set during creation. Second part of asset's mqtt messages' topic. 
        /// </summary>
        public string Name { get; internal set; }

        internal Dictionary<string, Submodel> _submodels { get; private set; }

        /// <summary>
        /// Offers a list (copy) containing the asset's submodels. 
        /// </summary>
        public List<Submodel> Submodels
        {
            get
            {
                List<Submodel> result = new List<Submodel>();
                foreach (var sm in _submodels.Values)
                {
                    result.Add(sm);
                }
                return result;
            }
        }

        /// <summary>
        /// Mode describes, if this asset is a locally implemented asset <seealso cref="Mode.Owner"/> or a proxy communicating 
        /// with a remotely implemented asset <seealso cref="Mode.Consumer"/>. 
        /// </summary>
        public Mode Mode { get; internal set; }

        protected Dictionary<string, Action> CustomCallbacks { get; private set; }

        /// <summary>
        /// Convenience property to access the endpoint submodel of this asset. Analogue to 
        /// Submodel("_endpoint") <seealso cref="Submodel(string)"/>. 
        /// Will return null, if no endpoint is available. 
        /// </summary>
        public Submodel SubmodelEndpoint
        {
            get
            {
                _submodels.TryGetValue(StringConstants.SubmodelNameEnpoint, out Submodel ep);
                return ep;
            }
        }

        /// <summary>
        /// Returns the full asset's name in the form [namespace]/[name]. 
        /// </summary>
        public string FullName {
            get {
                return string.Format("{0}/{1}", this.Namespace, this.Name); 
            }
        }

        /// <summary>
        /// returns this asset's AssetMgr. <seealso cref="AssetMgr"/>
        /// </summary>
        public AssetMgr AssetMgr { get; internal set; }

        /// <summary>
        /// returns this asset's submodel named <paramref name="name"/>. Needed when accessing the submodel's 
        /// properties, operations and events. 
        /// </summary>
        /// <param name="name">the submodel's name (no the url - just the short name. Third part of the topic)</param>
        /// <returns></returns>
        public Submodel Submodel(string name)
        {
            return _submodels[name];
        }

        internal Asset(string @namespace, string name, AssetMgr assetMgr)
        {
            _submodels = new Dictionary<string, Submodel>();
            //_mapReqIdResponse = new Dictionary<string, SubmodelOperationResponse>();

            Namespace = @namespace;
            Name = name;
            AssetMgr = assetMgr;




            CustomCallbacks = new Dictionary<string, Action>();

            log.DebugFormat("Exit constructor Asset({0}, {1}, {2}, {3})", @namespace, name, Mode, assetMgr);
        }

        internal void publishAllProperties()
        {
            if (Mode == Mode.Owner)
            {
                // publish all properties initially
                foreach (var kvpSubmodel in _submodels)
                {
                    foreach (var p in kvpSubmodel.Value.Properties)
                    {
                        if (p.Value.Value != null)
                        {
                            p.Value.Publish();
                        }
                    }
                }
            }
        }

        internal bool Initialized()
        {
            return AssetMgr._mqttClient != null;
        }

        internal void addSubmodel(Submodel submodel)
        {
            _submodels.Add(submodel.Name, submodel);
        }


        internal ISet<string> getSubscriptions(Mode mode)
        {
            var subscriptions = new HashSet<string>(); 
            foreach (var kvpSubmodel in _submodels)
            {
                foreach (var kvpSme in kvpSubmodel.Value.Elements)
                {
                    subscriptions.UnionWith(kvpSme.Value.getSubscriptions(mode)); 
                }
            }
            return subscriptions; 
        }
        //internal void createSubscriptions()
        //{
        //    foreach (var kvpSubmodel in _submodels)
        //    {
        //        foreach (var kvpSme in kvpSubmodel.Value.Elements)
        //        {
        //            kvpSme.Value.createSubscriptions(AssetMgr._mqttClient, Mode);
        //        }
        //    }
        //}

        internal void publish(string topic, string text, bool retain)
        {
            log.DebugFormat("Asset {0} pub: {1} @ {2}", Name, text, topic);
            AssetMgr.Publish(topic, text, retain); 
        }
    }
}
