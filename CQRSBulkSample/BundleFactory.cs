namespace CQRSBulkSample {
	class BundleFactory {
		public static Bundle CreateBundle() {
			var bundle = new Bundle();

			bundle.RegisterOptimizator<SameTypeOptimizator<IQuery>>();
			bundle.RegisterOptimizator<SameTypeOptimizator<CreateUser>>();

			bundle.RegisterQueryHandler<EmailExistsHandler>();
			bundle.RegisterQueryHandler<GetExistingEmailsHandler>();
			bundle.RegisterOptimizator<UserWithEmailExistsOptimizator>();

			bundle.RegisterCommandHandler<CreateUserHandler>();
			bundle.RegisterCommandHandler<CreateUserBulkHandler>();
			bundle.RegisterOptimizator<CreateUserBulkOptimizator>();

			return bundle;
		}
	}
}
