# CQRS-Bulk-Sample

This is a sample of optimizing pending queries and messages within CQRS style.

For example, let we have following code

    var tasks = GetEmailsToCreate().Select(email => CreateUserIfNotExists(bundle, email)).ToList();
    await bundle.Wait(tasks);
    
    private static async Task CreateUserIfNotExists(IBundle bundle, string email) {
      var userExists = await bundle.Query(new EmailExists() { Email = email });
      if (!userExists) {
        await bundle.Do(new CreateUser() { Email = email });
        Console.WriteLine("\tuser {0} created", email);
      }
      else {
        Console.WriteLine("\tuser {0} exists", email);
      }
    }
    
Obviously, this is not optimal code. If we need to create ~1000 users we will waste a lot of time to perform this operation. But what if we have ability to perform optimizing of queries and messages? Like that

	bundle.RegisterOptimizator<SameTypeOptimizator<IQuery>>();
	bundle.RegisterOptimizator<SameTypeOptimizator<CreateUser>>();
	bundle.RegisterOptimizator<UserWithEmailExistsOptimizator>();
	bundle.RegisterOptimizator<CreateUserBulkOptimizator>();
	
And well, we can do it. Let's create a memory queue of pending messages & commands. And let force executing of queries & messages after all tasks started. Then each time when we look at the queue we'll find group (more than 1) messages that need be handled. We can easily group similar queries & commands to bulk operations. It has limitations - tasks must be very similar, grouping doesn't works perfectly (for example, first command CreateUser doesn't get grouped with others).

But this allows us to do lot ot interesting things:

1. Group tasks into bulk operations
2. Remove duplicate queries & commands
3. Rewrite partial updates with total update of entity
4. Remove sending requests to database after commands that produces known changes
