using MongoDB.Driver;

namespace FileUploadService.Config
{
    public static class DatabaseApplicationContextExtensions
    {
        public static IServiceCollection AddMongoCollectionFromContext<T>(
            this IServiceCollection services,
            Func<IDatabaseApplicationContext, IMongoCollection<T>> collectionAccessor)
        {
            services.AddSingleton<IMongoCollection<T>>(sp =>
            {
                var context = sp.GetRequiredService<IDatabaseApplicationContext>();
                return collectionAccessor(context);
            });

            return services;
        }
    }
}

