using Muflone.Core;
using Muflone.Persistence;
using System.Collections.Concurrent;

namespace Muflone.Transport.RabbitMQ.Tests;

public class InMemoryRepository : IRepository, IDisposable
{
	internal static readonly ConcurrentDictionary<Guid, string> Data = new();


	public void Dispose()
	{
		Data.Clear();
	}

	public Task SaveAsync(IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>> updateHeaders, CancellationToken cancellationToken = default)
	{
		throw new NotImplementedException();
	}

	public Task SaveAsync(IAggregate aggregate, Guid commitId, CancellationToken cancellationToken = default)
	{
		throw new NotImplementedException();
	}

	Task<TAggregate?> IRepository.GetByIdAsync<TAggregate>(IDomainId id, CancellationToken cancellationToken) where TAggregate : class
	{
		throw new NotImplementedException();
	}

	Task<TAggregate?> IRepository.GetByIdAsync<TAggregate>(IDomainId id, long version, CancellationToken cancellationToken) where TAggregate : class
	{
		throw new NotImplementedException();
	}
}