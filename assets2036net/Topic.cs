// Copyright (c) 2021 - for information on the respective copyright owner
// see the NOTICE file and/or the repository github.com/boschresearch/assets2036net.
//
// SPDX-License-Identifier: Apache-2.0

using System.Linq;

namespace assets2036net
{
    public class Topic
    {
        public Topic(string text)
        {
            Text = text; 
        }

        public static implicit operator string(Topic t)
        {
            return t.ToString(); 
        }

        public static Topic From(params string[] topicElements)
        {
            return new Topic(string.Join("/", topicElements)); 
        }

        public string Text { 
            get; 
            internal set; 
        }

        internal string GetRootTopicName()
        {
            return Text.Split('/')[0];
        }

        internal string GetAssetName()
        {
            return Text.Split('/')[1];
        }

        internal string GetFullAssetName()
        {
            var elements = Text.Split('/');
            return CommElementBase.BuildTopic(elements[0], elements[1]); 
        }

        internal string GetSubmodelName()
        {
            return Text.Split('/')[2];
        }

        internal string GetElementName()
        {
            return Text.Split('/')[3];
        }

        public override string ToString()
        {
            return Text; 
        }
    }
}
