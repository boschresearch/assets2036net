// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using MQTTnet;
using MQTTnet.Client;
using System;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace assets2036net.unittests
{
    public class AssetMgrQueries : UnitTestBase
    {
        private new readonly ITestOutputHelper log;

        public AssetMgrQueries(ITestOutputHelper outp)
        {
            log = outp;
        }

        [Fact]
        public void GetAvailableAssetNames()
        {
            var mgr = new assets2036net.AssetMgr(Settings.BrokerHost, Settings.BrokerPort, "arena2036", "Assets2036Aasx");

            var names = mgr.GetAvailableAssetNames();

            foreach (var tuple in names)
            {
                log.WriteLine(tuple.Item1, tuple.Item2);
            }
        }

        [Fact]
        public void GetSupportedSubmodels()
        {
            var mgr = new assets2036net.AssetMgr(Settings.BrokerHost, Settings.BrokerPort, "arena2036", "Assets2036Aasx");

            var submodels = mgr.GetSupportedSubmodels("arena2036", "SimCISS");

            foreach (var sm in submodels)
            {
                log.WriteLine(string.Format("{0} from {1}", sm.Name, sm.SubmodelUrl));
            }
        }

        // [Fact]
        // public async void RemoveAssetTrace()
        // {
        //     var assetName = "WillBeDeleted";
        //     var @namespace = "assets2036net";

        //     var mgr = new assets2036net.AssetMgr(Settings.BrokerHost, Settings.BrokerPort, @namespace, assetName);

        //     try
        //     {
        //         var submodelUrl = "https://raw.githubusercontent.com/boschresearch/assets2036-submodels/master/testmodel.json";
        //         var asset = mgr.CreateAsset(@namespace, assetName, new Uri(submodelUrl));

        //         var proxy = mgr.CreateFullAssetProxy(@namespace, assetName);

        //         double value = 1.99;

        //         asset.Submodel("testmodel").Property("number").Value = value;
        //         Thread.Sleep(1000);
        //         Assert.Equal(value, proxy.Submodel("testmodel").Property("number").ValueDouble);
        //     }
        //     finally
        //     {
        //         // mgr.Dispose();
        //     }

        //     await Tools.RemoveAssetTraceAsync(Settings.BrokerHost, Settings.BrokerPort, @namespace, assetName);

        //     mgr = new assets2036net.AssetMgr(Settings.BrokerHost, Settings.BrokerPort, @namespace, assetName);

        //     var factory = new MqttFactory();
        //     using (var mqttClient = factory.CreateMqttClient())
        //     {
        //         mqttClient.ApplicationMessageReceivedAsync += (MqttApplicationMessageReceivedEventArgs eventArgs) => 
        //         {
        //             throw new Exception(string.Format("Received unexpected message on topic {0}", eventArgs.ApplicationMessage.Topic));
        //         }; 

        //         var options = new MqttClientOptionsBuilder()
        //             .WithTcpServer(Settings.BrokerHost, Settings.BrokerPort)
        //             .WithCleanSession();

        //         mqttClient.ConnectAsync(options.Build(), CancellationToken.None).Wait();
        //     }
        // }
    }
}
