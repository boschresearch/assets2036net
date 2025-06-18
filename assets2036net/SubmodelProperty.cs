// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;

namespace assets2036net
{
    /// <summary>
    /// The SubmodelProperty represents the property of a Submodel. A property has a 
    /// type (JSON schema types) and a name. 
    /// When implementing a submodel provider, when you set the <seealso cref="Value"/>, 
    /// it will be published to all proxies so that they will be informed about the new value. 
    /// When implementing a proxy, you can simply read the <seealso cref="Value"/> or 
    /// use one of the typed methods <seealso cref="ValueBool"/>, <seealso cref="ValueInt"/> 
    /// <seealso cref="ValueDouble"/, <seealso cref="ValueFloat"/>, <seealso cref="ValueInt"/>, 
    /// <seealso cref="ValueString"/> to get the value as desired type. 
    /// Listen to the event <seealso cref="ValueModified"/> on the proxy side to get informed, if 
    /// the property valued changes. 
    /// </summary>
    public class SubmodelProperty : SubmodelElement
    {
        /// <summary>
        /// Delegate definition you have to implement, if you want to get informed about property
        /// value changes. 
        /// </summary>
        /// <param name="p">Reference to the SubmodelProperty which emits the event</param>
        /// <param name="oldValue">The old property's value</param>
        /// <param name="newValue">the new and current property's value</param>
        public delegate void HandleValueModified(SubmodelProperty p, object oldValue, object newValue);

        /// <summary>
        /// When working with an asset proxy, you can add a listener to this event to get informed, when
        /// the value on the remote asset changes. 
        /// <seealso cref="HandleValueModified"/>
        /// </summary>
        //public event HandleValueModified ValueModified;
        public event HandleValueModified ValueModified;

        /// <summary>
        /// The property's datatype. <seealso cref="ValueType"/>
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; }

        internal void updateLocalValue(object newValue)
        {
            if (newValue != _value)
            {
                var oldValue = _value;
                _value = newValue;
                ValueModified?.Invoke(this, oldValue, _value);
            }
        }

        internal object _value;

        /// <summary>
        /// Used to grab the property value. 
        /// When implementing a submodel provider, use the Value.Set to set new values for this 
        /// property and automatically inform all remote asset proxies on the network. 
        /// When implementing an asset consumer, use Value to grab the current value or alternatively 
        /// subscribe to the event <seealso cref="HandleValueModified"/> and get informed, if the 
        /// remote value changes. 
        /// Because the value is encapsulated in a System.Text.Json.JsonElement, you have to transform it into 
        /// the native C#-type you expect. You can use the methods 
        /// <list type="bullet">
        /// <item><seealso cref="ValueBool"/></item>
        /// <item><seealso cref="ValueDouble"/></item>
        /// <item><seealso cref="ValueFloat"/></item>
        /// <item><seealso cref="ValueInt"/></item>
        /// <item><seealso cref="ValueString"/></item>
        /// </list>
        /// </summary>
        /// If you expect some special object or array type, use <seealso cref="ValueAs{T}"/>. 
        [JsonIgnore]
        public object Value
        {
            get
            {
                // if (Asset.Mode != Mode.Consumer)
                // {
                //     throw new Exception("You cannot read property values on an asset owner");
                // }

                return _value;
            }
            set
            {
                if (Asset.Mode != Mode.Owner)
                {
                    throw new Exception("You cannot set property values on an asset proxy");
                }

                _value = value;

                if (Asset != null)
                {
                    Publish();
                }
            }
        }

        private string _latestPublishedValueJson = "";

        private static readonly log4net.ILog log = Config.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName);

        /// <summary>
        /// removes from broker by resetting the retained message. 
        /// </summary>
        /// <returns></returns>
        public void RemoveFromBroker()
        {
            if (Asset.Initialized())
            {
                AssetMgr.Publish(Topic, null, true);
            }
        }

        internal void Publish()
        {
            if (Asset.Initialized())
            {
                string json = null;

                json = JsonSerializer.Serialize(
                    _value,
                    Tools.JsonSerializerOptions);

                if (json != _latestPublishedValueJson)
                {
                    AssetMgr.Publish(Topic, json, true);
                    _latestPublishedValueJson = json;
                }
            }
        }

        /// <summary>
        /// returns the current property value as string
        /// </summary>
        [JsonIgnore]
        public string ValueString
        {
            get
            {
                if (Value == null)
                {
                    return null;
                }
                else
                {
                    return ((JsonElement)Value).GetString();
                }
            }
        }
        /// <summary>
        /// returns the current property value as int
        /// </summary>
        [JsonIgnore]
        public int ValueInt
        {
            get
            {
                if (Value == null)
                {
                    return (int)0;
                }
                else
                {
                    return (int)((JsonElement)Value).GetInt32();
                }
            }
        }
        /// <summary>
        /// returns the current property value as double
        /// </summary>
        [JsonIgnore]
        public double ValueDouble
        {
            get
            {
                if (Value == null)
                {
                    return 0.0f;
                }
                else
                {
                    return (double)((JsonElement)Value).GetDouble();
                }
            }
        }
        /// <summary>
        /// returns the current property value as float
        /// </summary>
        [JsonIgnore]
        public float ValueFloat
        {
            get
            {
                if (Value == null)
                {
                    return 0.0f;
                }
                else
                {
                    return (float)((JsonElement)Value).GetDouble();
                }
            }
        }

        /// <summary>
        /// returns the current property value as boolean
        /// </summary>
        [JsonIgnore]
        public bool ValueBool
        {
            get
            {
                if (Value == null)
                {
                    return false;
                }
                else
                {
                    return ((JsonElement)Value).GetBoolean();
                }
            }
        }

        /// <summary>
        /// returns the current property value as generic type T. 
        /// implemented: JsonSerializer.Deserialize<T>(JsonSerializer.ValueObject.ToString()); 
        /// </summary>
        public T ValueAs<T>() where T : new()
        {
            if (Value is T)
            {
                return (T)Value;
            }
            else
            {
                return ((JsonElement)Value).Deserialize<T>(
                    Tools.JsonSerializerOptions
                );
            }
        }

        /// <summary>
        /// returns the current property value as Dictionary<string, object>
        /// </summary>
        [JsonIgnore]
        public Dictionary<string, object> ValueObject
        {
            get
            {
                return JsonSerializer.Deserialize<Dictionary<string, object>>(
                    JsonSerializer.Serialize(Value));
            }
        }

        internal override ISet<string> getSubscriptions(Mode mode)
        {
            if (mode == Mode.Consumer)
            {
                log.InfoFormat("{0} subscribes to {1}", Name, Topic);
                return new HashSet<string>() { Topic };
            }
            else return new HashSet<string>();
        }
    }
}
