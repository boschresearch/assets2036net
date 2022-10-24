// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using System;
using System.Collections.Generic;
using System.Text;

namespace assets2036net
{
    /// <summary>
    /// Default provider for the endpoint submodel. Operation calls are mapped to 
    /// c#-events, where they can be handled. 
    /// </summary>
    public class SubmodelEndpoint : Submodel
    {
        public SubmodelEndpoint()
            : base()
        {
            Name = StringConstants.SubmodelNameEnpoint; 
        }


        public bool Online { get; set; }
        public bool Healthy {
            get
            {
                return Property("healthy").ValueBool; 
            }
            set
            {
                Property("healthy").Value = value; 
            }
        }

        public event Action ShutdownTriggered;
        public event Action RestartTriggered;

        internal void Shutdown()
        {
            if (ShutdownTriggered != null)
            {
                ShutdownTriggered.Invoke(); 
            }
        }
        internal void Restart()
        {
            if (RestartTriggered != null)
            {
                RestartTriggered.Invoke();
            }
        }
    }
}
