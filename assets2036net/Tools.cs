// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using MQTTnet;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Subscribing;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace assets2036net
{
    public static class Tools
    {
        /// <summary>
        /// Helper method to erase all retained messages produced by a specific asset. 
        /// </summary>
        /// <param name="host">host name of the MQTT broker</param>
        /// <param name="port">port of the MQTT broker (typical: 1883)</param>
        /// <param name="namespace">asset's namespace</param>
        /// <param name="name">asset's name</param>
        public static void RemoveAssetTrace(string host, int port, string @namespace, string name)
        {
            var factory = new MqttFactory();
            using (var mqttClient = factory.CreateMqttClient())
            {
                DateTime latest = DateTime.Now;

                var topicsToDelete = new List<string>();

                mqttClient.ApplicationMessageReceivedHandler = new GenericApplicationMessageHandler((MqttApplicationMessageReceivedEventArgs eventArgs) =>
                {
                    if (eventArgs.ApplicationMessage.Retain)
                    {
                        topicsToDelete.Add(eventArgs.ApplicationMessage.Topic);
                    }

                    return Task.CompletedTask;
                }); 


                mqttClient.ConnectedHandler = new GenericClientConnectedHandler()
                {
                    TheHandler = (MqttClientConnectedEventArgs eventArgs) =>
                    {
                        var topics = new MqttClientSubscribeOptionsBuilder()
                            .WithTopicFilter(new MqttTopicFilter()
                            {
                                Topic = string.Format("{0}/{1}/#", @namespace, name),
                                QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce
                            });

                        return mqttClient.SubscribeAsync(topics.Build(), CancellationToken.None);
                    }
                };

                var options = new MqttClientOptionsBuilder()
                    //.WithClientId(_mqttClientId)
                    .WithTcpServer(host, port)
                    .WithCleanSession();

                mqttClient.ConnectAsync(options.Build(), CancellationToken.None).Wait();

                Thread.Sleep(TimeSpan.FromSeconds(1));

                List<Task> tasks = new List<Task>(); 
                foreach (var t in topicsToDelete)
                {
                    var mb = new MqttApplicationMessageBuilder()
                        .WithTopic(t)
                        .WithExactlyOnceQoS()
                        .WithPayload(new byte[] { })
                        .WithRetainFlag(); 

                    tasks.Add(mqttClient.PublishAsync(mb.Build(), CancellationToken.None));
                }

                Task.WaitAll(tasks.ToArray()); 
            }
        }

        /// <summary>
        /// Helper method to clean all! retained messages matching the given root topic 
        /// from the broker (e.g. [mynamespace/myAsset]. Use with care!!! All retained 
        /// message at the topics mynamespace/myAsset/# will be reset. 
        /// </summary>
        /// <param name="broker">hostname of the MQTT broker</param>
        /// <param name="port">port of the MQTT Broker. Typical: 1883</param>
        /// <param name="rootTopic"></param>
        public static void CleanAllRetainedMessages(string broker, int port, string rootTopic)
        {
            var factory = new MqttFactory(); 

            using (var client = factory.CreateMqttClient())
            {
                var tasks = new List<Task>();

                client.ApplicationMessageReceivedHandler = new GenericApplicationMessageHandler((MqttApplicationMessageReceivedEventArgs e) =>
                {
                    if (e.ApplicationMessage.Retain)
                    {
                        tasks.Add(client.PublishAsync(
                            new MqttApplicationMessageBuilder()
                                .WithExactlyOnceQoS()
                                .WithPayload(new byte[] { })
                                .WithRetainFlag().Build(), 
                            CancellationToken.None)); 
                    }

                    return Task.CompletedTask; 
                });

                client.ConnectedHandler = new GenericClientConnectedHandler()
                {
                    TheHandler = (MqttClientConnectedEventArgs eventArgs) =>
                    {
                        return Task.Run(() =>
                        {
                            client.SubscribeAsync(new MqttClientSubscribeOptionsBuilder()
                                .WithTopicFilter(new MqttTopicFilter()
                                {
                                    Topic = rootTopic + "/#"
                                }).Build(),
                                CancellationToken.None);
                        });
                    }
                };

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(broker, port)
                    .WithCleanSession();

                client.ConnectAsync(options.Build(), CancellationToken.None).Wait();

                Task.WaitAll(tasks.ToArray()); 
            }
        }

        ///// <summary>
        ///// Helper method to clean all! retained messages matching the given root topic 
        ///// from the broker (e.g. [mynamespace/myAsset]. Use with care!!! All retained 
        ///// message at the topics mynamespace/myAsset/# will be reset. 
        ///// </summary>
        ///// <param name="broker">hostname of the MQTT broker</param>
        ///// <param name="port">port of the MQTT Broker. Typical: 1883</param>
        ///// <param name="rootTopic"></param>
        //public static void CleanAllRetainedMessages(string broker, int port, string rootTopic)
        //{
        //    _broker = broker;
        //    _port = port;
        //    _rootTopic = rootTopic;

        //    _mqttClient = new MqttClient(broker, port, false, null, null, MqttSslProtocols.None);
        //    _mqttClient.MqttMsgPublishReceived += _mqttClient_MqttMsgPublishReceived;

        //    _mqttClient.Subscribe(new string[] { rootTopic+"/#" }, new byte[] { 2 });
        //    _mqttClient.Connect(Guid.NewGuid().ToString());
        //}



        //private static void _mqttClient_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        //{
        //    if (e.Retain)
        //    {
        //        log.Info("CLEAN: " + e.Topic);
        //        bool sent = false; 
        //        try
        //        {
        //            while (!sent)
        //            {
        //                _mqttClient.Publish(e.Topic, new byte[0], 2, true);
        //                sent = true;
        //            }
        //        }
        //        catch(Exception)
        //        {
        //            _mqttClient.Disconnect();

        //            _mqttClient = new MqttClient(_broker, _port, false, null, null, MqttSslProtocols.None);
        //            _mqttClient.MqttMsgPublishReceived += _mqttClient_MqttMsgPublishReceived;

        //            _mqttClient.Subscribe(new string[] { _rootTopic+"/#" }, new byte[] { 2 });
        //            _mqttClient.Connect(Guid.NewGuid().ToString());
        //        }
        //    }
        //}

        private static log4net.ILog log = Config.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName);

        //private static string _broker;
        //private static int _port;

        //private static string _rootTopic;
    }
}
