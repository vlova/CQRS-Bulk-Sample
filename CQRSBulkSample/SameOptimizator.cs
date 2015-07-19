using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CQRSBulkSample {
	abstract class SameMessageOptimizator : IMessageOptimizator {
		public virtual IDictionary<IMessage, Task> Optimize(IBundle bundle, IEnumerable<PendingMessage> messages) {
			return messages
				.GroupBy(q => q.Message)
				.Where(g => g.Count() > 1)
				.Select(g => new { duplicateMessages = g.Skip(1).ToList(), task = (Task)g.First().TCS.Task })
				.SelectMany(g => g.duplicateMessages.Select(q => new { message = q, task = g.task }))
				.ToDictionary(p => (IMessage)p.message.Message, p => p.task, ReferenceEqualityComparer<IMessage>.Instance);
		}
	}

	class SameTypeOptimizator<TMessage> : SameMessageOptimizator {
		public override IDictionary<IMessage, Task> Optimize(IBundle bundle, IEnumerable<PendingMessage> messages) {
			return base.Optimize(bundle, messages.Where(c => c.Message is TMessage));
		}
	}
}
