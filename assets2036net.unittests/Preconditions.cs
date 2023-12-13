// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using log4net;
using log4net.Repository;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Xunit;

namespace assets2036net.unittests
{
    public class _Preconditions : UnitTestBase
    {
        [Fact]
        public void _CleanAllRetainedMessages()
        {
            Tools.CleanAllRetainedMessages(Settings.BrokerHost, Settings.BrokerPort, Settings.Namespace);
            //Thread.Sleep(5000);
        }
    }
}
