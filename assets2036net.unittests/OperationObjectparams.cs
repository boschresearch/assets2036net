// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using assets2036net;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using Xunit;

namespace assets2036net.unittests
{
    public class OperationObjectParams : UnitTestBase
    {
        [Fact]
        public void OperationWithObjectType()
        {
            string location = this.GetType().Assembly.Location;
            location = Path.GetDirectoryName(location);
            location = Path.Combine(location, "resources/object_operation.json");
            Uri uri = new Uri(location);

            AssetMgr mgrOwner = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, Settings.RootTopic, Settings.EndpointName);
            AssetMgr mgrConsumer = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, Settings.RootTopic, Settings.EndpointName);

            Asset assetOwner = mgrOwner.CreateAsset(Settings.RootTopic, "OperationWithObjectType", uri);
            Asset assetConsumer = mgrConsumer.CreateAssetProxy(Settings.RootTopic, "OperationWithObjectType", uri);

            // bind local operation to asset operation
            assetOwner.Submodel("object_operation").Operation("splitstring").Callback = this.callBackSplitString;

            var response = assetConsumer.Submodel("object_operation").Operation("splitstring").Invoke(
                new Dictionary<string, object>()
                {
                    {"aaa", "A_bis_Z" }
                },
                TimeSpan.FromSeconds(5));

            // check result: 

            Assert.Equal(
                "A",
                response.GetReturnValueOrDefault<Dictionary<string, string>>()["first_letter"]); 

