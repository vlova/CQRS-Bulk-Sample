using System.Collections.Generic;
using System.Threading.Tasks;

namespace CQRSBulkSample {
	interface IMessage {

	}

	interface IQuery : IMessage {

	}

	interface IQuery<TRes> : IQuery {

	}

	interface ICommand : IMessage {

	}

	interface IQueryHandler {

	}

	interface IQueryHandler<TQuery, TResult> : IQueryHandler where TQuery : IQuery<TResult> {
		Task<TResult> Handle(TQuery query);
	}

	interface ICommandHandler {

	}

	interface ICommandHandler<TCommand> : ICommandHandler where TCommand : ICommand {
		Task Handle(TCommand command);
	}

	interface IMessageOptimizator {
		IDictionary<IMessage, Task> Optimize(IBundle bundle, IEnumerable<PendingMessage> queries);
	}

	interface IBundle {
		Task<TResult> Query<TResult>(IQuery<TResult> query);
		Task Do(ICommand command);
		Task Wait(IEnumerable<Task> tasks);
		void RegisterQueryHandler<THandler>() where THandler : IQueryHandler;
		void RegisterCommandHandler<THandler>() where THandler : ICommandHandler;
		void RegisterOptimizator<TOptimizator>() where TOptimizator : IMessageOptimizator;
	}
}
