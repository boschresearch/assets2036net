﻿// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace assets2036net.unittests
{
    class Settings
    {
        // public static string BrokerHost = "broker.hivemq.com";
        public static string BrokerHost = "test.mosquitto.org";
        // public static string BrokerHost = "192.168.100.3";
        public static int BrokerPort = 1883;
        public static string EndpointName = "assets2036net_tests"; 

        public static TimeSpan WaitTime = TimeSpan.FromSeconds(50);
        public static string RootTopic = "arena2036test";

        public static Uri GetUriToEndpointSubmodel()
        {
            //Stream s = typeof(Submodel).Assembly.GetManifestResourceStream("assets2036net.resources._endpoint.json");
            //TextReader text = new StreamReader(s);
            //var json = text.ReadToEnd();

            //var path = Path.GetTempFileName();
            //TextWriter outWriter = new StreamWriter(path);
            //outWriter.Write(json);
            //outWriter.Close();

            //return new Uri(path);

            return new Uri("https://raw.githubusercontent.com/boschresearch/assets2036-submodels/master/_endpoint.json");
        }

    }
}
