// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using log4net;
using log4net.Repository;
using System.IO;
using System.Reflection;

namespace assets2036net
{
    public class Config
    {
        internal static string EndpointSubmodelDescriptionUrl
        {
            get
            {
                string resourceName = "";
                foreach (var cand in Assembly.GetExecutingAssembly().GetManifestResourceNames())
                {
                    if (cand.EndsWith("_endpoint.json"))
                    {
                        resourceName = cand;
                    }
                }

                string fullPath = Path.Combine(Path.GetTempPath(), "_endpoint.json");

                if (!File.Exists(fullPath))
                {
                    using (Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
                    {
                        using (StreamReader reader = new StreamReader(stream))
                        {
                            string result = reader.ReadToEnd();
                            File.WriteAllText(fullPath, result);
                        }
                    }
                }

                return new System.Uri(fullPath).ToString();
            }
        }

        private static ILoggerRepository _log4netRepo; 

        public static ILoggerRepository Log4NetRepoToUse
        {
            get
            {
                return _log4netRepo; 
            }
            set
            {
                _log4netRepo = value; 
            }
        }

        internal static ILog GetLogger(string loggerName)
        {
            if (Log4NetRepoToUse == null)
            {
                Log4NetRepoToUse = LogManager.CreateRepository("assets2036net");
            }

            ILog result = LogManager.GetLogger(Log4NetRepoToUse.Name, loggerName);

            return result; 
        }

        internal static string Assets2036SubmodelSchema
        {
            get
            {
                string schemaResourceName = "assets2036net.resources.submodel.schema"; 
                var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(schemaResourceName);
                var reader = new StreamReader(stream);
                return reader.ReadToEnd(); 
            }
        }
    }
}
