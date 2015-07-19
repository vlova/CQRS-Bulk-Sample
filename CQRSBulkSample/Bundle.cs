using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CQRSBulkSample {
	class Bundle : IBundle {
		private readonly List<Type> handlerTypes = new List<Type>();
		private readonly List<PendingMessage> pendingMessages = new List<PendingMessage>();
		private readonly List<IMessageOptimizator> optimizators = new List<IMessageOptimizator>();

		public Task<TResult> Query<TResult>(IQuery<TResult> query) {
			var tcs = new TaskCompletionSource<TResult>();

			lock (pendingMessages) {
				pendingMessages.Add(new PendingMessage {
					Message = query,
					TCS = tcs
				});
			}

			return tcs.Task;
		}

		public Task Do(ICommand command) {
			var tcs = new TaskCompletionSource<object>();

			lock (pendingMessages) {
				pendingMessages.Add(new PendingMessage {
					Message = command,
					TCS = tcs
				});
			}

			return tcs.Task;
		}

		public async Task Wait(IEnumerable<Task> tasks = null) {
			var incompletedTasks = new List<Task>();
			if (tasks != null) incompletedTasks.AddRange(tasks);
			while (true) {
				await Task.Yield();
				bool hasMessages = await ForceInternal();
				incompletedTasks.RemoveAll(t => t.IsCompleted);
				if (!hasMessages && !incompletedTasks.Any()) {
					break;
				}
			}
		}

		private static int ActiveChildCount(Task t) {
			var property = typeof(Task).GetProperty("ActiveChildCount", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
			var value = (int)property.GetValue(t);
			if (value != 0) Console.WriteLine(value);
			return value;
		}

		private async Task<bool> ForceInternal() {
			await Task.Yield();
			IEnumerable<PendingMessage> messagesToExecute;
			lock (pendingMessages) {
				messagesToExecute = pendingMessages.Where(q => !q.IsRunning).ToList();
			}

			var hasMessages = messagesToExecute.Any();
			if (!hasMessages) {
				return hasMessages;
			}

			var optimizedMessageList = Optimize(messagesToExecute);

			lock (pendingMessages) {
				pendingMessages.RemoveAll(p => p.IsRunning);
			}

			var tasks = optimizedMessageList.Select(pending => RunMessageHandle(pending)).ToList();
			await Task.WhenAll(tasks);

			return hasMessages;
		}



		private IEnumerable<PendingMessage> Optimize(IEnumerable<PendingMessage> toExecute) {
			foreach (var optimizator in optimizators) {
				var queries = toExecute;
				if (!queries.Any()) break;
				var optimizations = optimizator.Optimize(this, queries);
				var optimized = toExecute.Where(q => optimizations.ContainsKey(q.Message));
				toExecute = toExecute.Where(q => !optimizations.Keys.Contains(q.Message));
				foreach (var item in optimized) {
					item.IsRunning = true;
					var optimizedTask = optimizations[item.Message];
					optimizedTask.ContinueWith((task) => item.TCS.TrySetResult(((dynamic)task).Result));
				}
			}
			return toExecute;
		}

		private async Task RunMessageHandle(PendingMessage pendingMessage) {
			try {
				Console.WriteLine("handling {0} {1}", pendingMessage.Message.GetType().Name, pendingMessage.Message.ToString());
				pendingMessage.IsRunning = true;
				dynamic handler = GetHandler((dynamic)pendingMessage.Message);
				dynamic task = handler.Handle((dynamic)pendingMessage.Message);
				await task;
				pendingMessage.TCS.SetResult(handler is IQueryHandler ? task.Result : null);
			}
			catch (Exception ex) {
				pendingMessage.TCS.SetException(ex);
			}
			finally {
				lock (pendingMessages) {
					pendingMessages.Remove(pendingMessage);
				}
			}
		}

		private IQueryHandler GetHandler(IQuery query) {
			var handlerType = handlerTypes
				.Where(typeof(IQueryHandler).IsAssignableFrom)
				.Where(t => t.GetInterfaces()
					.Where(i => i.IsGenericType)
					.Where(i => i.GetGenericTypeDefinition() == typeof(IQueryHandler<,>))
					.Where(i => i.GetGenericArguments()[0] == query.GetType())
					.Any())
				.FirstOrDefault();

			return (IQueryHandler)Activator.CreateInstance(handlerType);
		}

		private ICommandHandler GetHandler(ICommand command) {
			var handlerType = handlerTypes
				.Where(typeof(ICommandHandler).IsAssignableFrom)
				.Where(t => t.GetInterfaces()
					.Where(i => i.IsGenericType)
					.Where(i => i.GetGenericTypeDefinition() == typeof(ICommandHandler<>))
					.Where(i => i.GetGenericArguments()[0] == command.GetType())
					.Any())
				.FirstOrDefault();

			return (ICommandHandler)Activator.CreateInstance(handlerType);
		}

		public void RegisterQueryHandler<THandler>() where THandler : IQueryHandler {
			handlerTypes.Add(typeof(THandler));
		}

		public void RegisterCommandHandler<THandler>() where THandler : ICommandHandler {
			handlerTypes.Add(typeof(THandler));
		}

		public void RegisterOptimizator<TOptimizator>() where TOptimizator : IMessageOptimizator {
			optimizators.Add(Activator.CreateInstance<TOptimizator>());
		}
	}
}
