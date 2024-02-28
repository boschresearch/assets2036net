// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace assets2036net
{
    /// <summary>
    /// Instances of class SubmodelOperationRequest encapsulate one request to an asset operation.
    /// Its serialization is used as payload on the wire. When you implement operations for your ohwn asset
    /// or submodel, all parameters to an operation will be encapsulated inside an instance of 
    /// SubmodelOperationRequest. 
    /// </summary>
    public class SubmodelOperationRequest : CommElementBase
    {
        /// <summary>
        /// The unique request id for an operation call. Used to map request and answer on the client side. 
        /// </summary>
        [JsonPropertyName("req_id")]
        public string RequestId { get; set; }

        /// <summary>
        /// a dictionary containig the request's corresponding parameters. 
        /// </summary>
        [JsonPropertyName("params")]
        public Dictionary<string, object> Parameters { get; set; }

        /// <summary>
        /// Referehnce to the submodel operation which will be used to handle this request. 
        /// </summary>
        [JsonIgnore]
        public SubmodelOperation Operation { get; set; }

        /// <summary>
        /// When implementing your own submodel provider, the operation handler needs to return 
        /// a SubmodelOperationRequest-object. This operation builds one correspondung to this
        /// SubmodelOperationRequest, so that e.g. the request-id is automatically set for the response. 
        /// </summary>
        /// <returns>a prefilled SubmodelOperationResponse corresponding to this request. You only need 
        /// to set the return value using <seealso cref="SubmodelOperationResponse.Value"/></returns>
        public SubmodelOperationResponse CreateResponseObj()
        {
            return new SubmodelOperationResponse()
            {
                AssetMgr = this.AssetMgr,
                Asset = this.Asset,
                Operation = this.Operation, 
                //Name = SubmodelElement.buildTopic(Operation.Name, StringConstants.StringConstant_RESP),
                RequestId = this.RequestId,
                Submodel = this.Submodel
            };
        }


        private readonly static log4net.ILog log = Config.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName);

        public SubmodelOperationRequest()
        {
            Parameters = new Dictionary<string, object>(); 
        }

        internal SubmodelOperationRequest(SubmodelOperation operation)
            : this()
        {
            Operation = operation; 
            //Name = SubmodelElement.buildTopic(operation.Name, StringConstants.StringConstant_REQ);
            Parameters = new Dictionary<string, object>();
        }

        public T ParameterValueOrDefault<T>(string parameterKey, T defaultValue = default)
        {
            if (!Parameters.ContainsKey(parameterKey))
            {
                return defaultValue; 
            }

            var value = Parameters[parameterKey]; 

            try
            {
                var res = (T)Convert.ChangeType(value, typeof(T));
                return res; 
            }
            catch (Exception)
            {
                return defaultValue; 
            }
        }

        public List<string> ValidateParameters(Dictionary<string, Type> parameters)
        {
            List<string> result = new List<string>(); 

            foreach(var kvp in parameters)
            {
                if (!Parameters.ContainsKey(kvp.Key))
                {
                    result.Add(kvp.Key);
                    continue; 
                }

                var value = Parameters[kvp.Key]; 

                try
                {
                    var res = Convert.ChangeType(value, kvp.Value); 
                }
                catch (Exception)
                {
                    result.Add(kvp.Key); 
                }
            }

            return result; 
        }


        public bool ValidateParameter<T>(string parameterKey)
        {
            if (!Parameters.ContainsKey(parameterKey))
            {
                return false; 
            }

            var value = Parameters[parameterKey]; 

            try
            {
                var res = Convert.ChangeType(value, typeof(T)); 
                return true; 
            }
            catch (Exception)
            {
                return false; 
            }
        }

        internal void Publish()
        {
            string message = JsonSerializer.Serialize(this,  Tools.JsonSerializerOptions);

            var topic = BuildTopic(Operation.Topic, StringConstants.StringConstant_REQ); 
            log.DebugFormat("Send: {0} @ {1}", message, topic);
            Asset.publish(
                topic,
                message,
                false);
        }

    }
}
