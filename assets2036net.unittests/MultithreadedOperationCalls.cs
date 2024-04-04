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

            using AssetMgr mgrOwner = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, Settings.Namespace, Settings.EndpointName);
            using Asset assetOwner = mgrOwner.CreateAsset(Settings.Namespace, "ConcurrentCustomers", uri);

            // bind local operation to asset operation
            assetOwner.Submodel("math").Operation("square").Callback = this.square;
            assetOwner.Submodel("math").Operation("sqrt").Callback = this.sqrt;
            assetOwner.Submodel("math").Operation("sin").Callback = this.sin;

            using AssetMgr mgrConsumer = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, Settings.Namespace, Settings.EndpointName);

            using Asset assetConsumer1 = mgrConsumer.CreateAssetProxy(Settings.Namespace, "ConcurrentCustomers", uri);
            //Asset assetConsumer2 = mgrConsumer.CreateAsset("ConcurrentCustomers", Mode.Consumer, uri);
            //Asset assetConsumer3 = mgrConsumer.CreateAsset("ConcurrentCustomers", Mode.Consumer, uri);

            int numberCalls = 20;

            var t1 = new Thread(() =>
            {
                for (int i = 0; i < numberCalls; ++i)
                {
                    var prod = assetConsumer1.Submodel("math").Operation("square").Invoke(new Dictionary<string, object>()
                        {
                            {"x", i }
                        }, 
                        Settings.WaitTime); 

                    Assert.Equal(
                        (double)i * i,
                        prod.GetReturnValueOrDefault<double>()); 

                    //Thread.Sleep(new Random().Next(0, 5));
                }
            });
            t1.Start();

            var t21 = new Thread(() =>
            {
                for (int i = 0; i < numberCalls; ++i)
                {
                    var sqrt = assetConsumer1.Submodel("math").Operation("sqrt").Invoke(new Dictionary<string, object>()
                        {
                            {"x", i }
                        }, 
                        Settings.WaitTime).GetReturnValueOrDefault<double>(); 

                    Assert.Equal(
                        (double)Math.Sqrt(i),
                        sqrt); 

                    //Thread.Sleep(new Random().Next(0, 5));
                }
            });
            t21.Start();

            var t22 = new Thread(() =>
            {
                for (int i = numberCalls; i > 0; --i)
                {
                    var sqrt = assetConsumer1.Submodel("math").Operation("sqrt").Invoke(new Dictionary<string, object>()
                        {
                            {"x", i }
                        }, 
                        Settings.WaitTime).GetReturnValueOrDefault<double>(); 

                    Assert.Equal(
                        (double)Math.Sqrt(i),
                        sqrt); 


                    // Thread.Sleep(new Random().Next(0, 5));
                }
            });
            t22.Start();

            for (int i = numberCalls; i >= 1; --i)
            {
                    var sin = assetConsumer1.Submodel("math").Operation("sin").Invoke(new Dictionary<string, object>()
                        {
                            {"x", i }
                        }, 
                        Settings.WaitTime).GetReturnValueOrDefault<double>(); 

                    Assert.Equal(
                        (double)Math.Sin(i),
                        sin); 


                // Thread.Sleep(new Random().Next(0, 5));
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
