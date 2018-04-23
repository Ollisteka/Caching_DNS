﻿using System;
using Caching_DNS.Helpers;

namespace Caching_DNS.DnsQueries
{
    [Serializable]
    public class Question
    {
        public ResourceClass Class;
        public string Name;
        public ResourceType Type;

        public override string ToString()
        {
            return $"{Name} {Type.Description()}  {Class.Description()}";
        }
    }
}