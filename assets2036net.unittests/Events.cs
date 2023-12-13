// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using Xunit;

namespace assets2036net.unittests
{
    public class Events: UnitTestBase
    {
        [Fact]
        public void EventWithComplexParameters()
        {
            string location = this.GetType().Assembly.Location;
            location = Path.GetDirectoryName(location);
            location = Path.Combine(location, "resources/events.json");
            Uri uri = new Uri(location);

            using AssetMgr mgrOwner = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, Settings.Namespace, Settings.EndpointName);
            using AssetMgr mgrConsumer = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, Settings.Namespace, Settings.EndpointName);

            using Asset assetOwner = mgrOwner.CreateAsset(Settings.Namespace, "EventWithComplexParameters", uri);
            using Asset assetConsumer = mgrConsumer.CreateAssetProxy(Settings.Namespace, "EventWithComplexParameters", uri);

            // bind local eventlistener to event
            assetConsumer.Submodel("events").Event("anevent").Emission += this.handleEvent; 

            this._a = null;
            this._b = null;
            this._c = null;

            object a = "Parameter_a";
            object b = "Parameter_b";
            object c = new Dictionary<string, object>
            {
                {"eins", 1 },
                {"zwei", 2 }, 
                {"drei", 3 }, 
                {"vier", "4" }
            };

            Thread.Sleep(1000);

            assetOwner.Submodel("events").Event("anevent").Emit(new Dictionary<string, object>()
            {
                {"aaa", a },
                {"bbb", b },
                {"objectparameter", c }
            });

            //Thread.Sleep(Settings.WaitTime);

            Assert.True(waitForCondition(() =>
            {
                return a.Equals(this._a);
            }, Settings.WaitTime));

            Assert.True(waitForCondition(() =>
            {
                return b.Equals(this._b);
            }, Settings.WaitTime));

            Assert.True(waitForCondition(() =>
            {
                if (this._c == null)
                {
                    return false; 
                }

                return c.ToString().Equals(this._c.ToString());
            }, Settings.WaitTime));

        }

        object _a = null;
        object _b = null;
        object _c = null;

        private void handleEvent(SubmodelEventMessage emission)
        {
            this._a = ((JsonElement)emission.Parameters["aaa"]).GetString();
            this._b = ((JsonElement)emission.Parameters["bbb"]).GetString();
            this._c = ((JsonElement)emission.Parameters["objectparameter"]).Deserialize<Dictionary<string, object>>();
        }
    }
}
