// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using Xunit;

namespace assets2036net.unittests
{
    public class StandardConformity : UnitTestBase
    {
        [Fact]
        public void MetaInformationExistance()
        {
            string location = this.GetType().Assembly.Location;
            location = Path.GetDirectoryName(location);
            location = Path.Combine(location, "resources/properties.json");
            Uri uri = new Uri(location);

            string @namespace = Settings.RootTopic;
            string assetName = "MetaInformationExistance"; 
            AssetMgr mgr = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, @namespace, Settings.EndpointName);

            Asset assetOwner = mgr.CreateAsset(Settings.RootTopic, assetName, uri);
            Asset assetConsumer = mgr.CreateFullAssetProxy(Settings.RootTopic, assetName);

            // now there is an asset _endpoint
            Assert.NotNull(mgr.EndpointAsset);

            // the explicitely built asset is no endpoint
            Assert.Null(assetOwner.SubmodelEndpoint);

            Thread.Sleep(TimeSpan.FromMilliseconds(300));

            // get the _meta properties
            Assert.Equal(
                Topic.From(@namespace, Settings.EndpointName),
                assetConsumer.Submodel("properties").MetaPropertyValue.Source.ToString());

            
            var metaValue = assetConsumer.Submodel("properties").MetaPropertyValue; 

            var endpointProxy = mgr.CreateFullAssetProxy(metaValue.Source.Split('/')[0], metaValue.Source.Split('/')[1]); 
            Thread.Sleep(TimeSpan.FromMilliseconds(300));


            Assert.True(endpointProxy.SubmodelEndpoint.Property(StringConstants.PropertyNameOnline).ValueBool);

            mgr.EndpointAsset.Healthy = true;

            // Create asset to read _endpoint submodel: 
            var endpointConsumer = mgr.CreateAssetProxy(
                Settings.RootTopic, 
                Settings.EndpointName, 
                Settings.GetUriToEndpointSubmodel());

            //Thread.Sleep(Settings.WaitTime);

            // healthy and online read from the consuming asset are true now...
            // Assert.True(endpointConsumer.SubmodelEndpoint.Property(StringConstants.PropertyNameOnline).ValueBool);
            // Assert.True(endpointConsumer.SubmodelEndpoint.Property(StringConstants.PropertyNameHealthy).ValueBool);

            // connect to log event... 
            endpointConsumer.SubmodelEndpoint.Event("log").Emission += this.handleLogEvent; 

            _receivedLogMessage = "";
            string msg = "Here comes the logging message. Read it. Or dont. Whatever."; 
            mgr.EndpointAsset.log(msg);

            Assert.True(waitForCondition(() =>
            {
                return msg.Equals(_receivedLogMessage);
            }, Settings.WaitTime));

        }

        private string _receivedLogMessage; 

        void handleLogEvent(SubmodelEventMessage msg)
        {
            _receivedLogMessage = Convert.ToString(msg.Parameters["entry"]); 
        }
    }
}
