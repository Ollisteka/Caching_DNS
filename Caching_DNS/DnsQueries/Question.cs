using System;
using Caching_DNS.Enums;
using Caching_DNS.Helpers;

namespace Caching_DNS.DnsQueries
{
    [Serializable]
    public class Question
    {
        public readonly ResourceClass Class;
        public readonly string Name;
        public readonly ResourceType Type;

        public Question(ResourceClass @class, string name, ResourceType type)
        {
            Class = @class;
            Name = name;
            Type = type;
        }

        public override string ToString()
        {
            return $"{Name} {Type.Description()}  {Class.Description()}";
        }
    }
}