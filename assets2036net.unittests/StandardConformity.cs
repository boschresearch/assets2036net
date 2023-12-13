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
using System.Threading.Tasks;
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

            string @namespace = Settings.Namespace;
            string assetName = "MetaInformationExistance"; 

            string endpointname = Settings.EndpointName+"_metatest"; 

            using AssetMgr mgr = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, @namespace, endpointname);

            using Asset assetOwner = mgr.CreateAsset(Settings.Namespace, assetName, uri);
            using Asset assetConsumer = mgr.CreateFullAssetProxy(Settings.Namespace, assetName);

            // now there is an asset _endpoint
            Assert.NotNull(mgr.EndpointAsset);

            // the explicitely built asset is no endpoint
            Assert.Null(assetOwner.SubmodelEndpoint);

            Thread.Sleep(TimeSpan.FromMilliseconds(300));

            // get the _meta properties
            Assert.Equal(
                Topic.From(@namespace, endpointname),
                assetConsumer.Submodel("properties").MetaPropertyValue.Source.ToString());

            
            var metaValue = assetConsumer.Submodel("properties").MetaPropertyValue; 

            using var endpointProxy = mgr.CreateFullAssetProxy(metaValue.Source.Split('/')[0], metaValue.Source.Split('/')[1]); 
            Thread.Sleep(TimeSpan.FromMilliseconds(300));


            Assert.True(endpointProxy.SubmodelEndpoint.Property(StringConstants.PropertyNameOnline).ValueBool);

            mgr.EndpointAsset.Healthy = true;

            // Create asset to read _endpoint submodel: 
            using var endpointConsumer = mgr.CreateAssetProxy(
                Settings.Namespace, 
                endpointname, 
                Settings.GetUriToEndpointSubmodel());

            //Thread.Sleep(Settings.WaitTime);

            // healthy and online read from the consuming asset are true now...
            // Assert.True(endpointConsumer.SubmodelEndpoint.Property(StringConstants.PropertyNameOnline).ValueBool);
            // Assert.True(endpointConsumer.SubmodelEndpoint.Property(StringConstants.PropertyNameHealthy).ValueBool);

            _receivedLogMessage = "";
            
            // connect to log event... 
            endpointConsumer.SubmodelEndpoint.Event("log").Emission += (SubmodelEventMessage msg) => 
            {
                _receivedLogMessage = msg.GetParameterString("entry"); 
            }; 

            string msg = "Here comes the logging message. Read it. Or dont. Whatever."; 
            mgr.EndpointAsset.log(msg);

            Task.Delay(10000); 

            Assert.True(waitForCondition(() =>
            {
                return msg.Equals(_receivedLogMessage);
            }, Settings.WaitTime));

        }

        private string _receivedLogMessage; 
    }
}
