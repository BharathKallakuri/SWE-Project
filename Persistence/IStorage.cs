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
        Task Update(string key, byte[] data);
        Task Delete(string key);

    }

}

