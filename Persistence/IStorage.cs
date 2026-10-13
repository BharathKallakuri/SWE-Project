using System;
using System.Collections.Generic;
using System.Text;

namespace Persistence
{
    // All CRUD operations are asynchronous
    // Applications module interacts with the Persistence module through this interface
    public interface IStorage
    {
        Task Create(string key, byte[] data);
        Task<byte[]> Read(string key);
        Task<bool> Update(string key, byte[] data, string etag);
        Task Delete(string key);
        Task<string> ReadEtag(string key);

    }

}

