// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using Xunit;

namespace assets2036net.unittests
{
    public class SimpleOperation: UnitTestBase
    {
        private static AssetMgr GetAssetManager(string managerIp, ushort managerPort, string managerNamespace)
        {
            var manager = new AssetMgr(managerIp, managerPort, managerNamespace, string.Empty);
            return manager;
        }

        [Fact]
        public void CallSimpleOperation()
        {
            string location = this.GetType().Assembly.Location;
            location = Path.GetDirectoryName(location);
            location = Path.Combine(location, "resources/simple_operations.json");
            Uri uri = new Uri(location);

            AssetMgr mgr = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, Settings.RootTopic, Settings.EndpointName);

            Asset assetOwner = mgr.CreateAsset(Settings.RootTopic, "CallSimpleOperation", uri);
            Asset assetConsumer = mgr.CreateAssetProxy(Settings.RootTopic, "CallSimpleOperation", uri);

            // bind local operation to asset operation
            assetOwner.Submodel("simple_operations").Operation("do_it").Callback = this.callBackDoIt;

            this.iwascalled = false;

            // call asset operation: 
            assetConsumer.Submodel("simple_operations").Operation("do_it").Invoke(null, TimeSpan.FromSeconds(5));

            // check result: 
            Assert.True(this.iwascalled);
        }

        private bool iwascalled = false;

        private SubmodelOperationResponse callBackDoIt(SubmodelOperationRequest req)
        {
            iwascalled = true;

            var response = req.CreateResponseObj();
            return response;
        }

        [Fact]
        public void CallOperationWithParameters()
        {
            string location = this.GetType().Assembly.Location;
            location = Path.GetDirectoryName(location);
            location = Path.Combine(location, "resources/simple_operations.json");
            Uri uri = new Uri(location);

            AssetMgr mgr = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, Settings.RootTopic, Settings.EndpointName);

            Asset assetOwner = mgr.CreateAsset(Settings.RootTopic, "CallOperationWithParameters", uri);
            Asset assetConsumer = mgr.CreateAssetProxy(Settings.RootTopic, "CallOperationWithParameters", uri);

            // bind local operation to asset operation
            assetOwner.Submodel("simple_operations").Operation("addnumbers").Callback = this.callBackAdd;

            var response = assetConsumer.Submodel("simple_operations").Operation("addnumbers").Invoke(new Dictionary<string, object>()
            {
                {"aaa", 2.5},
                { "bbb", 7.7 }
            },
            TimeSpan.FromSeconds(5));

            // check result: 
            Assert.Equal(
                2.5 + 7.7,
                response.GetReturnValueOrDefault<double>()); 
        }

        private SubmodelOperationResponse callBackAdd(SubmodelOperationRequest req)
        {
            // double num1 = Convert.ToDouble(req.Parameters["aaa"]);
            // double num2 = Convert.ToDouble(req.Parameters["bbb"]);

            double num1 = ((JsonElement)req.Parameters["aaa"]).GetDouble(); 
            double num2 = ((JsonElement)req.Parameters["bbb"]).GetDouble(); 

            var response = req.CreateResponseObj();
            response.Value = num1 + num2;

            return response;
        }



    }
}