            Assert.Equal(
                "Z",
                response.GetReturnValueOrDefault<Dictionary<string, string>>()["last_letter"]); 
        }

        private SubmodelOperationResponse callBackSplitString(SubmodelOperationRequest req)
        {
            string param = ((JsonElement)req.Parameters["aaa"]).GetString();
            var result = new Dictionary<string, object>()
            {
                {"first_letter", param.Substring(0,1)},
                {"last_letter", param.Substring(param.Length-1, 1)}
            };

            var response = req.CreateResponseObj();
            response.Value = result;

            return response;
        }


        [Fact]
        public void OperationWithObjectParameter()
        {
            string location = this.GetType().Assembly.Location;
            location = Path.GetDirectoryName(location);
            location = Path.Combine(location, "resources/object_operation.json");
            Uri uri = new Uri(location);

            AssetMgr mgr = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, Settings.RootTopic, Settings.EndpointName);

            Asset assetOwner = mgr.CreateAsset(Settings.RootTopic, "OperationWithObjectParameter", uri);
            Asset assetConsumer = mgr.CreateAssetProxy(Settings.RootTopic, "OperationWithObjectParameter", uri);

            // bind local operation to asset operation
            assetOwner.Submodel("object_operation").Operation("getfirstparam").Callback = (SubmodelOperationRequest req) =>
            {
                var person = req.GetParameterAs<Person>("papa"); 

                var response = req.CreateResponseObj();
                response.Value = person.age + person.kids[0].age;
                return response;
            }; 

            int testValue1 = 99; 
            int testValue2 = 199; 

            var response = assetConsumer.Submodel("object_operation").Operation("getfirstparam").Invoke(
                new Dictionary<string, object>
                {
                    {
                        "papa", 
                        new Person
                        {
                            age = testValue1, 
                            name = "Thomas", 
                            kids = new List<Person>
                            {
                                new ()
                                {
                                    age = testValue2, name = "Karla"
                                }
                            }
                        }
                    }
                },
                TimeSpan.FromSeconds(5));

            // check result: 
            int res = response.GetReturnValueOrDefault<int>(); 

            Assert.Equal(
                testValue1+testValue2,
                res);
        }

        [Fact]
        public void OperationWithObjectResponse()
        {
            string location = this.GetType().Assembly.Location;
            location = Path.GetDirectoryName(location);
            location = Path.Combine(location, "resources/object_operation.json");
            Uri uri = new Uri(location);

            AssetMgr mgr = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, Settings.RootTopic, Settings.EndpointName);

            Asset assetOwner = mgr.CreateAsset(Settings.RootTopic, "OperationWithObjectParameter", uri);
            Asset assetConsumer = mgr.CreateAssetProxy(Settings.RootTopic, "OperationWithObjectParameter", uri);

            // bind local operation to asset operation
            assetOwner.Submodel("object_operation").Operation("objectoperation").Callback = (SubmodelOperationRequest req) =>
            {
                var resp = req.CreateResponseObj();

                var papa = new Person()
                {
                    age = 34,
                    name = "Hans Meier",
                    kids = new List<Person>()
                    {
                        new Person()
                        {
                            age = 3,
                            name = "Johanna"
                        },
                        new Person()
                        {
                            age = 5,
                            name = "Max"
                        }
                    }
                };

                resp.Value = papa;

                return resp;
            }; 

            var response = assetConsumer.Submodel("object_operation").Operation("objectoperation").Invoke(
                new Dictionary<string, object>(), 
                TimeSpan.FromSeconds(5));

            var papa = response.GetReturnValueOrDefault<Person>(); 

            // // check result: 
            // JObject joPapa = response as JObject;
            // Person papa = joPapa.ToObject<Person>(); 

            Assert.Equal(
                2, 
                papa.kids.Count);
        }

        [Fact]
        public void OperationWithArrayParameter()
        {
            string location = this.GetType().Assembly.Location;
            location = Path.GetDirectoryName(location);
            location = Path.Combine(location, "resources/object_operation.json");
            Uri uri = new Uri(location);

            AssetMgr mgr = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, Settings.RootTopic, Settings.EndpointName);

            Asset assetOwner = mgr.CreateAsset(Settings.RootTopic, "OperationWithArrayParameter", uri);
            Asset assetConsumer = mgr.CreateAssetProxy(Settings.RootTopic, "OperationWithArrayParameter", uri);

            // bind local operation to asset operation
            assetOwner.Submodel("object_operation").Operation("getsumofelements").Callback = (SubmodelOperationRequest req) =>
            {
                var response = req.CreateResponseObj();

                double sum = 0.0;
                var values = req.Parameters["tuple"];
                foreach (var d in ((JsonElement)values).EnumerateArray())
                {
                    sum += ((JsonElement)d).GetDouble();
                }
                response.Value = sum;
                return response;
            }; 

            //object[] elements = new object[] { 9.0, 17.5 };
            var elements = new Dictionary<string, object>()
            {
                {"tuple", new double[]{ 9.0, 17.5 } }
            };
            var response = assetConsumer.Submodel("object_operation").Operation("getsumofelements").Invoke(
                elements,
                TimeSpan.FromSeconds(5));

            // check result: 
            double res = response.GetReturnValueOrDefault<double>(); 

            Assert.Equal(
                26.5,
                res);
        }

        [Fact]
        public void OperationWithArrayReturnType()
        {
            string location = this.GetType().Assembly.Location;
            location = Path.GetDirectoryName(location);
            location = Path.Combine(location, "resources/object_operation.json");
            Uri uri = new Uri(location);

            AssetMgr mgr = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, Settings.RootTopic, Settings.EndpointName);

            Asset assetOwner = mgr.CreateAsset(Settings.RootTopic, "OperationWithArrayReturnType", uri);
            Asset assetConsumer = mgr.CreateAssetProxy(Settings.RootTopic, "OperationWithArrayReturnType", uri);

            // bind local operation to asset operation
            assetOwner.Submodel("object_operation").Operation("arrayoperation").Callback = (SubmodelOperationRequest req) =>
            {
                var resp = req.CreateResponseObj();

                resp.Value = new double[]
                {
                    0.0, 1.0, 2.0, 3.0, 4.0
                };

                return resp;
            }; 

            var result = assetConsumer.Submodel("object_operation").Operation("arrayoperation").Invoke(new Dictionary<string, object>());

            Assert.Equal(5, result.GetReturnValueOrDefault<double[]>().Length);
            Assert.Equal(2.0, result.GetReturnValueOrDefault<double[]>()[2]); 
        }


    }
}
