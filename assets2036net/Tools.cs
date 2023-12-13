// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Packets;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace assets2036net
{
    public static class Tools
    {
        public static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions()
        {
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        }; 

        /// <summary>
        /// Helper method to erase all retained messages produced by a specific asset. 
        /// </summary>
        /// <param name="host">host name of the MQTT broker</param>
        /// <param name="port">port of the MQTT broker (typical: 1883)</param>
        /// <param name="namespace">asset's namespace</param>
        /// <param name="name">asset's name</param>
        public async static Task RemoveAssetTraceAsync(string host, int port, string @namespace, string name)
        {
            var factory = new MqttFactory();
            using (var mqttClient = factory.CreateMqttClient())
            {
                DateTime latest = DateTime.Now;

                var topicsToDelete = new List<string>();

                mqttClient.ApplicationMessageReceivedAsync += (MqttApplicationMessageReceivedEventArgs eventArgs) => 
                {
                    if (eventArgs.ApplicationMessage.Retain)
                    {
                        topicsToDelete.Add(eventArgs.ApplicationMessage.Topic);
                    }

                    return Task.CompletedTask;
                }; 


                mqttClient.ConnectedAsync += (MqttClientConnectedEventArgs evtArgs) => 
                {
                    var topics = new MqttClientSubscribeOptionsBuilder()
                        .WithTopicFilter(new MqttTopicFilter()
                        {
                            Topic = string.Format("{0}/{1}/#", @namespace, name),
                            QualityOfServiceLevel = MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce
                        });

                    return mqttClient.SubscribeAsync(topics.Build(), CancellationToken.None);
                };

                var options = new MqttClientOptionsBuilder()
                    //.WithClientId(_mqttClientId)
                    .WithTcpServer(host, port)
                    .WithCleanSession();

                var taskConnect = await mqttClient.ConnectAsync(options.Build(), CancellationToken.None); 

                await Task.Delay(TimeSpan.FromSeconds(2)); 

                List<Task> tasks = new List<Task>(); 
                foreach (var t in topicsToDelete)
                {
                    var mb = new MqttApplicationMessageBuilder()
                        .WithTopic(t)
                        .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                        .WithPayload(new byte[] { })
                        .WithRetainFlag(); 

                    Console.WriteLine(t); 

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

                client.ApplicationMessageReceivedAsync += (MqttApplicationMessageReceivedEventArgs e) => 
                {
                    if (e.ApplicationMessage.Retain)
                    {
                        tasks.Add(client.PublishAsync(
                            new MqttApplicationMessageBuilder()
                                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.ExactlyOnce)
                                .WithPayload(new byte[] { })
                                .WithRetainFlag().Build(), 
                            CancellationToken.None)); 
                    }

                    return Task.CompletedTask; 
                };

                client.ConnectedAsync += (MqttClientConnectedEventArgs evtArgs) => 
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
                };

                var options = new MqttClientOptionsBuilder()
                    .WithTcpServer(broker, port)
                    .WithCleanSession();

                client.ConnectAsync(options.Build(), CancellationToken.None).Wait();

                Task.WaitAll(tasks.ToArray()); 
            }
        }

        public static Dictionary<string, object> BuildJsonObject(params (string, object)[] properties)
        {
            var result = new Dictionary<string, object>(); 
            
            if (properties != null)
            {
                foreach(var tupel in properties)
                {
                    result.Add(tupel.Item1, tupel.Item2); 
                }
            }

            return result; 
        }

        // private readonly static log4net.ILog log = Config.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName);

    }
}
