using Bit.Api.Implementations.Project;
using Bit.Api.Middlewares.WebApi.OData.ActionFilters;
using Bit.Api.Middlewares.WebApi.OData.Contracts;
using Bit.Api.Middlewares.WebApi.OData.Implementations;
using Bit.Core;
using Bit.Core.Contracts;
using Bit.Core.Contracts.Project;
using Bit.Core.Implementations;
using Bit.Data;
using Bit.Data.Contracts;
using Bit.Data.EntityFrameworkCore.Implementations;
using Bit.Hangfire.Middlewares.JobScheduler.Implementations;
using Bit.Model.Implementations;
using Bit.Owin.Contracts;
using Bit.Owin.Contracts.Metadata;
using Bit.Owin.Implementations;
using Bit.Owin.Implementations.Metadata;
using Bit.Owin.Middlewares;
using Bit.Signalr.Middlewares.Signalr.Implementations;
using Bit.Test;
using Bit.Tests.Api.Implementations.Project;
using Bit.Tests.Data.Implementations;
using Bit.Tests.IdentityServer.Implementations;
using Bit.Tests.Model.Implementations;
using Bit.Tests.Properties;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Http;

namespace Bit.Tests
{
    public class BitOwinTestDependenciesManagerProvider : IDependenciesManager, IDependenciesManagerProvider
    {
        private readonly TestEnvironmentArgs _args;

        public BitOwinTestDependenciesManagerProvider(TestEnvironmentArgs args)
        {
            _args = args;
        }

        protected BitOwinTestDependenciesManagerProvider()
        {

        }

