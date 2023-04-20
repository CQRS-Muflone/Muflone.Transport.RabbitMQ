using Microsoft.Extensions.Logging;
using Muflone.Persistence;

namespace Muflone.Transport.RabbitMQ.Abstracts;

public abstract class ConsumerBase
{
	protected readonly ILogger Logger;


	/// <summary>
	/// For now just as a proxy to pass directly to the Handler this class is wrapping
	/// </summary>
	protected IRepository Repository { get; }

	/// <summary>
	/// For now just as a proxy to pass directly to the Handler this class is wrapping
	/// </summary>
	protected ILoggerFactory LoggerFactory { get; }

	protected ConsumerBase(IRepository repository, ILoggerFactory loggerFactory)
	{
		Repository = repository ?? throw new ArgumentNullException(nameof(repository));
		LoggerFactory = loggerFactory;
		Logger = loggerFactory.CreateLogger(GetType()) ?? throw new ArgumentNullException(nameof(loggerFactory));
	}
}