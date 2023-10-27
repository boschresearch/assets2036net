// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace assets2036net
{
    /// <summary>
    /// Represents an operation in a submodel. Used for serialization to and from json and
    /// <list type="bullet">
    ///     <item>in the submodel provider implementation you have to provide a callback of type Func<SubmodelOperationRequest, SubmodelOperationResponse>
    ///     for the actual implementation of the operation, </item>
    ///     <item>while on the consumer side you will use the <seealso cref="Invoke(Dictionary{string, object}, int)"/>, 
    ///     <seealso cref="StartInvoke(Dictionary{string, object}, Action{object}, Action, int)"/> or 
    ///     <seealso cref="StartInvoke(Dictionary{string, object}, int)"/> methods to call the operations of a 
    ///     remote asset via its proxy. </item> 
    /// </list>
    /// </summary>
    public class SubmodelOperation : SubmodelElement
    {
        /// <summary>
        /// The operation's parameters
        /// </summary>
        [JsonPropertyName("parameters")]
        public Dictionary<string, Parameter> Parameters
        {
            get
            {
                return _parameters;
            }
            set
            {
                _parameters = value;
                foreach (var kvp in _parameters)
                {
                    kvp.Value.Name = kvp.Key;
                }
            }
        }

        /// <summary>
        /// When working with an asset proxy, use Invoke to synchronously call the operation implemented
        /// remotely. The return value is the return value of the remote call or null, if there was no return 
        /// value. 
        /// </summary>
        /// <param name="parameters">The parameters for the remote call</param>
        /// timespan, TimeoutException is thrown</param>
        /// <returns>the return value of the asset submodel operation, if there is one, else null.</returns>
        public SubmodelOperationResponse Invoke(Dictionary<string, object> parameters)
        {
            return Invoke(parameters, TimeSpan.FromSeconds(5));
        }


        /// <summary>
        /// When working with an asset proxy, use Invoke to synchronously call the operation implemented
        /// remotely. The return value is the return value of the remote call or null, if there was no return 
        /// value. 
        /// </summary>
        /// <param name="parameters">The parameters for the remote call</param>
        /// <param name="timeoutMs">A timeout in ms. If the remote asset doesn't answer within this 
        /// timespan, TimeoutException is thrown</param>
        /// <returns>the return value of the asset submodel operation, if there is one, else null.</returns>
        public SubmodelOperationResponse Invoke(Dictionary<string, object> parameters, TimeSpan timeout)
        {
            try
            {
                _mutex.WaitOne();

                if (Asset.Mode == Mode.Consumer)
                {
                    SubmodelOperationRequest req = new SubmodelOperationRequest(this);
                    req.Populate(AssetMgr, Asset, Submodel);

                    var reqId = Guid.NewGuid().ToString();
                    req.RequestId = reqId.ToString();

                    req.Parameters = parameters;

                    DateTime started = DateTime.Now;

                    req.Publish();

                    TimeSpan timeGone = TimeSpan.MinValue;
                    do
                    {
                        Thread.Sleep(10);

                        // check for an answer
                        SubmodelOperationResponse resp = AssetMgr.CheckForResponse(reqId);

                        if (resp != null)
                        {
                            log.DebugFormat("{0}.{1} received response {2} on request {3}", Asset.Name, Name, resp.Value, resp.RequestId);
                            return resp; 
                        }

                        timeGone = DateTime.Now - started;
                    }
                    while (timeGone.TotalMilliseconds < timeout.TotalMilliseconds);

                    log.ErrorFormat("{0}.{1} timeout on request {2}", Asset.Name, Name, req.RequestId);

                    throw new TimeoutException("The remote asset did not answer within the given timeout span");
                }
                else
                {
                    throw new Exception("Invoke can only be called when asset in Consumer Mode!");
                }

            }
            finally
            {
                _mutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// When working with an asset proxy, use StartInvoke to asynchronously call the operation implemented
        /// remotely. The typed, returned task object represents the running operation and contains its return 
        /// value, when operation call has finished. 
        /// </summary>
        /// <param name="parameters">The parameters for the remote call</param>
        /// <param name="timeoutMs">A timeout in ms. If the remote asset doesn't answer within this 
        /// timespan, TimeoutException is thrown</param>
        /// <returns>The task object representing the remote call, which will give you the return value
        /// if available</returns>
        public async Task<SubmodelOperationResponse> InvokeAsync(
            Dictionary<string, object> parameters,
            TimeSpan timeout)
        {
            var t = Task.Run(() =>
            {
                return Invoke(parameters, timeout);
            });

            return await t;
        }

        /// <summary>
        /// When working with an asset proxy, use StartInvoke to asynchronously call the operation implemented
        /// remotely. The typed, returned task object represents the running operation. With the parameters 
        /// inCaseOfSuccess and inCaseOfFailure you can define handlers for each conceivable outcome. 
        /// </summary>
        /// <param name="parameters">The parameters for the remote call</param>
        /// <param name="inCaseOfSuccess">Action to be called when operation finishes successfully. 
        /// Return value will be given as parameter</param>
        /// <param name="inCaseOfFailure">Action to be called in case of failure.</param>
        /// <param name="timeout">A timeout in ms. If the remote asset doesn't answer within this 
        /// timespan, TimeoutException is thrown</param>
        /// <returns>The task object representing the remote call, which will give you the return value
        /// if available</returns>
        public Task StartInvoke(
            Dictionary<string, object> parameters,
            Action<SubmodelOperationResponse> inCaseOfSuccess,
            Action inCaseOfFailure,
            TimeSpan timeout)
        {
            return Task.Run(() =>
            {
                try
                {
                    var result = Invoke(parameters, timeout);
                    log.Debug("StartInvoke succeeded.");

                    if (inCaseOfSuccess != null)
                    {
                        try
                        {
                            inCaseOfSuccess.Invoke(result);
                        }
                        catch (System.Exception ex)
                        {
                            log.Error($"action inCaseOfSuccess threw exception: {ex}"); 
                        }
                    }
                }
                catch (Exception e)
                {
                    log.Error(e);
                    if (inCaseOfFailure != null)
                    {
                        try
                        {
                            inCaseOfFailure.Invoke();
                        }
                        catch (Exception ex)
                        {
                            log.Error($"action inCaseOfFailure threw exception: {ex}"); 
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Currently, the operation call on the client side is synchronized, so that only one operation 
        /// call at a time is allowed. Use this method to check, if there is currently a remote call running. 
        /// </summary>
        /// <returns>true, if currently there is a remote call running</returns>
        public bool InvocationRunning()
        {
            if (!_mutex.WaitOne(0))
            {
                return true;
            }
            else
            {
                _mutex.ReleaseMutex();
                return false;
            }
        }

        /// <summary>
        /// When implementing the submodel provider, set Callback to define the method which will be called, 
        /// when somebody calls thus submodel operation remotely. 
        /// </summary>
        [JsonIgnore]
        public Func<SubmodelOperationRequest, SubmodelOperationResponse> Callback { get; set; }

        private readonly static log4net.ILog log = Config.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName);

        private readonly Mutex _mutex = new Mutex();

        private Dictionary<string, Parameter> _parameters;
        //internal override void createSubscriptions(MqttClient mqttClient, Mode mode)
        //{
        //    if (mode == Mode.Consumer)
        //    {
        //        var topic = SubmodelElement.buildTopic(Topic, StringConstants.StringConstant_RESP);
        //        log.InfoFormat("{0} subscribes to {1}", Name, topic);
        //        mqttClient.Subscribe(
        //            new string[] { topic },
        //            new byte[] { uPLibrary.Networking.M2Mqtt.Messages.MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        //    }
        //    else
        //    {
        //        var topic = SubmodelElement.buildTopic(Topic, StringConstants.StringConstant_REQ);
        //        log.InfoFormat("{0} subscribes to {1}", Name, topic);
        //        mqttClient.Subscribe(
        //            new string[] { topic },
        //            new byte[] { uPLibrary.Networking.M2Mqtt.Messages.MqttMsgBase.QOS_LEVEL_EXACTLY_ONCE });
        //    }
        //}

        internal override ISet<string> getSubscriptions(Mode mode)
        {
            var subscriptions = new HashSet<string>();

            var topic = "";

            if (mode == Mode.Consumer)
            {
                subscriptions.Add(SubmodelElement.BuildTopic(Topic, StringConstants.StringConstant_RESP));
                log.InfoFormat("{0} subscribes to {1}", Name, topic);
            }
            else
            {
                subscriptions.Add(SubmodelElement.BuildTopic(Topic, StringConstants.StringConstant_REQ));
                log.InfoFormat("{0} subscribes to {1}", Name, topic);
            }

            return subscriptions;
        }
    }
}
