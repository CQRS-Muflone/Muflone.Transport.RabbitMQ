using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Muflone.Transport.RabbitMQ.Abstracts;

public abstract class ConsumerBase
{
	protected readonly ILogger Logger;

	protected ConsumerBase(IServiceProvider serviceProvider)
	{
		var loggerFactory = serviceProvider.GetService<ILoggerFactory>();
		Logger = loggerFactory!.CreateLogger(GetType());
	}
}