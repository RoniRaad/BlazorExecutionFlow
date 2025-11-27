using BlazorExecutionFlow.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BlazorExecutionFlow.Extensions
{
    /// <summary>
    /// Extension methods for IServiceCollection to configure BlazorExecutionFlow services.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Adds BlazorExecutionFlow services to the service collection.
        /// This includes workflow management, environment variables, and user prompts.
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="configure">Optional configuration action</param>
        /// <returns>The service collection for chaining</returns>
        public static IServiceCollection AddBlazorExecutionFlow(
            this IServiceCollection services,
            Action<BlazorExecutionFlowOptions>? configure = null)
        {
            var options = new BlazorExecutionFlowOptions();
            configure?.Invoke(options);

            // Register UserPromptService (always singleton)
            services.AddSingleton<IUserPromptService, UserPromptService>();
            services.AddSingleton<UserPromptService>(sp => (UserPromptService)sp.GetRequiredService<IUserPromptService>());

            // Register WorkflowService
            if (options.WorkflowServiceFactory != null)
            {
                // Custom implementation provided
                services.AddSingleton(options.WorkflowServiceFactory);
            }
            else
            {
                // Use file-based implementation
                services.AddSingleton<IWorkflowService>(sp =>
                {
                    var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<FileBasedWorkflowService>>();
                    return new FileBasedWorkflowService(options.WorkflowStorageDirectory, logger);
                });
            }

            // Register EnvironmentVariablesService
            if (options.EnvironmentVariablesServiceFactory != null)
            {
                // Custom implementation provided
                services.AddSingleton(options.EnvironmentVariablesServiceFactory);
            }
            else
            {
                // Use file-based implementation
                services.AddSingleton<IEnvironmentVariablesService>(sp =>
                {
                    var logger = sp.GetService<Microsoft.Extensions.Logging.ILogger<FileBasedEnvironmentVariablesService>>();
                    return new FileBasedEnvironmentVariablesService(options.EnvironmentVariablesFilePath, logger);
                });
            }

            return services;
        }

        /// <summary>
        /// Adds BlazorExecutionFlow services with custom service implementations.
        /// </summary>
        public static IServiceCollection AddBlazorExecutionFlow<TWorkflowService, TEnvironmentService>(
            this IServiceCollection services)
            where TWorkflowService : class, IWorkflowService
            where TEnvironmentService : class, IEnvironmentVariablesService
        {
            services.AddSingleton<IUserPromptService, UserPromptService>();
            services.AddSingleton<UserPromptService>(sp => (UserPromptService)sp.GetRequiredService<IUserPromptService>());
            services.AddSingleton<IWorkflowService, TWorkflowService>();
            services.AddSingleton<IEnvironmentVariablesService, TEnvironmentService>();

            return services;
        }
    }

    /// <summary>
    /// Configuration options for BlazorExecutionFlow services.
    /// </summary>
    public class BlazorExecutionFlowOptions
    {
        /// <summary>
        /// Directory where workflow JSON files will be stored.
        /// Default: "./Data/Workflows"
        /// </summary>
        public string WorkflowStorageDirectory { get; set; } = Path.Combine("Data", "Workflows");

        /// <summary>
        /// File path where environment variables will be stored.
        /// Default: "./Data/environment-variables.json"
        /// </summary>
        public string EnvironmentVariablesFilePath { get; set; } = Path.Combine("Data", "environment-variables.json");

        /// <summary>
        /// Optional factory for providing a custom IWorkflowService implementation.
        /// If null, FileBasedWorkflowService will be used.
        /// </summary>
        public Func<IServiceProvider, IWorkflowService>? WorkflowServiceFactory { get; set; }

        /// <summary>
        /// Optional factory for providing a custom IEnvironmentVariablesService implementation.
        /// If null, FileBasedEnvironmentVariablesService will be used.
        /// </summary>
        public Func<IServiceProvider, IEnvironmentVariablesService>? EnvironmentVariablesServiceFactory { get; set; }
    }
}
