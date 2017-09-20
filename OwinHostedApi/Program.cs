using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Security.Permissions;
using System.Security.Principal;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dependencies;
using System.Web.Http.Dispatcher;
using System.Web.Http.Filters;
using System.Web.Http.Results;
using System.Web.Http.Routing;
using Microsoft.Owin.Hosting;
using Newtonsoft.Json.Serialization;
using Ninject;
using Ninject.Http;
using Ninject.Modules;
using Ninject.Planning.Bindings;
using Ninject.Syntax;
using Ninject.Web.Common.OwinHost;
using Ninject.Web.WebApi;
using Ninject.Web.WebApi.OwinHost;
using Owin;
using OwinHostedApi;
using OwinHostedApi.api;

namespace OwinHostedApi
{
    public interface ISomething
    {
        int Id { get; set; }
    }

    public class Something : ISomething
    {
        public int Id
        {
            get { return 5; }
            set
            {

            }
        }
    }

    public class SomethingDependencyScope : IDependencyScope
    {
        public void Dispose()
        {
            throw new NotImplementedException();
        }

        public object GetService(Type serviceType)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            throw new NotImplementedException();
        }
    }

    public class SomethingDependencyResolver : IDependencyResolver
    {

        public void Dispose()
        {
            return;
        }

        public object GetService(Type serviceType)
        {
            return null;
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            return Enumerable.Empty<object>();
        }

        public IDependencyScope BeginScope()
        {
            return this;
        }
    }

    public class MyIdentity : IPrincipal
    {
        public MyIdentity(string name)
        {
            Identity = new GenericIdentity(name);
        }
        public bool IsInRole(string role)
        {
            if (!Identity.IsAuthenticated)
            {
                return false;
            }
            return false;
        }

        public IIdentity Identity { get; }
    }

    public class MyAuthenticationFilter : IAuthenticationFilter
    {
        public bool AllowMultiple { get; }
        public Task AuthenticateAsync(HttpAuthenticationContext context, CancellationToken cancellationToken)
        {

            context.Principal = new MyIdentity("somethingz");
            return Task.Delay(10);

            var authorization = context.Request.Headers.Authorization;

            context.ErrorResult =
                new UnauthorizedResult(
                    new List<AuthenticationHeaderValue>()
                    {
                        new AuthenticationHeaderValue("something", "PARAM")
                    }, context.Request);
            return Task.Delay(10);
        }

        public Task ChallengeAsync(HttpAuthenticationChallengeContext context, CancellationToken cancellationToken)
        {
            context.Result = new ActionResultDelegate(context.Result, async (token, result) =>
            {
                var res = await result.ExecuteAsync(token);
                if (res.StatusCode == HttpStatusCode.Unauthorized)
                {
                    res.Headers.WwwAuthenticate.Add(new AuthenticationHeaderValue("Basic", "realm=realm"));
                }
                return res;
            });
            return Task.FromResult<object>(null);
        }
    }

    public class ActionResultDelegate : IHttpActionResult
    {
        private IHttpActionResult _next;
        private Func<CancellationToken, IHttpActionResult, Task<HttpResponseMessage>> _func;

        public ActionResultDelegate(IHttpActionResult next,
            Func<CancellationToken, IHttpActionResult, Task<HttpResponseMessage>> func)
        {
            _next = next;
            _func = func;
        }
        public Task<HttpResponseMessage> ExecuteAsync(CancellationToken cancellationToken)
        {
            return _func(cancellationToken, _next);
        }
    }

    //public class MyActionFilter : IActionFilter
    //{
    //    public bool AllowMultiple { get; }
    //    public Task<HttpResponseMessage> ExecuteActionFilterAsync(HttpActionContext actionContext, CancellationToken cancellationToken, Func<Task<HttpResponseMessage>> continuation)
    //    {
    //        return new Task<HttpResponseMessage>;
    //    }
    //}

    public class MyAuthorizationFilter : IAuthorizationFilter
    {
        public bool AllowMultiple { get; }

        public Task<HttpResponseMessage> ExecuteAuthorizationFilterAsync(HttpActionContext actionContext, CancellationToken cancellationToken,
            Func<Task<HttpResponseMessage>> continuation)
        {

            throw new NotImplementedException();

        }

    }

    public class MyExceptionFilter : IExceptionFilter
    {
        public bool AllowMultiple { get; }
        public Task ExecuteExceptionFilterAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
        {
            return Task.Delay(10);
        }
    }

    public class SpecialDelegatingHandler : DelegatingHandler
    {

        private async Task<HttpResponseMessage> GoAway()
        {
            var result = new HttpResponseMessage(HttpStatusCode.Unauthorized);
            result.Headers.WwwAuthenticate.Add(new AuthenticationHeaderValue("Basic"));
            return await new Task<HttpResponseMessage>(() => result);
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (request.Headers.Authorization != null && request.Headers.Authorization.Scheme == "Basic")
            {
                var content = Encoding.UTF8.GetString(Convert.FromBase64String(request.Headers.Authorization.Parameter));
                return await base.SendAsync(request, cancellationToken);
            }
            return await GoAway();
            //return base.SendAsync(request, cancellationToken);
        }
    }

    public class Config
    {
        public void Configuration(IAppBuilder app)
        {
            var config = new HttpConfiguration();
            config.MapHttpAttributeRoutes();

            config.Routes.MapHttpRoute(name: "routemy",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional },
                handler: new SpecialDelegatingHandler(), constraints: null);

            config.Formatters.JsonFormatter.SupportedMediaTypes.Add(new MediaTypeHeaderValue("text/html"));
            config.Formatters.JsonFormatter.SerializerSettings.ContractResolver =
                new CamelCasePropertyNamesContractResolver();

            //config.MessageHandlers.Add(new SpecialDelegatingHandler());


            var pipeline = HttpClientFactory.CreatePipeline(new HttpControllerDispatcher(config), new List<DelegatingHandler>()
            {
                new SpecialDelegatingHandler()
            });

            config.Routes.MapHttpRoute(name: "specialroute",
                routeTemplate: "api2/{controller}/{id}",
                defaults: new {id = RouteParameter.Optional},
                constraints: null,
                handler: pipeline);

            config.Filters.Add(new MyAuthenticationFilter());

            app.UseWebApi(config);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            using (WebApp.Start<Config>("https://awa.localhost:30213"))
            {
                Console.WriteLine("Press a key to stop the webapp.");
                Console.ReadKey();
            }
        }
    }
}
