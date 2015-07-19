using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CQRSBulkSample {
	class EmailExists : IQuery<bool> {
		public string Email { get; set; }

		public override string ToString() {
			return Email;
		}

		public override bool Equals(object obj) {
			if (!(obj is EmailExists)) return false;
			return Email.Equals(((EmailExists)obj).Email);
		}

		public override int GetHashCode() {
			return Email.GetHashCode();
		}
	}

	class EmailExistsHandler : IQueryHandler<EmailExists, bool> {
		public static string[] emails = new[] { "god@gmail.com" };


		public async Task<bool> Handle(EmailExists query) {
			await Task.Delay(1000);
			return emails.Contains(query.Email);
		}
	}

	class GetExistingEmails : IQuery<IEnumerable<string>> {
		public IEnumerable<string> Emails { get; set; }

		public override string ToString() {
			return string.Join(";", Emails);
		}
	}

	class GetExistingEmailsHandler : IQueryHandler<GetExistingEmails, IEnumerable<string>> {
		public async Task<IEnumerable<string>> Handle(GetExistingEmails query) {
			await Task.Delay(1000);
			return EmailExistsHandler.emails;
		}
	}

	class CreateUser : ICommand {
		public string Email { get; set; }

		public override string ToString() {
			return Email;
		}

		public override bool Equals(object obj) {
			if (!(obj is CreateUser)) return false;
			return Email.Equals(((CreateUser)obj).Email);
		}

		public override int GetHashCode() {
			return Email.GetHashCode();
		}
	}

	class CreateUserHandler : ICommandHandler<CreateUser> {
		public async Task Handle(CreateUser command) {
			await Task.Delay(1000);
		}
	}

	class CreateUserBulk : ICommand {
		public IEnumerable<string> Emails { get; set; }

		public override string ToString() {
			return string.Join(";", Emails);
		}
	}

	class CreateUserBulkHandler : ICommandHandler<CreateUserBulk> {
		public async Task Handle(CreateUserBulk command) {
			await Task.Delay(1000);
		}
	}

	class UserWithEmailExistsOptimizator : AbstractBulkQueryOptimizator<EmailExists, bool> {
		protected override IDictionary<IMessage, Task> OptimizeBulk(IBundle bundle, IEnumerable<EmailExists> queries) {
			return Map(
				queries,
				bundle.Query(new GetExistingEmails() { Emails = queries.Select(s => s.Email).ToList() }),
				(results, query) => results.Contains(query.Email));
		}
	}

	class CreateUserBulkOptimizator : AbstractBulkCommandOptimizator<CreateUser> {
		protected override ICommand OptimizeBulk(IBundle bundle, IEnumerable<CreateUser> commands) {
			return new CreateUserBulk() { Emails = commands.Select(c => c.Email).ToList() };
		}
	}
}
