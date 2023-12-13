using System;
using System.IO;
using Xunit;

namespace assets2036net.unittests
{

    public class Tuple
    {
        public string first { get; set; }
        public string second { get; set; }

    }
    public class RequestParamsConvenienceMethods : UnitTestBase
    {
        [Fact]
        public void CallSimpleOperation()
        {
            string location = this.GetType().Assembly.Location;
            location = Path.GetDirectoryName(location);
            location = Path.Combine(location, "resources/object_operation.json");
            Uri uri = new Uri(location);

            using AssetMgr mgrOwner = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, Settings.Namespace, Settings.EndpointName);
            using AssetMgr mgrConsumer = new AssetMgr(Settings.BrokerHost, Settings.BrokerPort, Settings.Namespace, Settings.EndpointName);

            using Asset assetOwner = mgrOwner.CreateAsset(Settings.Namespace, "OperationWithObjectType", uri);
            using Asset assetConsumer = mgrConsumer.CreateAssetProxy(Settings.Namespace, "OperationWithObjectType", uri);

            // bind local operation to asset operation
            assetOwner.Submodel("object_operation").Operation("getfirstparam").Callback = (SubmodelOperationRequest req) =>
            {
                var resp = req.CreateResponseObj();

                var t = req.GetParameterValueOrDefault<Tuple>("tuple"); 
                resp.Value = t.first; 

                return resp;
            }; 

            var resp = assetConsumer.Submodel("object_operation").Operation("getfirstparam").Invoke(new System.Collections.Generic.Dictionary<string, object>()
            {
                {
                    "tuple", new Tuple
                    {
                        first = "eins", second="zwei"
                    }
                }
            }); 

            Assert.Equal("eins", resp.GetReturnValueOrDefault<string>()); 
        }
    }
}