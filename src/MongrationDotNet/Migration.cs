using System;
using System.Threading.Tasks;
using MongoDB.Driver;

namespace MongrationDotNet
{
    public abstract class Migration
    {
        public abstract Version Version { get; }
        public abstract string Type { get; }
        public virtual string Description { get; }

        public abstract Task ExecuteAsync(IMongoDatabase database);
    }
}