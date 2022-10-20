// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

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
    [JsonObject(MemberSerialization.OptIn)]
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
        public event HandleValueModified ValueModified;

        /// <summary>
        /// The property's datatype. <seealso cref="ValueType"/>
        /// </summary>
        [JsonProperty("type")]
        public ValueType Type { get; set; }

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
        /// Because the value is encapsulated in a Newtonsoft JToken, you have to transform it into 
        /// the native C#-type you expect. You can use the methods 
        /// <list type="bullet">
        /// <item><seealso cref="ValueBool"/></item>
        /// <item><seealso cref="ValueDouble"/></item>
        /// <item><seealso cref="ValueFloat"/></item>
        /// <item><seealso cref="ValueInt"/></item>
        /// <item><seealso cref="ValueString"/></item>
        /// </list>
        /// </summary>
        /// If you expect some special object or array type, use <seealso cref="GetValueAs{T}"/>. 
        public object Value
        {
            get
            {
                return _value;
            }
            set
            {
                if (Asset.Mode != Mode.Owner)
                {
                    throw new Exception("You cannot set property values on an asset proxy"); 
                }

                if (value != _value)
                {
                    _value = value;

                    if (Asset != null)
                    {
                        Publish();
                    }
                }
            }
        }

        private string _latestPublishedValueJson = "";

        private static readonly log4net.ILog log = Config.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName);

        internal void Publish()
        {
            if (Asset.Initialized())
            {
                // compare serialized values to check if publish
                string json = JsonConvert.SerializeObject(_value);
                if (json != _latestPublishedValueJson)
                {
                    Asset.publish(Topic, JsonConvert.SerializeObject(_value), true);
                    _latestPublishedValueJson = json;
                }
            }
        }

        /// <summary>
        /// returns the current property value as string
        /// </summary>
        public string ValueString
        {
            get
            {
                return Convert.ToString(Value);
            }
        }
        /// <summary>
        /// returns the current property value as int
        /// </summary>
        public int ValueInt
        {
            get
            {
                return Convert.ToInt32(Value);
            }
        }
        /// <summary>
        /// returns the current property value as double
        /// </summary>
        public double ValueDouble
        {
            get
            {
                return Convert.ToDouble(Value);
            }
        }
        /// <summary>
        /// returns the current property value as float
        /// </summary>
        public float ValueFloat
        {
            get
            {
                return Convert.ToSingle(Value);
            }
        }

        /// <summary>
        /// returns the current property value as boolean
        /// </summary>
        public bool ValueBool
        {
            get
            {
                return Convert.ToBoolean(Value);
            }
        }

        /// <summary>
        /// returns the current property value as generic type T. 
        /// Implemented as JsonConvert.PopulateObject(ValueObject.ToString(), result);
        /// </summary>
        public T GetValueAs<T>() where T : new()
        {
            T result = new T();
            JsonConvert.PopulateObject(ValueObject.ToString(), result);
            return result;
        }

        /// <summary>
        /// returns the current property value as <seealso cref="JObject"/>
        /// </summary>
        public JObject ValueObject
        {
            get
            {
                return Value as JObject;
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
