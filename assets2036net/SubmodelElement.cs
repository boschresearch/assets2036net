// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace assets2036net
{
    /// <summary>
    /// Base class of all submodel elements (Property, Operation and Event) 
    /// </summary>
    // [JsonObject(MemberSerialization.OptIn)]
    public abstract class SubmodelElement : CommElementBase
    {
        /// <summary>
        /// Name of the submodel element
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// description of the submodel element
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; }

        private static readonly log4net.ILog log = Config.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName);

        internal abstract ISet<string> getSubscriptions(Mode mode); 

        //internal abstract void createSubscriptions(IMqttClient mqttClient, Mode mode);

        private string _topic = null; 
        /// <summary>
        /// Topic of the submodel element. Used internally for MQTT communication. 
        /// </summary>
        [JsonIgnore]
        public string Topic
        {
            get
            {
                if (_topic == null)
                {
                    _topic = BuildTopic(Asset.Namespace, Asset.Name, Submodel.Name, Name);
                }

                return _topic; 
            }
        }
    }
}