        public virtual void ConfigureDependencies(IDependencyManager dependencyManager)
        {
            dependencyManager.RegisterMinimalDependencies();

            dependencyManager.RegisterInstance(DefaultAppEnvironmentProvider.Current);
            dependencyManager.RegisterInstance(DefaultJsonContentFormatter.Current);
            dependencyManager.RegisterInstance(DefaultPathProvider.Current);

            dependencyManager.Register<ITimeZoneManager, DefaultTimeZoneManager>();
            dependencyManager.Register<IRequestInformationProvider, DefaultRequestInformationProvider>();
            dependencyManager.Register<Microsoft.Owin.Logging.ILoggerFactory, DefaultOwinLoggerFactory>();
            dependencyManager.Register<ILogger, DefaultLogger>();
            dependencyManager.RegisterLogStore<ConsoleLogStore>();
            dependencyManager.RegisterLogStore<DebugLogStore>();
            dependencyManager.Register<IUserInformationProvider, DefaultUserInformationProvider>();
            dependencyManager.Register<IDbConnectionProvider, DefaultSqlDbConnectionProvider>();

            dependencyManager.Register<IDateTimeProvider, DefaultDateTimeProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.Register<IExceptionToHttpErrorMapper, DefaultExceptionToHttpErrorMapper>(lifeCycle: DependencyLifeCycle.SingleInstance);

            dependencyManager.RegisterAppEvents<RazorViewEngineConfiguration>();
            dependencyManager.RegisterAppEvents<InitialTestDataConfiguration>();

            dependencyManager.RegisterOwinMiddleware<StaticFilesMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<AutofacDependencyInjectionMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<OwinExceptionHandlerMiddlewareConfiguration>();
            dependencyManager.RegisterOwinMiddleware<LogRequestInformationMiddlewareConfiguration>();
            dependencyManager.RegisterSingleSignOnClient();
            dependencyManager.RegisterOwinMiddleware<LogUserInformationMiddlewareConfiguration>();

            dependencyManager.RegisterOwinMiddleware<MetadataMiddlewareConfiguration>();

            dependencyManager.RegisterDefaultWebApiConfiguration(AssemblyContainer.Current.GetBitApiAssembly(), AssemblyContainer.Current.GetBitTestsAssembly());

            dependencyManager.RegisterUsing<IOwinMiddlewareConfiguration>(() =>
            {
                return dependencyManager.CreateChildDependencyResolver(childDependencyManager =>
                {
                    childDependencyManager.RegisterGlobalWebApiActionFiltersUsing(httpConfiguration =>
                    {
                        httpConfiguration.Filters.Add(new AuthorizeAttribute());
                    });

                    childDependencyManager.RegisterWebApiMiddlewareUsingDefaultConfiguration("WebApi");

                }).Resolve<IOwinMiddlewareConfiguration>("WebApi");

            }, lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            dependencyManager.RegisterUsing<IOwinMiddlewareConfiguration>(() =>
            {
                return dependencyManager.CreateChildDependencyResolver(childDependencyManager =>
                {
                    childDependencyManager.RegisterGlobalWebApiActionFiltersUsing(httpConfiguration =>
                    {
                        httpConfiguration.Filters.Add(new DefaultODataAuthorizeAttribute());
                    });

                    childDependencyManager.RegisterWebApiODataMiddlewareUsingDefaultConfiguration("WebApiOData");
                    childDependencyManager.RegisterEdmModelProvider<BitEdmModelProvider>();
                    childDependencyManager.RegisterEdmModelProvider<TestEdmModelProvider>();

                }).Resolve<IOwinMiddlewareConfiguration>("WebApiOData");

            }, lifeCycle: DependencyLifeCycle.SingleInstance, overwriteExciting: false);

            dependencyManager.Register<IODataSqlBuilder, DefaultODataSqlBuilder>(lifeCycle: DependencyLifeCycle.SingleInstance);

            dependencyManager.RegisterSignalRConfiguration<SignalRAuthorizeConfiguration>();
            dependencyManager.RegisterSignalRMiddlewareUsingDefaultConfiguration(AssemblyContainer.Current.GetBitSignalRAssembly());

            dependencyManager.RegisterBackgroundJobWorkerUsingDefaultConfiguration<JobSchedulerInMemoryBackendConfiguration>();

            dependencyManager.Register<IAppMetadataProvider, DefaultAppMetadataProvider>(lifeCycle: DependencyLifeCycle.SingleInstance);
            dependencyManager.RegisterMetadata(AssemblyContainer.Current.GetBitMetadataAssembly(), AssemblyContainer.Current.GetBitTestsAssembly(), AssemblyContainer.Current.GetBitIdentityServerAssembly());

            dependencyManager.RegisterGeneric(typeof(IRepository<>).GetTypeInfo(), typeof(TestEfRepository<>).GetTypeInfo(), DependencyLifeCycle.InstancePerLifetimeScope);

            dependencyManager.RegisterGeneric(typeof(IEntityWithDefaultGuidKeyRepository<>).GetTypeInfo(), typeof(TestEfEntityWithDefaultGuidKeyRepository<>).GetTypeInfo(), DependencyLifeCycle.InstancePerLifetimeScope);

            if (Settings.Default.UseInMemoryProviderByDefault)
                dependencyManager.RegisterEfCoreDbContext<TestDbContext, InMemoryDbContextObjectsProvider>();
            else
                dependencyManager.RegisterEfCoreDbContext<TestDbContext, SqlDbContextObjectsProvider>();

            dependencyManager.RegisterDtoModelMapper();

            dependencyManager.RegisterDtoModelMapperConfiguration<DefaultDtoModelMapperConfiguration>();
            dependencyManager.RegisterDtoModelMapperConfiguration<TestDtoModelMapperConfiguration>();

            dependencyManager.RegisterSingleSignOnServer<TestUserService, TestClientProvider>();

            if (_args?.AdditionalDependencies != null)
                _args?.AdditionalDependencies(dependencyManager);

            dependencyManager.RegisterOwinMiddleware<RedirectToSsoIfNotLoggedInMiddlewareConfiguration>();
            dependencyManager.RegisterDefaultPageMiddlewareUsingDefaultConfiguration();
        }

        public virtual IEnumerable<IDependenciesManager> GetDependenciesManagers()
        {
            yield return this;
        }
    }
}
