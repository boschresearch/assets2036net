// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using System.Runtime.Serialization;

namespace assets2036net
{
    /// <summary>
    /// All valid datatypes for submodel properties. Aligned with JSON schema data types. 
    /// </summary>
    public class ValueType 
    {
        const string @boolean = "boolean"; 
        const string @string = "string"; 
        const string @object = "object"; 
        const string @array = "array"; 
        const string @integer = "integer"; 
        const string @number = "number"; 
        const string @list = "list"; 
    }
}
