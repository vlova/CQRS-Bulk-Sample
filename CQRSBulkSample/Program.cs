using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CQRSBulkSample {
	class Program {
		static void Main(string[] args) {
			var bundle = BundleFactory.CreateBundle();

			Task.Run(async () => await Do(bundle).ConfigureAwait(false)).Wait();
		}

		private static async Task Do(IBundle bundle) {
			var tasks = GetEmailsToCreate().Select(email => CreateUserIfNotExists(bundle, email)).ToList();
			await bundle.Wait(tasks);
		}

		private static List<string> GetEmailsToCreate() {
			var emails = new List<string> { "god@gmail.com", "pikabu@pikabu.ru", "coolguy@gmail.com", "coolguy@gmail.com", "d3@d3.ru" };

			for (int i = 0; i < 10; i++) {
				emails.Add("ololo" + i / 2 + "@gmail.com");
			}

			// adding duplicates
			emails.AddRange(emails);
			emails.AddRange(emails);

			return emails;
		}

		private static async Task CreateUserIfNotExists(IBundle bundle, string email) { // note: must be a command
			var userExists = await bundle.Query(new EmailExists() { Email = email });
			if (!userExists) {
				await bundle.Do(new CreateUser() { Email = email });
				Console.WriteLine("\tuser {0} created", email);
			}
			else {
				Console.WriteLine("\tuser {0} exists", email);
			}
		}
	}

}
