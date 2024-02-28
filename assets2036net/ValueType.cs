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
    // [JsonConverter(typeof(StringEnumConverter))]
    public enum ValueType
    {
        [EnumMember(Value ="boolean")]
        boolean,
        [EnumMember(Value = "string")]
        @string,
        [EnumMember(Value = "object")]
        @object,
        [EnumMember(Value = "array")]
        array,
        [EnumMember(Value = "integer")]
        integer,
        [EnumMember(Value = "number")]
        number,
        [EnumMember(Value = "list")]
        list,
    }
}
