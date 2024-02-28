// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using Xunit;

namespace assets2036net.unittests
{
    public class MultithreadedOperationCalls : UnitTestBase, IDisposable
    {
        public MultithreadedOperationCalls()
        {
            log.Info("LOGGINGAUSGABE!"); 
        }

        public void Dispose()
        {
        }

        [Fact]
        public void ConcurrentCustomers()
        {
            string location = this.GetType().Assembly.Location;
            location = Path.GetDirectoryName(location);
            location = Path.Combine(location, "resources/math.json");
            Uri uri = new Uri(location);

            AssetMgr mgrOwner = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, Settings.RootTopic, Settings.EndpointName);
            Asset assetOwner = mgrOwner.CreateAsset(Settings.RootTopic, "ConcurrentCustomers", uri);

            // bind local operation to asset operation
            assetOwner.Submodel("math").Operation("square").Callback = this.square;
            assetOwner.Submodel("math").Operation("sqrt").Callback = this.sqrt;
            assetOwner.Submodel("math").Operation("sin").Callback = this.sin;

            AssetMgr mgrConsumer = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, Settings.RootTopic, Settings.EndpointName);

            Asset assetConsumer1 = mgrConsumer.CreateAssetProxy(Settings.RootTopic, "ConcurrentCustomers", uri);
            //Asset assetConsumer2 = mgrConsumer.CreateAsset("ConcurrentCustomers", Mode.Consumer, uri);
            //Asset assetConsumer3 = mgrConsumer.CreateAsset("ConcurrentCustomers", Mode.Consumer, uri);

            int numberCalls = 20;

            var t1 = new Thread(() =>
            {
                for (int i = 0; i < numberCalls; ++i)
                {
                    Assert.Equal(
                        (double)i * i,
                        ((JsonElement)assetConsumer1.Submodel("math").Operation("square").Invoke(new Dictionary<string, object>()
                        {
                            {"x", i }
                        }, 
                        Settings.WaitTime)).GetDouble());

                    //Thread.Sleep(new Random().Next(0, 5));
                }
            });
            t1.Start();

            var t21 = new Thread(() =>
            {
                for (int i = 0; i < numberCalls; ++i)
                {
                    Assert.Equal(
                        (double)Math.Sqrt(i),
                        ((JsonElement)assetConsumer1.Submodel("math").Operation("sqrt").Invoke(new Dictionary<string, object>()
                        {
                            {"x", i }
                        }, 
                        Settings.WaitTime)).GetDouble());

                    //Thread.Sleep(new Random().Next(0, 5));
                }
            });
            t21.Start();

            var t22 = new Thread(() =>
            {
                for (int i = numberCalls; i > 0; --i)
                {
                    Assert.Equal(
                        (double)Math.Sqrt(i),
                        ((JsonElement)assetConsumer1.Submodel("math").Operation("sqrt").Invoke(new Dictionary<string, object>()
                            {
                                {"x", i }
                            }, 
                            Settings.WaitTime)).GetDouble());

                    Thread.Sleep(new Random().Next(0, 5));
                }
            });
            t22.Start();

            for (int i = numberCalls; i >= 1; --i)
            {
                Assert.Equal(
                    Math.Sin(i),
                    ((JsonElement)assetConsumer1.Submodel("math").Operation("sin").Invoke(new Dictionary<string, object>()
                    {
                        {"x", i }
                    }, 
                    Settings.WaitTime)).GetDouble());

                Thread.Sleep(new Random().Next(0, 5));
            }

            t1.Join();
            t21.Join();
            t22.Join();

            // Assert.True(mgrOwner.Wait());
            // Assert.True(mgrConsumer.Wait());
        }

        private SubmodelOperationResponse square(SubmodelOperationRequest req)
        {
            double a = ((JsonElement)(req.Parameters["x"])).GetDouble();
            var resp = req.CreateResponseObj();
            resp.Value = a * a;

            return resp; 
        }
        private SubmodelOperationResponse sqrt(SubmodelOperationRequest req)
        {
            double a = ((JsonElement)(req.Parameters["x"])).GetDouble();
            var resp = req.CreateResponseObj();
            resp.Value = Math.Sqrt(a);

            return resp;
        }
        private SubmodelOperationResponse sin(SubmodelOperationRequest req)
        {
            double a = ((JsonElement)(req.Parameters["x"])).GetDouble();
            var resp = req.CreateResponseObj();
            resp.Value = Math.Sin(a);

            return resp;
        }
    }
}
