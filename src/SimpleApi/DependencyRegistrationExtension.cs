using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

namespace SimpleApi
{
    public static class DependencyRegistrationExtension
    {
        public static IApplicationBuilder RunStartupTasks(this IApplicationBuilder app)
        {
            if (app == null) throw new ArgumentNullException(nameof(app));

            using (var serviceScope = app.ApplicationServices.CreateScope())
            {
                var startupTasks = serviceScope.ServiceProvider.GetServices<IStartupTask>();

                foreach (var startupTask in startupTasks)
                {
                    startupTask.ExecuteAsync();
                }
            }

            return app;
        }
    }
}