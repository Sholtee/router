/********************************************************************************
* UseCases.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;

using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using DI;
    using DI.Interfaces;
    using Extensions;

    using ResponseData = (HttpStatusCode Status, object? Body);

    public enum ArithmeticalOperation
    {
        Add = 1,
        Subtract = -1
    }

    public abstract class UseCaseTestsBase
    {
        protected HttpListener Listener { get; private set; } = null!;

        protected HttpClient Client { get; private set; } = null!;

        protected static HttpListener CreateListener(Action<HttpListenerContext> callback)
        {
            HttpListener listener = new();
            listener.Prefixes.Add("http://localhost:8080/");
            listener.Start();

            Task.Factory.StartNew(() =>
            {
                while (listener.IsListening)
                {
                    try
                    {
                        HttpListenerContext context = listener.GetContext();
                        callback(context);
                        context.Response.Close();
                    }
                    catch (HttpListenerException e)
                    {
                        if (e.ErrorCode == 995) // listener.Stop() has been called
                            return;

                        Debug.WriteLine(e);
                    }
                }
            }, TaskCreationOptions.LongRunning);

            return listener;
        }

        protected abstract HttpListener SetupServer();

        [OneTimeSetUp]
        public virtual void SetupFixture()
        {
            Listener = SetupServer();
            Client = new HttpClient();
        }

        [OneTimeTearDown]
        public virtual void TearDownFixture()
        {
            Listener?.Close();
            Listener = null!;

            Client?.Dispose();
            Client = null!;
        }
    }

    [TestFixture]
    public class UseCaseBasic: UseCaseTestsBase
    {
        #region Helpers
        private const string RouteTemplate = "{a:int}/{op:enum:Solti.Utils.Router.Tests.ArithmeticalOperation}/{b:int}";

        protected override HttpListener SetupServer()
        {
            Router router = SetupRouter();

            return CreateListener(context =>
            {
                ResponseData data;

                try
                {
                    data = (ResponseData) router(context.Request, context.Request.Url!.AbsolutePath.AsSpan(), context.Request.HttpMethod.AsSpan())!;
                }
                catch (Exception e)
                {
                    data = new ResponseData(HttpStatusCode.InternalServerError, e.Message);
                }

                context.Response.StatusCode = (int)data.Status;
                context.Response.ContentType = "application/json";

                using StreamWriter streamWriter = new(context.Response.OutputStream);
                streamWriter.Write(JsonSerializer.Serialize(data.Body));
            });
        }

        private static Router SetupRouter()
        {
            RouterBuilder routerBuilder = new
            (
                handler: static (object? state, HttpStatusCode reason) => new ResponseData(reason, reason.ToString())
            );

            routerBuilder.AddRoute
            (
                RouteTemplate,
                handler: static (IReadOnlyDictionary<string, object?> paramz, object? state) => new ResponseData
                (
                    HttpStatusCode.OK,
                    (int) paramz["a"]! + (int) paramz["op"]! * (int) paramz["b"]!
                )
            );

            return routerBuilder.Build();
        }
        #endregion

        [Test]
        public async Task Calculator_InvalidRoute()
        {
            using HttpResponseMessage resp = await Client.GetAsync("http://localhost:8080/");
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            using Stream stm = await resp.Content.ReadAsStreamAsync();
            Assert.That(await JsonSerializer.DeserializeAsync<string>(stm), Is.EqualTo("NotFound"));
        }

        [Test]
        public async Task Calculator_InvalidMethod()
        {
            RouteTemplateCompiler getRoute = Utils.Router.RouteTemplate.CreateCompiler
            (
                "http://localhost:8080/" + RouteTemplate
            );

            using HttpResponseMessage resp = await Client.PostAsync
            (
                getRoute
                (
                    new Dictionary<string, object?>
                    {
                        { "a", 1 },
                        { "op", ArithmeticalOperation.Add },
                        { "b", 2 }
                    }
                ),
                JsonContent.Create(new object())
            );
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.MethodNotAllowed));

            using Stream stm = await resp.Content.ReadAsStreamAsync();
            Assert.That(await JsonSerializer.DeserializeAsync<string>(stm), Is.EqualTo("MethodNotAllowed"));
        }

        [Test]
        public async Task Calculator()
        {
            RouteTemplateCompiler getRoute = Utils.Router.RouteTemplate.CreateCompiler
            (
                "http://localhost:8080/" + RouteTemplate
            );

            using HttpResponseMessage resp = await Client.GetAsync
            (
                getRoute
                (
                    new Dictionary<string, object?>
                    {
                        { "a", 1 },
                        { "op", ArithmeticalOperation.Add },
                        { "b", 2 }
                    }
                )
            );
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            using Stream stm = await resp.Content.ReadAsStreamAsync();
            Assert.That(await JsonSerializer.DeserializeAsync<int>(stm), Is.EqualTo(3));
        }
    }

    public abstract class UseCaseIOCBase<TRootScope, TScope>: UseCaseTestsBase where TRootScope: class, IDisposable
    {
        #region Helpers
        protected sealed class CalculatorService
        {
            public int Calculate(int a, ArithmeticalOperation op, int b) => a + (int) op * b;

            public void ErrorMethod() => throw new Exception("This is the end");
        }

        private const string RouteTemplate = "{a:int}/{op:enum:Solti.Utils.Router.Tests.ArithmeticalOperation}/{b:int}";

        protected TRootScope RootScope { get; private set; } = null!;

        protected override HttpListener SetupServer()
        {
            AsyncRouter router = SetupRouter();

            return CreateListener(context =>
            {
                using (CreateScope(out TScope scope))
                {
                    object? response = router(scope, context.Request.Url!.AbsolutePath.AsSpan(), context.Request.HttpMethod.AsSpan())
                        .GetAwaiter()
                        .GetResult()!;

                    context.Response.ContentType = "application/json";

                    if (response is ResponseData responseData)
                    {
                        context.Response.StatusCode = (int) responseData.Status;
                        response = responseData.Body;
                    }
                    else
                    {
                        context.Response.StatusCode = (int) HttpStatusCode.OK;
                    }

                    using StreamWriter streamWriter = new(context.Response.OutputStream);
                    streamWriter.Write(JsonSerializer.Serialize(response));
                }
            });
        }

        private static AsyncRouter SetupRouter()
        {
            AsyncRouterBuilder routerBuilder = AsyncRouterBuilder.Create
            (
                handler: static (object? state, HttpStatusCode reason) => new ResponseData(reason, reason.ToString())
            );

            routerBuilder.AddRoute<CalculatorService>
            (
                RouteTemplate,
                calc => calc.Calculate(0, default, 0)
            );
            routerBuilder.AddRoute<CalculatorService>("/error", calc => calc.ErrorMethod());
            routerBuilder.RegisterExceptionHandler<Exception, ResponseData>(handler: (_, exc) => new ResponseData(HttpStatusCode.InternalServerError, exc.Message));

            return routerBuilder.Build();
        }

        protected abstract TRootScope SetupIOC();

        protected abstract IDisposable CreateScope(out TScope scope);

        [OneTimeSetUp]
        public override void SetupFixture()
        {
            RootScope = SetupIOC();
            base.SetupFixture();
        }

        [OneTimeTearDown]
        public override void TearDownFixture()
        {
            base.TearDownFixture();

            RootScope?.Dispose();
            RootScope = null!;
        }
        #endregion

        [Test]
        public async Task Calculator_InvalidRoute()
        {
            using (HttpResponseMessage resp = await Client.GetAsync("http://localhost:8080/"))
            {
                Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

                using Stream stm = await resp.Content.ReadAsStreamAsync();
                Assert.That(await JsonSerializer.DeserializeAsync<string>(stm), Is.EqualTo("NotFound"));
            }

            using (HttpResponseMessage resp = await Client.GetAsync("http://localhost:8080/10"))
            {
                Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

                Stream stm = await resp.Content.ReadAsStreamAsync();
                Assert.That(await JsonSerializer.DeserializeAsync<string>(stm), Is.EqualTo("NotFound"));
            }
        }

        [Test]
        public async Task Calculator_InternalError()
        {
            using HttpResponseMessage resp = await Client.GetAsync("http://localhost:8080/error");
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.InternalServerError));

            using Stream stm = await resp.Content.ReadAsStreamAsync();
            Assert.That(await JsonSerializer.DeserializeAsync<string>(stm), Is.EqualTo("This is the end"));
        }

        [Test]
        public async Task Calculator_InvalidMethod()
        {
            RouteTemplateCompiler getRoute = Utils.Router.RouteTemplate.CreateCompiler("http://localhost:8080/" + RouteTemplate);

            using HttpResponseMessage resp = await Client.PostAsync
            (
                getRoute
                (
                    new Dictionary<string, object?>
                    {
                        { "a", 1 },
                        { "op", ArithmeticalOperation.Add },
                        { "b", 2 }
                    }
                ),
                JsonContent.Create(new object())
            );
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.MethodNotAllowed));

            using Stream stm = await resp.Content.ReadAsStreamAsync();
            Assert.That(await JsonSerializer.DeserializeAsync<string>(stm), Is.EqualTo("MethodNotAllowed"));
        }

        [Test]
        public async Task Calculator()
        {
            RouteTemplateCompiler getRoute = Utils.Router.RouteTemplate.CreateCompiler("http://localhost:8080/" + RouteTemplate);

            using HttpResponseMessage resp = await Client.GetAsync
            (
                getRoute
                (
                    new Dictionary<string, object?>
                    {
                        { "a", 1 },
                        { "op", ArithmeticalOperation.Add },
                        { "b", 2 }
                    }
                )
            );
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            using Stream stm = await resp.Content.ReadAsStreamAsync();
            Assert.That(await JsonSerializer.DeserializeAsync<int>(stm), Is.EqualTo(3));
        }
    }

    [TestFixture]
    public class UseCaseIOC_MsDI : UseCaseIOCBase<ServiceProvider, IServiceProvider>
    {
        protected override ServiceProvider SetupIOC()
        {
            Microsoft.Extensions.DependencyInjection.ServiceCollection services = new();
            services.AddScoped<CalculatorService>();
            return services.BuildServiceProvider();
        }

        protected override IDisposable CreateScope(out IServiceProvider scope)
        {
            IServiceScope serviceScope = RootScope.CreateScope();
            scope = serviceScope.ServiceProvider;
            return serviceScope;
        }
    }

    [TestFixture]
    public class UseCaseIOC_InjectorDotNet: UseCaseIOCBase<IScopeFactory, IInjector>
    {
        private RequestHandlerBuilder OldBuilder { get; set; } = null!;

        public override void SetupFixture()
        {
            OldBuilder = AsyncRouterBuilderAddRouteExtensions.RequestHandlerBuilder;
            AsyncRouterBuilderAddRouteExtensions.RequestHandlerBuilder = new InjectorDotNetRequestHandlerBuilder();
            base.SetupFixture();
        }

        public override void TearDownFixture()
        {
            base.TearDownFixture();
            AsyncRouterBuilderAddRouteExtensions.RequestHandlerBuilder = OldBuilder;
        }

        protected override IScopeFactory SetupIOC() => ScopeFactory.Create(svcs => svcs.Service<CalculatorService>(Lifetime.Scoped));

        protected override IDisposable CreateScope(out IInjector scope)
        {
            scope = RootScope.CreateScope();
            return scope;
        }
    }

    [TestFixture]
    public class UseCaseIOC_BodyParameter: UseCaseTestsBase
    {
        #region Helpers
        private sealed class RequestHandlerBuilderSupportsBodyParameter : InjectorDotNetRequestHandlerBuilder
        {
            protected internal override Expression GetInvokeServiceArgument(ParameterInfo param, ParsedRoute route, IReadOnlyDictionary<string, int> shortcuts, object? userData)
            {
                return param.Name == "body"
                    ? Expression.Invoke(Expression.Constant((Func<object, string>) GetBody), UserData)
                    : base.GetInvokeServiceArgument(param, route, shortcuts, userData);

                static string GetBody(object userData)
                {
                    IInjector scope = (IInjector) userData;

                    using StreamReader rdr = new(scope.Get<HttpListenerContext>().Request.InputStream);
                    return rdr.ReadToEnd();
                }
            }
        }

        private sealed class ConverterService
        {
            public string ToUpperCase(string body) => body.ToUpper();
        }

        private RequestHandlerBuilder OldBuilder { get; set; } = null!;

        private IScopeFactory RootScope { get; set; } = null!;

        protected override HttpListener SetupServer()
        {
            AsyncRouter router = SetupRouter();

            return CreateListener(context =>
            {
                using IInjector scope = RootScope.CreateScope();
                scope.AssignScopeLocal(context);

                object? response = router(scope, context.Request.Url!.AbsolutePath.AsSpan(), context.Request.HttpMethod.AsSpan())
                    .GetAwaiter()
                    .GetResult()!;

                context.Response.ContentType = "text/html";

                if (response is ResponseData responseData)
                {
                    context.Response.StatusCode = (int) responseData.Status;
                    response = responseData.Body;
                }
                else
                {
                    context.Response.StatusCode = (int) HttpStatusCode.OK;
                }

                using StreamWriter streamWriter = new(context.Response.OutputStream);
                streamWriter.Write((string) response!);
            });
        }

        private static AsyncRouter SetupRouter()
        {
            AsyncRouterBuilder routerBuilder = AsyncRouterBuilder.Create
            (
                handler: static (object? state, HttpStatusCode reason) => new ResponseData(reason, reason.ToString())
            );

            routerBuilder.AddRoute<ConverterService>("/upper", conv => conv.ToUpperCase(default!), "POST");
            routerBuilder.RegisterExceptionHandler<Exception, ResponseData>(handler: (_, exc) => new ResponseData(HttpStatusCode.InternalServerError, exc.Message));

            return routerBuilder.Build();
        }

        private static IScopeFactory SetupIOC() => ScopeFactory.Create
        (
            svcs => svcs
                .Service<ConverterService>(Lifetime.Scoped)
                .SetupScopeLocal<HttpListenerContext>()
        );

        private async Task<HttpResponseMessage> PostRequest(string msg)
        {
            using MemoryStream input = new();
            using StreamWriter sw = new(input);

            sw.Write("cica");
            sw.Flush();

            input.Seek(0, SeekOrigin.Begin);

            return await Client.PostAsync("http://localhost:8080/upper", new StreamContent(input));
        }
      
        [OneTimeSetUp]
        public override void SetupFixture()
        {
            OldBuilder = AsyncRouterBuilderAddRouteExtensions.RequestHandlerBuilder;
            AsyncRouterBuilderAddRouteExtensions.RequestHandlerBuilder = new RequestHandlerBuilderSupportsBodyParameter();

            RootScope = SetupIOC();
            base.SetupFixture();
        }

        [OneTimeTearDown]
        public override void TearDownFixture()
        {
            base.TearDownFixture();

            AsyncRouterBuilderAddRouteExtensions.RequestHandlerBuilder = OldBuilder;

            RootScope?.Dispose();
            RootScope = null!;
        }
        #endregion

        [Test]
        public async Task ToUpper()
        {
            using HttpResponseMessage resp = await PostRequest("cica");
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            using Stream output = await resp.Content.ReadAsStreamAsync();
            using StreamReader sr = new(output);
            Assert.That(sr.ReadToEnd(), Is.EqualTo("CICA"));
        }
    }
}
