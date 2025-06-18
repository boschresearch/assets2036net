// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using MQTTnet;
using MQTTnet.Client;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace assets2036net
{
    public enum Mode
    {
        Owner,
        Consumer
    }

    /// <summary>
    /// Class AssetMgr. The starting point for your assets2036 implementation. An instance of 
    /// AssetMgr will create Assets and AssetProxies for you. It holds the MQTT client, which is 
    /// used by all assets and assetProxies created via this. 
    /// </summary>
    public partial class AssetMgr : IDisposable
    {
        /// <summary>
        /// Event is emitted, when connection to MQTT broker is lost
        /// </summary>
        public event Action LostConnection;


        /// <summary>
        /// AssetMgr Constructor. 
        /// </summary>
        /// <param name="host">The MQTT Broker's hostname. Valid Examples:
        ///     <list type="bullet">
        ///     <item>"192.168.2.3"</item>
        ///     <item>"test.mosquitto.org"</item>
        ///     </list>
        /// </param>
        /// <param name="port">The MQTT Broker's port number. Typical: 1883.</param>
        /// <param name="namespace">Some default namespace for your assets. When creating an assets 
        /// without explicitely defining a namespace, this defaul namespace will be used. </param>
        /// <param name="endpointName">If you create Assets and not only AssetProxies using this 
        /// AssetMgr, you need to specify the name of the endpoint which actively implements 
        /// the assets. Imagine you implement a service which offers the positions of small load 
        /// carriers and each slc is modelled to be its own asset. In that case the service is the 
        /// endpoint which all the slc assets should refer to as their endpoint. In that case for 
        /// example you would name the endpoint "slc_service". 
        /// Internally, the assetMgr will create this endpoint asset automatically when it is used 
        /// to create the first asset. If you only create AssetProxies using this AssetMgr, you can 
        /// ignore this param.</param>
        public AssetMgr(string host, int port, string @namespace, string endpointName, string knownSessionId = "")
        {
            BrokerHost = host;
            BrokerPort = port;
            Namespace = @namespace;
            _endpointName = endpointName;
            _mqttKnownSessionId = knownSessionId;

            _consumerAssets = new ConcurrentDictionary<string, ConcurrentBag<Asset>>();
            _ownerAssets = new ConcurrentDictionary<string, ConcurrentBag<Asset>>();
            _mapReqIdResponse = new ConcurrentDictionary<string, SubmodelOperationResponse>();
            _mqttKnownSessionId = knownSessionId;

            Connect();

            var ep = _createOwnedAsset(@namespace, _endpointName, new Uri(Config.EndpointSubmodelDescriptionUrl));
            EndpointAsset = new AssetEndpoint(ep);
            EndpointAsset.Submodel(StringConstants.SubmodelNameEnpoint).Property(StringConstants.PropertyNameOnline).Value = true;
        }

        /// <summary>
        /// The MQTT Broker's host. Set in constructor. 
        /// </summary>
        public string BrokerHost { get; private set; }

        /// <summary>
        /// The MQTT Broker's port. Set in constructor. 
        /// </summary>
        public int BrokerPort { get; private set; }

        /// <summary>
        /// The AssetsMgr's default namespace. Used for assets and proxies, for which you 
        /// didn't define a namespace explicitely and for this AssetMgr's endpoint if needed. 
        /// </summary>
        public string Namespace { get; private set; }

        /// <summary>
        /// When the AssetMgr is used to create assets (not only AssetProxies), the AssetMgr
        /// will automatically create an EnpointAsset representing the computing unit which 
        /// implements the assets. This EnpointAsset is hold in this property, if it exists. 
        /// </summary>
        public AssetEndpoint EndpointAsset { get; private set; }


        private CancellationTokenSource _healthyCallbackTaskCTS = null;
        private Task _healthyCallbackTask = null;
        private string _mqttKnownSessionId = "";
        public TimeSpan HealthyCallbackRefreshRate { get; set; } = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Set a callback which will be called periodically to check the healthy status of 
        /// an endpoint associated by this asset manager. 
        /// </summary>
        /// <param name="callback">delegate to check the healthiness status of this asset</param>
        public Func<bool> HealthyCallback
        {
            get => _healthyCallback;
            set
            {
                _healthyCallback = value;
                var healthyProperty = EndpointAsset.Submodel(StringConstants.SubmodelNameEnpoint).Property(StringConstants.PropertyNameHealthy);
                if (value == null)
                {
                    _stopHealthyCallbackTask();
                    healthyProperty.Value = false;
                }
                else
                {
                    _stopHealthyCallbackTask();
                    _healthyCallbackTaskCTS = new CancellationTokenSource();
                    _healthyCallbackTask = Task.Run(() =>
                    {
                        while (!_healthyCallbackTaskCTS.Token.IsCancellationRequested)
                        {
                            try
                            {
                                bool result = _healthyCallback.Invoke();
                                log.Debug($"healthyCallback returned {result}");
                                healthyProperty.Value = result;
                            }
                            catch (Exception e)
                            {
                                log.Error($"an error occured during the healthy callback: \n{e}");
                                healthyProperty.Value = false;
                            }

                            Thread.Sleep(HealthyCallbackRefreshRate);
                        }
                    },
                    _healthyCallbackTaskCTS.Token);
                }
            }
        }

        private void _stopHealthyCallbackTask()
        {
            if (_healthyCallbackTask != null)
            {
                _healthyCallbackTaskCTS.Cancel();
                _healthyCallbackTask.Wait();
                _healthyCallbackTask = null;
            }
        }

        // public void SetHealthyCheck(Func<bool> callback)
        // {
        //     _healthyCallback = callback;
        // }

        /// <summary>
        /// Creates and returns an (owned) asset. The newly created asset is managed by this assetMgr. You
        /// have to implement the asset's submodels, set its properties, implement its operations and emit 
        /// its events. The asset's namespace equals this asset manager's default namespace. 
        /// </summary>
        /// <param name="assetName">The new asset's name</param>
        /// <param name="submodels">List of urls to the submodels to be implemented by the new asset</param>
        /// <seealso cref="CreateAsset(string, string, Uri[])"/>
        /// <returns>the newly created asset</returns>
        public Asset CreateAsset(string assetName, params Uri[] submodels)
        {
            return CreateAsset(this.Namespace, assetName, submodels);
        }

        /// <summary>
        /// Creates and returns an (owned) asset. The newly created asset is managed by this assetMgr. You
        /// have to implement the asset's submodels, set its properties, implement its operations and emit 
        /// its events. 
        /// </summary>
        /// <param name="namespace">The new asset's namespace</param>
        /// <param name="assetName">The new asset's name</param>
        /// <param name="submodels">List of urls to the submodels to be implemented by the new asset</param>
        /// <seealso cref="CreateAsset(string, Uri[])"/>
        /// <returns>the newly created asset</returns>
        public Asset CreateAsset(string @namespace, string assetName, params Uri[] submodels)
        {
            var asset = _createOwnedAsset(@namespace, assetName, submodels);
            return asset;
        }

        /// <summary>
        /// Create a proxy for an existing asset, so that you can locally communicate with the 
        /// remote asset. 
        /// </summary>
        /// <param name="assetName">the name of the remote asset. Second part of all mqtt topics corresponding to the asset</param>
        /// <param name="submodels">List of urls to the submodels to to be used via the new proxy. </param>
        /// <returns>the newly created asset proxy</returns>
        public Asset CreateAssetProxy(string assetName, params Uri[] submodels)
        {
            return CreateAssetProxy(Namespace, assetName, submodels);
        }

        /// <summary>
        /// Create a proxy for an existing asset, so that you can locally communicate with the 
        /// remote asset. 
        /// </summary>
        /// <param name="namespace">The namespace of the proxy's asset</param>
        /// <param name="assetName">the name of the remote asset. Second part of all mqtt topics corresponding to the asset</param>
        /// <param name="submodels">List of urls to the submodels to to be used via the new proxy. </param>
        /// <returns>the newly created asset proxy</returns>
        public Asset CreateAssetProxy(string @namespace, string assetName, params Uri[] submodelUrls)
        {
            var submodels = _parseSubmodels(submodelUrls);
            return _createAssetProxy(@namespace, assetName, submodels);
        }

        /// <summary>
        /// Create a proxy for an existing asset supporting all submodels the asset supports. AssetMgr
        /// subscribes to the meta topic of the defined asset, waits for received meta messages, 
        /// reads the given submodel descriptions and thereby is able to automatically offer all 
        /// implemented submodels. 
        /// If you want to create a proxy for an asset, which is not yet available, you can use 
        /// CreateAssetProxy and offer the expected Submodels manually. 
        /// </summary>
        /// <param name="namespace">The namespace of the proxy's asset</param>
        /// <param name="assetName">The proxy's asset's name</param>
        /// <returns></returns>
        public Asset CreateFullAssetProxy(string @namespace, string assetName)
        {
            var submodels = GetSupportedSubmodels(@namespace, assetName);
            return _createAssetProxy(@namespace, assetName, submodels);
        }

        private readonly ConcurrentDictionary<string, ConcurrentBag<Asset>> _consumerAssets;
        private readonly ConcurrentDictionary<string, ConcurrentBag<Asset>> _ownerAssets;

        // private NJsonSchema.JsonSchema _submodelSchema;

        private bool validateSubmodel(string json, List<string> errors = null)
        {
            // if (_submodelSchema == null)
            // {
            //    string submodelSchema = Config.Assets2036SubmodelSchema;

            //    var taskSchema = NJsonSchema.JsonSchema.FromJsonAsync(submodelSchema);
            //    _submodelSchema = taskSchema.Result;
            // }

            // bool valid = true;
            // foreach (var error in _submodelSchema.Validate(json))
            // {
            //    valid = false;

            //    if (errors != null)
            //    {
            //        errors.Add(string.Format("{0} at line {1}", error.ToString(), error.LineNumber));
            //    }
            // }

            // return valid;
            return true;
        }

        private Func<bool> _healthyCallback;

        private List<Submodel> _parseSubmodels(params Uri[] submodelUrls)
        {
            var submodels = new List<Submodel>();
            foreach (Uri submodelUri in submodelUrls)
            {
                var submodelDefinition = LoadTextFrom(submodelUri);

                List<string> errors = new List<string>();
                if (!validateSubmodel(submodelDefinition, errors))
                {
                    log.ErrorFormat("Validation of submodel {0} failed: \n {1} \n Continue with other submodels.",
                        submodelUri.ToString(),
                        string.Join("\n", errors));

                    continue;
                }


                var submodel = JsonSerializer.Deserialize<Submodel>(
                    submodelDefinition,
                    new JsonSerializerOptions
                    {
                        Converters = { new JsonStringEnumConverter() }
                    });

                submodel.SubmodelUrl = submodelUri.ToString();

                submodels.Add(submodel);
            }

            return submodels;
        }

        private Asset _createAssetProxy(string @namespace, string assetName, List<Submodel> submodels)
        {
            var proxy = _createBaseAsset(@namespace, assetName, submodels);
            proxy.Mode = Mode.Consumer;

            // add meta property to each submodel: 
            foreach (var submodel in submodels)
            {
                submodel.Properties.Add(
                    StringConstants.PropertyNameMeta,
                    new SubmodelProperty
                    {
                        Name = StringConstants.PropertyNameMeta,
                        Asset = submodel.Asset,
                        AssetMgr = submodel.AssetMgr,
                        Submodel = submodel
                    }
                );
            }

            if (!_consumerAssets.TryGetValue(proxy.FullName, out ConcurrentBag<Asset> bag))
            {
                bag = new ConcurrentBag<Asset>();
                _consumerAssets.TryAdd(proxy.FullName, bag);
            }
            bag.Add(proxy);

            var subscriptions = proxy.getSubscriptions(Mode.Consumer);
            var tasks = new List<Task>();

            foreach (var topic in subscriptions)
            {
                tasks.Add(_mqttClient.SubscribeAsync(topic));
            }

            Task.WaitAll(tasks.ToArray());

            return proxy;
        }

        private Asset _createBaseAsset(string @namespace, string assetName, List<Submodel> submodels)
        {
            var asset = new Asset(@namespace, assetName, this);

            foreach (var submodel in submodels)
            {
                submodel.populateElements(this, asset);
                asset.addSubmodel(submodel);
            }

            return asset;
        }

        private Asset _createOwnedAsset(string @namespace, string assetName, params Uri[] submodels)
        {
            var fullAssetName = string.Format("{0}/{1}", @namespace, assetName);

            var asset = _createBaseAsset(@namespace, assetName, _parseSubmodels(submodels));
            asset.Mode = Mode.Owner;

            if (!_ownerAssets.ContainsKey(fullAssetName))
            {
                _ownerAssets.TryAdd(fullAssetName, new ConcurrentBag<Asset>());
            }
            _ownerAssets[fullAssetName].Add(asset);

            foreach (Submodel submodel in asset.Submodels)
            {
                // for the metaproperty submodelDefinition we need a true copy of the submodel, 
                // without the _meta property! 

                var metaValue = new MetaPropertyValue
                {
                    Source = Topic.From(Namespace, this._endpointName),
                    SubmodelDefinition = submodel,
                    Url = submodel.SubmodelUrl
                };

                SubmodelProperty meta = new SubmodelProperty();
                meta.Populate(this, asset, submodel);
                meta.Name = StringConstants.PropertyNameMeta;
                meta.Value = metaValue;

                submodel.AddElement(meta);
            }


            if (_mqttClient != null)
            {
                var subscriptions = asset.getSubscriptions(Mode.Owner);
                var tasks = new List<Task>();

                foreach (var topic in subscriptions)
                {
                    tasks.Add(_mqttClient.SubscribeAsync(topic));
                }

                Task.WaitAll(tasks.ToArray());
            }

            return asset;
        }

        private readonly ConcurrentDictionary<Uri, (DateTime, string)> _submodelsCache = new ConcurrentDictionary<Uri, (DateTime, string)>();

        private string LoadTextFrom(Uri locator, bool noSslValidation = true)
        {
            try
            {
                var cachedValue = _submodelsCache[locator];

                if ((DateTime.Now - cachedValue.Item1).TotalSeconds <= 30)
                {
                    return cachedValue.Item2;
                }
            }
            catch (Exception)
            {
            }


            string text = null;

            switch (locator.Scheme)
            {
                case ("file"):
                    {
                        text = File.ReadAllText(locator.AbsolutePath);
                        break;
                    }
                case ("http"):
                case ("https"):
                    {
                        if (noSslValidation)
                        {
                            try
                            {
                                //Change SSL checks so that all checks pass
                                ServicePointManager.ServerCertificateValidationCallback =
                                   new RemoteCertificateValidationCallback(
                                        delegate { return true; }
                                   );
                            }
                            catch (Exception ex)
                            {
                                log.Error(ex);
                            }
                        }

                        var webClient = new WebClient();
                        text = webClient.DownloadString(locator);

                        break;
                    }
                default:
                    throw new Exception("Unknown URI schema");
            }

            if (_submodelsCache.ContainsKey(locator))
            {
                DateTime datetime = DateTime.Now;
                string json = "";
                var tuple = (datetime, json);
                _submodelsCache.TryRemove(locator, out tuple);
            }

            _submodelsCache.TryAdd(locator, (DateTime.Now, text));

            return text;
        }

        // TODO: override finalizer only if 'Dispose(bool disposing)' has code to free unmanaged resources
        ~AssetMgr()
        {
            // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
            Dispose(disposing: false);
        }

        /// <summary>
        /// Stops all started tasks and disconnects from the mqtt broker. 
        /// </summary>
        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
            // Suppress finalization.
            GC.SuppressFinalize(this);
        }

        bool disposedValue = false;

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects)
                }

                // TODO: free unmanaged resources (unmanaged objects) and override finalizer
                // TODO: set large fields to null
                _stopHealthyCallbackTask();
                // if (EndpointAsset != null && EndpointAsset.Asset != null)
                // {
                //     EndpointAsset.Asset.RemoveAllSubmodelsPropertiesFromBroker(); 
                // }

                if (_mqttClient != null && _mqttClient.IsConnected)
                {
                    _mqttClient.DisconnectAsync().Wait();
                    _mqttClient = null;
                }

                disposedValue = true;
            }
        }

        internal IMqttClient _mqttClient;

        internal virtual void Connect()
        {
            try
            {
                if (_mqttClient != null)
                {
                    _mqttClient.DisconnectAsync().Wait();
                }
            }
            catch (Exception e)
            {
                log.Error(e);
            }

            var factory = new MqttFactory();
            _mqttClient = factory.CreateMqttClient();

            _mqttClient.ApplicationMessageReceivedAsync += this.HandleApplicationMessageReceivedAsync;
            _mqttClient.DisconnectedAsync += this.HandleDisconnectedAsync;

            var messageBuilder = new MqttApplicationMessageBuilder();

            var optionsBuilder = new MqttClientOptionsBuilder()
                .WithTcpServer(BrokerHost, BrokerPort)
                .WithWillRetain(true)
                .WithWillPayload(JsonSerializer.Serialize(false))
                .WithWillTopic(CommElementBase.BuildTopic(
                        Namespace,
                        _endpointName,
                        StringConstants.SubmodelNameEnpoint,
                        StringConstants.PropertyNameOnline));

            if (_mqttKnownSessionId != "")
            {
                optionsBuilder = optionsBuilder
                    .WithCleanSession(false)
                    .WithClientId(_mqttKnownSessionId);
            }
            else
            {
                optionsBuilder = optionsBuilder
                    .WithCleanSession(true)
                    .WithClientId(_mqttClientId);
            }

            var options = optionsBuilder.Build();

            log.InfoFormat("{0} connects to {1}:{2}", _mqttClientId, BrokerHost, BrokerPort);
            _mqttClient.ConnectAsync(options, CancellationToken.None).Wait();
        }

        internal void Publish(string topic, string text, bool retain, byte[] correlationData = null)
        {
            try
            {
                lock (unpublishedMessagedLock)
                {
                    var messageBuilder = new MqttApplicationMessageBuilder();
                    messageBuilder = messageBuilder.WithTopic(topic);
                    messageBuilder = messageBuilder.WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce);
                    messageBuilder = messageBuilder.WithRetainFlag(retain);

                    if (text != null)
                    {
                        messageBuilder = messageBuilder.WithPayload(text);
                    }

                    if (correlationData != null)
                    {
                        messageBuilder = messageBuilder.WithCorrelationData(correlationData);
                    }

                    _mqttClient.PublishAsync(messageBuilder.Build());
                }
            }
            catch (Exception e)
            {
                log.ErrorFormat("Publish failed: {1}@{0}, Exception: \n{2}", topic, text, e);
            }
        }

        internal SubmodelOperationResponse CheckForResponse(string reqId)
        {
            if (_mapReqIdResponse.TryRemove(reqId, out SubmodelOperationResponse resp))
            {
                return resp;
            }
            else
            {
                return null;
            }
        }

        private readonly object unpublishedMessagedLock = new object();

        private readonly static log4net.ILog log = Config.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName);

        private readonly string _mqttClientId = "assets2036net_" + Guid.NewGuid().ToString();

        private readonly string _endpointName;

        private readonly ConcurrentDictionary<string, SubmodelOperationResponse> _mapReqIdResponse;

        private void _mqttClient_ConnectionClosed(object sender, EventArgs e)
        {
        }

        public Task HandleDisconnectedAsync(MqttClientDisconnectedEventArgs eventArgs)
        {
            return Task.Run(() =>
            {
                log.Debug("MQTT connectionClosed!");
                LostConnection?.Invoke();
            });
        }

        public Task HandleApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs eventArgs)
        {
            return Task.Run(() =>
            {
                var topicElements = eventArgs.ApplicationMessage.Topic.Split('/');

                if (topicElements.Length < 4)
                {
                    // non aas conform message?! --> uses MqttConnectivityobserver
                    log.DebugFormat("AssetMgr {0} droped invalid message @ {1}", eventArgs.ApplicationMessage.Topic);
                    return;
                }

                //int offset = Domain != null ? 1 : 0;
                int offset = 1;
                string assetName = topicElements[0 + offset];
                string submodelName = topicElements[1 + offset];
                string elementName = topicElements[2 + offset];

                int topicElementPointer = 2 + offset;

                if (eventArgs.ApplicationMessage.PayloadSegment == null)
                {
                    return;
                }

                if (eventArgs.ApplicationMessage.PayloadSegment.Count <= 0)
                {
                    return;
                }

                string message = System.Text.Encoding.UTF8.GetString(eventArgs.ApplicationMessage.PayloadSegment.Array);
                log.DebugFormat("AssetMgr {0} parsed message {1} @ {2}", assetName, message, eventArgs.ApplicationMessage.Topic);
                Topic topic = new Topic(eventArgs.ApplicationMessage.Topic);

                if (eventArgs.ApplicationMessage.Topic.EndsWith(StringConstants.StringConstant_REQ))
                {
                    var req = JsonSerializer.Deserialize<SubmodelOperationRequest>(message);

                    var assetsBag = _ownerAssets[topic.GetFullAssetName()];

                    foreach (var asset in assetsBag)
                    {
                        if (asset._submodels.TryGetValue(topic.GetSubmodelName(), out Submodel submodel))
                        {
                            // populate req with relevant model information
                            req.Populate(this, asset, submodel);

                            var operation = submodel.Operation(topic.GetElementName());
                            req.Operation = operation;

                            if (operation.Callback != null)
                            {
                                var response = operation.Callback.Invoke(req);
                                response.Publish();
                            }
                        }
                    }
                }
                else if (eventArgs.ApplicationMessage.Topic.EndsWith(StringConstants.StringConstant_RESP))
                {
                    foreach (var asset in _consumerAssets[topic.GetFullAssetName()])
                    {
                        try
                        {
                            var submodel = asset.Submodel(topic.GetSubmodelName());
                            var operation = submodel.Operation(topic.GetElementName());

                            var respObj = JsonSerializer.Deserialize<SubmodelOperationResponse>(message);
                            respObj.Populate(this, asset, submodel);
                            respObj.Operation = operation;

                            if (!_mapReqIdResponse.TryAdd(respObj.RequestId, respObj))
                            {
                                log.ErrorFormat("probably duplicate request id detected: {0}", respObj.RequestId);
                            }
                        }
                        catch (Exception exc)
                        {
                            log.Error(exc);
                            continue;
                        }
                    }
                }
                else // Property or event
                {
                    foreach (var asset in _consumerAssets[topic.GetFullAssetName()])
                    {
                        try
                        {
                            var submodel = asset.Submodel(submodelName);

                            SubmodelProperty property = submodel.Property(elementName);
                            SubmodelEvent submodelEvent = submodel.Event(elementName);

                            if (property != null)
                            {
                                object messageObj = JsonSerializer.Deserialize<object>(message);
                                property.updateLocalValue(messageObj);
                            }
                            else if (submodelEvent != null)
                            {
                                // SubmodelEventMessage emission = JsonConvert.DeserializeObject<SubmodelEventMessage>(message);
                                var emission = JsonSerializer.Deserialize<SubmodelEventMessage>(message);
                                emission.Populate(this, asset, submodel);

                                submodelEvent.EmitEmission(emission);
                            }
                            else
                            {
                                throw new KeyNotFoundException(string.Format("Submodel element {0} could not be found", elementName));
                            }
                        }
                        catch (Exception exc)
                        {
                            log.Error(exc);
                            continue;
                        }
                    }
                }
            });
        }
    }
}
