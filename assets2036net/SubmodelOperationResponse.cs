// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace assets2036net
{
    /// <summary>
    /// class used for serialization of operation response payload message. Do not 
    /// instantiate by yourself. Use <seealso cref="SubmodelOperationRequest.CreateResponseObj"/>, 
    /// Then simply set your return value to <seealso cref="Value"/>
    /// </summary>
    public class SubmodelOperationResponse : CommElementBase
    {
        /// <summary>
        /// The operations return value
        /// </summary>
        [JsonPropertyName("resp")]
        public object Value { get; set; }

        /// <summary>
        /// the operation's request id
        /// </summary>
        [JsonPropertyName("req_id")]
        public string RequestId { get; set; }

        public SubmodelOperationResponse()
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

        [JsonIgnore]
        internal SubmodelOperation Operation { get; set; }

        internal void Publish()
        {
            string message = JsonSerializer.Serialize(this, Tools.JsonSerializerOptions); 
            log.Debug("Send: " + message); 
            Asset.publish(
                BuildTopic(this.Operation.Topic, StringConstants.StringConstant_RESP),
                message, 
                false);
        }

        public T GetReturnValueOrDefault<T>(T defVal = default)
        {
            if (Value == null)
            {
                return default; 
            }

            try
            {
                return ((JsonElement)this.Value).Deserialize<T>(); 
            }
            catch (Exception e)
            {
                log.Error($"error during deserialization of return value to type {typeof(T)}: {e}"); 
                return default; 
            }
        }
    }
}
