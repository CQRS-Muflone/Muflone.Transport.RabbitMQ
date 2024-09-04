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

	public Task<TAggregate> GetByIdAsync<TAggregate>(Guid id) where TAggregate : class, IAggregate
	{
		throw new NotImplementedException();
	}

	public Task<TAggregate> GetByIdAsync<TAggregate>(Guid id, int version) where TAggregate : class, IAggregate
	{
		throw new NotImplementedException();
	}

	public Task SaveAsync(IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>> updateHeaders)
	{
		throw new NotImplementedException();
	}

	public Task SaveAsync(IAggregate aggregate, Guid commitId)
	{
		throw new NotImplementedException();
	}

	public Task<TAggregate> GetByIdAsync<TAggregate>(Guid id, CancellationToken cancellationToken = new CancellationToken()) where TAggregate : class, IAggregate
	{
		throw new NotImplementedException();
	}

	public Task<TAggregate> GetByIdAsync<TAggregate>(Guid id, long version, CancellationToken cancellationToken = new CancellationToken()) where TAggregate : class, IAggregate
	{
		throw new NotImplementedException();
	}

	public Task SaveAsync(IAggregate aggregate, Guid commitId, Action<IDictionary<string, object>> updateHeaders,
		CancellationToken cancellationToken = new CancellationToken())
	{
		throw new NotImplementedException();
	}

	public Task SaveAsync(IAggregate aggregate, Guid commitId, CancellationToken cancellationToken = new CancellationToken())
	{
		throw new NotImplementedException();
	}
}