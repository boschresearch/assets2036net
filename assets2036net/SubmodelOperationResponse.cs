// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace assets2036net
{
    /// <summary>
    /// class used for serialization of operation response payload message. Do not 
    /// instantiate by yourself. Use <seealso cref="SubmodelOperationRequest.CreateResponseObj"/>, 
    /// Then simply set your return value to <seealso cref="Value"/>
    /// </summary>
    [JsonObject(MemberSerialization.OptIn)]
    public class SubmodelOperationResponse : CommElementBase
    {
        /// <summary>
        /// The operations return value
        /// </summary>
        [JsonProperty("resp")]
        public object Value { get; set; }

        /// <summary>
        /// the operation's request id
        /// </summary>
        [JsonProperty("req_id")]
        public string RequestId { get; internal set; }

        internal SubmodelOperationResponse()
        {
        }

        /// <summary>
        /// Convenience method to add named parameters to an object type return parameter. Internally a dictionary is created which lateron is serilized to JSON.
        /// </summary>
        public SubmodelOperationResponse WithObjectValue(params (string, object)[] properties)
        {
            this.Value = Tools.BuildJsonObject(properties); 

            return this; 
        }

        private readonly static log4net.ILog log = Config.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName);

        internal SubmodelOperation Operation { get; set; }

        internal void Publish()
        {
            string message = JsonConvert.SerializeObject(this); 
            log.Debug("Send: " + message); 
            Asset.publish(
                BuildTopic(this.Operation.Topic, StringConstants.StringConstant_RESP),
                message, 
                false);
        }
    }
}
