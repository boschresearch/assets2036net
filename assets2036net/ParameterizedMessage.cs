using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace assets2036net
{
    public abstract class ParameterizedMessage : CommElementBase
    {
        private readonly static log4net.ILog log = Config.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType.FullName);
        /// <summary>
        /// a dictionary containig the request's corresponding parameters. 
        /// </summary>
        [JsonPropertyName("params")]
        public Dictionary<string, object> Parameters { get; set; }

        public int GetParameterInt32(string paramName) 
        {
            if (!Parameters.ContainsKey(paramName))
            {
                log.Warn($"paramater named {paramName} not found - returning default value"); 
                return 0; 
            }

            try
            {
                return ((JsonElement)Parameters[paramName]).GetInt32(); 
            }
            catch
            {
                log.Warn($"deserialization of paramater named {paramName} failed - returning default value"); 
                return 0; 
            }
        }

        public string GetParameterString(string paramName) 
        {
            if (!Parameters.ContainsKey(paramName))
            {
                log.Warn($"paramater named {paramName} not found - returning default value"); 
                return null; 
            }

            try
            {
                return ((JsonElement)Parameters[paramName]).GetString(); 
            }
            catch
            {
                log.Warn($"deserialization of paramater named {paramName} failed - returning default value"); 
                return null; 
            }
        }

        public long GetParameterInt64(string paramName) 
        {
            if (!Parameters.ContainsKey(paramName))
            {
                log.Warn($"paramater named {paramName} not found - returning default value"); 
                return (Int64)0; 
            }

            try
            {
                return ((JsonElement)Parameters[paramName]).GetInt64(); 
            }
            catch
            {
                log.Warn($"deserialization of paramater named {paramName} failed - returning default value"); 
                return (long)0; 
            }
        }

        public double GetParameterDouble(string paramName) 
        {
            if (!Parameters.ContainsKey(paramName))
            {
                log.Warn($"paramater named {paramName} not found - returning default value"); 
                return (double)0.0; 
            }

            try
            {
                return ((JsonElement)Parameters[paramName]).GetDouble(); 
            }
            catch
            {
                log.Warn($"deserialization of paramater named {paramName} failed - returning default value"); 
                return (double)0.0; 
            }
        }

        public float GetParameterFloat(string paramName) 
        {
            if (!Parameters.ContainsKey(paramName))
            {
                log.Warn($"paramater named {paramName} not found - returning null value"); 
                return 0.0f; 
            }

            try
            {
                return (float)((JsonElement)Parameters[paramName]).GetDouble(); 
            }
            catch
            {
                log.Warn($"deserialization of paramater named {paramName} failed - returning default value"); 
                return 0.0f; 
            }
        }

        public bool GetParameterBool(string paramName) 
        {
            if (!Parameters.ContainsKey(paramName))
            {
                log.Warn($"paramater named {paramName} not found - returning default value"); 
                return false; 
            }

            try
            {
                return ((JsonElement)Parameters[paramName]).GetBoolean(); 
            }
            catch
            {
                log.Warn($"deserialization of paramater named {paramName} failed - returning default value"); 
                return false; 
            }
        }

        public T GetParameterValueOrDefault<T>(string parameterKey, T defaultValue = default)
        {
            if (!Parameters.ContainsKey(parameterKey))
            {
                return defaultValue; 
            }

            var value = (JsonElement)Parameters[parameterKey]; 

            try
            {
                var res = value.Deserialize<T>(); 
                return res; 
            }
            catch (Exception)
            {
                return defaultValue; 
            }
        }
    }
}