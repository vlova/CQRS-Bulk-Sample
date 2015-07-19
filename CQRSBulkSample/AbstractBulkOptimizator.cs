using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CQRSBulkSample {
	abstract class AbstractBulkQueryOptimizator<TQuery, TResult>
		: IMessageOptimizator where TQuery : IQuery<TResult> {
		public IDictionary<IMessage, Task> Optimize(IBundle bundle, IEnumerable<PendingMessage> queries) {
			var specificQueries = queries.Select(t => t.Message).OfType<TQuery>().ToList();
			if (specificQueries.Count > 1) {
				return OptimizeBulk(bundle, specificQueries);
			}
			else {
				return new Dictionary<IMessage, Task>(ReferenceEqualityComparer<IMessage>.Instance);
			}
		}

		protected abstract IDictionary<IMessage, Task> OptimizeBulk(IBundle bundle, IEnumerable<TQuery> specificQueries);

		protected IDictionary<IMessage, Task> Map<TBulkResult>(
			IEnumerable<TQuery> queries,
			Task<TBulkResult> bulkTask,
			Func<TBulkResult, TQuery, TResult> selector
		) {
			return queries.ToDictionary(q => (IQuery)q, q => (Task)GetFromTask(bulkTask, q, selector), ReferenceEqualityComparer<IMessage>.Instance);
		}

		private async Task<TResult> GetFromTask<TBulkResult>(Task<TBulkResult> bulkTask, TQuery q, Func<TBulkResult, TQuery, TResult> selector) {
			await bulkTask;
			return selector(bulkTask.Result, q);
		}
	}

	abstract class AbstractBulkCommandOptimizator<TCommand> : IMessageOptimizator where TCommand:ICommand {
		public IDictionary<IMessage, Task> Optimize(IBundle bundle, IEnumerable<PendingMessage> commands) {
			var specificCommands = commands.Select(t => t.Message).OfType<TCommand>().ToList();
			if (specificCommands.Count > 1) {
				var command = OptimizeBulk(bundle, specificCommands);
				var task = bundle.Do(command);
				return specificCommands.ToDictionary(c => (IMessage)c, c => task, ReferenceEqualityComparer<IMessage>.Instance);
			}
			else {
				return new Dictionary<IMessage, Task>(ReferenceEqualityComparer<IMessage>.Instance);
			}
		}

		protected abstract ICommand OptimizeBulk(IBundle bundle, IEnumerable<TCommand> specificCommands);
	}
}
