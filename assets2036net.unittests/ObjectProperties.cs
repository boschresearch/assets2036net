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
    public class ComplexType
    {
        public string name { get; set; }
        public float x { get; set; }
        public float y { get; set; }
    }

    public class ObjectProperties : UnitTestBase
    {
        [Fact]
        public void WriteAndReadObject()
        {
            string location = this.GetType().Assembly.Location;
            location = Path.GetDirectoryName(location);
            location = Path.Combine(location, "resources/properties.json");
            Uri uri = new Uri(location);


            AssetMgr mgr = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, Settings.RootTopic, Settings.EndpointName);

            Asset assetOwner = mgr.CreateAsset(Settings.RootTopic, Settings.EndpointName, uri);
            Asset assetConsumer = mgr.CreateAssetProxy(Settings.RootTopic, Settings.EndpointName, uri);

            var complexValue = new JObject()
            {
                {"name", "Testname" },
                {"x", "77.77" },
                {"y", "99.99" },
                {"encaps_object", new JObject()
                    {
                        {"name", "TestnameEncaps" },
                        {"x", "55.55" },
                        {"y", "44.44" },
                    }
                }
            };

            assetOwner.Submodel("properties").Property("an_object").Value = complexValue;

            object test = assetConsumer.Submodel("properties").Property("an_object").Value;

            Assert.Equal(
                complexValue,
                assetOwner.Submodel("properties").Property("an_object").Value);

            //Thread.Sleep(Settings.WaitTime);


            Assert.True(waitForCondition(() =>
            {
                if (assetConsumer.Submodel("properties").Property("an_object").Value == null)
                    return false; 

                return complexValue.ToString().Equals(assetConsumer.Submodel("properties").Property("an_object").Value.ToString()); 
            }, Settings.WaitTime));

            //Assert.Equal(
            //    complexValue,
            //    assetConsumer.Submodel("properties").Property("an_object").Value);

            var test2 = assetConsumer.Submodel("properties").Property("an_object").GetValueAs<ComplexType>();
        }
    }
}
