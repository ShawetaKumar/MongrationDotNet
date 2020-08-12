using System;

namespace MongrationDotNet
{
    public abstract class Migration
    {
        public abstract Version Version { get; }
        public abstract string Type { get; } 
        public virtual string Description { get; }
    }
}