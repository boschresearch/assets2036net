// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using log4net;
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Xml;

namespace assets2036net.unittests
{
    public class UnitTestBase
    {
        protected object TheOneLock = new object(); 

        protected ILog log { get; private set; }
        public UnitTestBase()
        {
            string location = this.GetType().Assembly.Location;
            location = Path.GetDirectoryName(location);
            location = Path.Combine(location, "resources/log4net.config");

            XmlDocument log4netConfig = new XmlDocument();
            log4netConfig.Load(File.OpenRead(location));

            var repo = LogManager.CreateRepository(
                Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));

            log4net.Config.XmlConfigurator.Configure(repo, log4netConfig["log4net"]);

            log = LogManager.GetLogger(this.GetType());

            assets2036net.Config.Log4NetRepoToUse = repo; 
        }

        protected delegate bool BoolExpression(); 

        protected bool waitForCondition(BoolExpression cond, TimeSpan maxWaitTime)
        {
            int waited = 0;
            while (waited <= Settings.WaitTime.TotalMilliseconds)
            {
                Thread.Sleep(10);
                waited += 10;

                if (cond())
                {
                    return true;
                }
            }

            return cond();
        }
    }
}
