// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using log4net;
using log4net.Repository;
using System;
using System.IO;
using System.Threading;
using Xunit;

namespace assets2036net.unittests
{
    public class SimpleProperty : UnitTestBase
    {
        [Fact]
        public void WriteAndReadStringProperty()
        {
            string location = this.GetType().Assembly.Location;
            location = Path.GetDirectoryName(location);
            location = Path.Combine(location, "resources/properties.json");
            Uri uri = new Uri(location);


            AssetMgr mgr = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, Settings.RootTopic, Settings.EndpointName);

            Asset assetOwner = mgr.CreateAsset(Settings.RootTopic, "WriteAndReadStringProperty", uri);

            string newValue = "SomeSmartPoseValue";
            assetOwner.Submodel("properties").Property("pose").Value = newValue;

            Thread.Sleep(1000); 

            Asset assetConsumer = mgr.CreateAssetProxy(Settings.RootTopic, "WriteAndReadStringProperty", uri);

            Assert.True(waitForCondition(() =>
            {
                return newValue == assetConsumer.Submodel("properties").Property("pose").ValueString;
            }, Settings.WaitTime));
        }


        [Fact]
        public void WriteAndReadIntProperty()
        {
            string location = this.GetType().Assembly.Location;
            location = Path.GetDirectoryName(location);
            location = Path.Combine(location, "resources/properties.json");
            Uri uri = new Uri(location);


            AssetMgr mgr = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, Settings.RootTopic, Settings.EndpointName);

            Asset assetOwner = mgr.CreateAsset(Settings.RootTopic, "WriteAndReadIntProperty", uri);
            Asset assetConsumer = mgr.CreateAssetProxy(Settings.RootTopic, "WriteAndReadIntProperty", uri);

            int newIntValue = new Random().Next(1, 9999);

            assetOwner.Submodel("properties").Property("age").Value = newIntValue;

            Assert.True(waitForCondition(() =>
            {
                return newIntValue == assetConsumer.Submodel("properties").Property("age").ValueInt;
            }, Settings.WaitTime));
        }

        [Fact]
        public void WriteAndReadFloatProperty()
        {
            string location = this.GetType().Assembly.Location;
            location = Path.GetDirectoryName(location);
            location = Path.Combine(location, "resources/properties.json");
            Uri uri = new Uri(location);


            AssetMgr mgr = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, Settings.RootTopic, Settings.EndpointName);

            Asset assetOwner = mgr.CreateAsset(Settings.RootTopic, "WriteAndReadFloatProperty", uri);
            Asset assetConsumer = mgr.CreateAssetProxy(Settings.RootTopic, "WriteAndReadFloatProperty", uri);

            double newFloatValue = Math.PI;

            assetOwner.Submodel("properties").Property("mathpi").Value = newFloatValue;

            //Assert.Equal(
            //    newFloatValue,
            //    assetOwner.GetSubmodel("Properties").GetProperty("pi").ValueDouble);

            Assert.True(waitForCondition(() =>
            {
                return newFloatValue == assetConsumer.Submodel("properties").Property("mathpi").ValueDouble;
            }, Settings.WaitTime)); 
        }

        [Fact]
        public void WriteAndReadBoolProperty()
        {
            string location = this.GetType().Assembly.Location;
            location = Path.GetDirectoryName(location);
            location = Path.Combine(location, "resources/properties.json");
            Uri uri = new Uri(location);

            AssetMgr mgr = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, Settings.RootTopic, Settings.EndpointName);

            Asset assetOwner = mgr.CreateAsset(Settings.RootTopic, "WriteAndReadBoolProperty", uri);
            Asset assetConsumer = mgr.CreateAssetProxy(Settings.RootTopic, "WriteAndReadBoolProperty", uri);


            assetOwner.Submodel("properties").Property("truth").Value = true;

            Assert.True(waitForCondition(() =>
            {
                return assetConsumer.Submodel("properties").Property("truth").ValueBool;
            }, Settings.WaitTime));
        }

        [Fact]
        public void RemoveRetainedProperties()
        {
            string location = this.GetType().Assembly.Location;
            location = Path.GetDirectoryName(location);
            location = Path.Combine(location, "resources/properties.json");
            Uri uri = new Uri(location);

            AssetMgr mgr = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, Settings.RootTopic, Settings.EndpointName);

            Asset assetOwner = mgr.CreateAsset(Settings.RootTopic, "WriteAndReadBoolProperty", uri);
            Asset assetConsumer = mgr.CreateAssetProxy(Settings.RootTopic, "WriteAndReadBoolProperty", uri);


            assetOwner.Submodel("properties").Property("truth").Value = true;

            Assert.True(waitForCondition(() =>
            {
                return assetConsumer.Submodel("properties").Property("truth").ValueBool;
            }, Settings.WaitTime));

            assetOwner.Submodel("properties").Property("truth").Value = null;

            Assert.True(waitForCondition(() =>
            {
                return assetConsumer.Submodel("properties").Property("truth").Value == null;
            }, Settings.WaitTime));


            assetOwner.RemoveAllSubmodelsPropertiesFromBroker(); 
        }
    }
}


