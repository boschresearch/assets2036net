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

            var complexValue = new Dictionary<string, object>()
            {
                {"name", "Testname" },
                {"x", 77.77},
                {"y", 99.99},
                {"encaps_object", new Dictionary<string, object>()
                    {
                        {"name", "TestnameEncaps" },
                        {"x", 55.55},
                        {"y", 44.44},
                    }
                }
            };

            assetOwner.Submodel("properties").Property("an_object").Value = complexValue;


            Assert.Throws<Exception>(() => assetOwner.Submodel("properties").Property("an_object").Value); 

            //Thread.Sleep(Settings.WaitTime);


            Assert.True(waitForCondition(() =>
            {
                if (assetConsumer.Submodel("properties").Property("an_object").Value == null)
                    return false; 
                Thread.Sleep(300); 

                // Console.WriteLine(
                //     complexValue.ToString()); 
                // Console.WriteLine(
                //     assetConsumer.Submodel("properties").Property("an_object").GetValueAs<Dictionary<string, object>>().ToString()); 

                return complexValue.ToString().Equals(
                    assetConsumer.Submodel("properties").Property("an_object").ValueAs<Dictionary<string, object>>().ToString()); 

            }, Settings.WaitTime));

            //Assert.Equal(
            //    complexValue,
            //    assetConsumer.Submodel("properties").Property("an_object").Value);

            var test2 = assetConsumer.Submodel("properties").Property("an_object").ValueAs<ComplexType>();

        }
    }
}
