// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;

namespace assets2036net
{
    /// <summary>
    /// Convinience class: When implementing asset submodel providers and you want to 
    /// work with the endpoint submodel, the asset manager will offer an instance 
    /// of this class to do so. 
    /// Use the endpoint submodel to handle calls to endpoint submodel like restart, shutdown
    /// or to emit log messages from this endpoint. It also implements the ping method of 
    /// the endpoint submodel. 
    /// <seealso cref="AssetMgr.EndpointAsset"/>
    /// </summary>
    public class AssetEndpoint
    {
        readonly Asset _asset; 

        /// <summary>
        /// a native asset object to access the endpoint submodel. 
        /// </summary>
        public Asset Asset
        {
            get { return _asset;  }
        }

        //private readonly static log4net.ILog logger = Config.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName);

        /// <summary>
        /// the endpoint asset's name
        /// </summary>
        public string Name
        {
            get
            {
                return Asset.Name; 
            }
        }

        /// <summary>
        /// returns an arbitrary submodel of the endpoint asset
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Submodel Submodel(string name)
        {
            return Asset.Submodel(name); 
        }

        /// <summary>
        /// returns the endpoint submodel of the asset
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public Submodel SubmodelEndpoint
        {
            get
            {
                return Asset.SubmodelEndpoint; 
            }
        }

        /// <summary>
        /// return the value of the Healthy-property of the endpoint submodel
        /// </summary>
        public bool Healthy
        {
            get
            {
                return _asset.Submodel(StringConstants.SubmodelNameEnpoint).Property(StringConstants.PropertyNameHealthy).ValueBool;
            }
            set
            {
                _asset.Submodel(StringConstants.SubmodelNameEnpoint).Property(StringConstants.PropertyNameHealthy).Value = value;
            }
        }

        internal AssetEndpoint(Asset asset)
        {
            _asset = asset;
            _asset.Submodel(
                StringConstants.SubmodelNameEnpoint).Operation(StringConstants.OperationNamePing).Callback = this.ping; 
        }

        internal SubmodelOperationResponse ping(SubmodelOperationRequest req)
        {
            var resp = req.CreateResponseObj();
            return resp; 
        }

        /// <summary>
        /// use this method to send log messages by the endpoint submodel event "log" 
        /// </summary>
        /// <param name="message"></param>
        public void log(string message)
        {
            _asset.Submodel(StringConstants.SubmodelNameEnpoint).Event("log").Emit(new Dictionary<string, object>()
            {
                {"entry", message}
            });
        }
    }
}
