using System;
using System.Threading;
using System.Threading.Tasks;

namespace Examples
{
    /// <summary>
    /// Simple program, runing an asset and an asset proxy, to show howto read 
    /// properties and call operations. 
    /// For further examples, e.g. howto use object type parameters, see the unit tests! 
    /// </summary>
    class Program
    {
        static void Main(string[] args)
        {
            log4net.Config.BasicConfigurator.Configure(); 

            // Create an asset manager for the owner side: 
            var mgrOwner = new assets2036net.AssetMgr("test.mosquitto.org", 1883, "arena2036", "example_asset");

            // URLs to the Submodel descriptions we want to implement: 
            var urlSixAxisRobot = new Uri("https://raw.githubusercontent.com/boschresearch/assets2036-submodels/master/six-axis-robot.json");
            var urlLight = new Uri("https://raw.githubusercontent.com/boschresearch/assets2036-submodels/master/light.json");

            // create the owned asset itself: 
            var asset = mgrOwner.CreateAsset("example_asset", new Uri[] { urlLight, urlSixAxisRobot });

            // implement a submodel's operation: 
            asset.Submodel("light").Operation("switch_light").Callback = switchLight;

            // run some task to continously modify some properties of the asset: 
            // It's the "business logic"
            Task tOwner = Task.Run(() =>
            {
                double someValue = 0.0;
                while (!Terminate)
                {
                    Thread.Sleep(TimeSpan.FromMilliseconds(100));
                    asset.Submodel("six_axis_robot").Property("joint1").Value = someValue++;
                }
            });


            // Now create an asset proxy to remotely access and use the asset
            var proxy = mgrOwner.CreateFullAssetProxy("arena2036", "example_asset");

            // build small ui: 
            while (!Terminate)
            {
                var key = PrintUi(); 

                switch (key.Key)
                {
                    case ConsoleKey.Q:
                        {
                            Terminate = true; 
                            break; 
                        }
                    case ConsoleKey.O:
                        {
                            Console.WriteLine(); 
                            // call remotely implemented operation
                            var result = proxy.Submodel("light").Operation("switch_light").Invoke(new System.Collections.Generic.Dictionary<string, object>()
                            {
                                {"state" , !LightState}
                            });

                            Console.WriteLine(string.Format("Operation called successfully, return value {0}", (bool)result.Value));

                            break; 
                        }
                    case ConsoleKey.P:
                        {
                            // For two seconds print the property values: 
                            var started = DateTime.Now; 
                            while (DateTime.Now.Subtract(started) < TimeSpan.FromSeconds(2))
                            {
                                Console.WriteLine("Property Joint 1: " + proxy.Submodel("six_axis_robot").Property("joint1").ValueDouble);  ;
                                Thread.Sleep(TimeSpan.FromMilliseconds(50)); 
                            }

                            break; 
                        }
                    case ConsoleKey.M:
                        {
                            Console.WriteLine();

                            proxy.Submodels.ForEach(submodel =>
                            {
                                Console.WriteLine(string.Format("Submodel {0}, URL: {1}", submodel.Name, submodel.SubmodelUrl));
                                Console.WriteLine("  Properties: ");
                                foreach (var e in submodel.GetProperties())
                                {
                                    Console.WriteLine(string.Format("    {0}, Type: {1}, Current Value: {2}", e.Name, e.Type, e.Value));
                                }

                                Console.WriteLine("  Operations: ");
                                foreach (var e in submodel.GetOperations())
                                {
                                    Console.WriteLine(string.Format("    {0}, Parameters: {1}", e.Name, e.Parameters));
                                }

                                Console.WriteLine("  Events: ");
                                foreach (var e in submodel.GetEvents())
                                {
                                    Console.WriteLine(string.Format("    {0}, Parameters: {1}", e.Name, e.Parameters));
                                }
                            }); 

                            break;
                        }
                }
            }

            tOwner.Wait();
        }

        static bool Terminate = false; 

        static bool LightState = false; 

        /// <summary>
        /// This method is called, when some consumer calls the method switch light of 
        /// our owned asset. 
        /// </summary>
        /// <param name="req">the submodel operation request</param>
        /// <returns>a submodel operation response. </returns>
        static assets2036net.SubmodelOperationResponse switchLight(assets2036net.SubmodelOperationRequest req)
        {
            // get value type operation call parameter: 
            bool state = (bool)req.Parameters["state"];

            // do whatever this operation should do: 
            Console.WriteLine(string.Format("Call to switchLight with parameter {0}", state));
            LightState = state; 

            // create resp object: 
            var resp = req.CreateResponseObj();

            // set response parameter / return value
            resp.Value = LightState; 

            return resp; 
        }

        static ConsoleKeyInfo PrintUi()
        {
            Console.WriteLine("\n\nChoose: "); 
            Console.WriteLine("O: Call operation\nP: Get Property\nM: List submodels\nQ: Quit app");

            return Console.ReadKey(); 
        }
    }
}
