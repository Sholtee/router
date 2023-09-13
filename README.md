# Compass.NET [![Build status](https://ci.appveyor.com/api/projects/status/uq0ep9idk7rw8ogr?svg=true)](https://ci.appveyor.com/project/Sholtee/router) ![AppVeyor tests](https://img.shields.io/appveyor/tests/sholtee/router/main) [![Coverage Status](https://coveralls.io/repos/github/Sholtee/router/badge.svg?branch=main)](https://coveralls.io/github/Sholtee/router?branch=main) ![GitHub last commit (branch)](https://img.shields.io/github/last-commit/sholtee/router/main) [![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/compass.net)](https://www.nuget.org/packages/compass.net)
> Simple HTTP request router for .NET backends

**This documentation refers the v1.X of the library**

## How to use
This library comes with an extremely simple API set (consits of a few methods only)

1. Register the known routes 
	```csharp
	using System.Net;
	using Solti.Utils.Router;

	RouterBuilder routerBuilder = new
	(
		// This handler is called on every unknown routes
		defaultHandler: (object? state) =>
		{
			HttpListenerContext ctx = (HttpListenerContext) state;
			...
		},
		// can be omitted
		converters: DefaultConverters.Instance
	);
	routerBuilder.AddRoute
	(
		// A route may contain parameter(s)
		route: "/get/picture-{id:int}",
		handler: (IReadOnlyDictionary<string, object?> paramz, object? state) =>
		{
			HttpListenerContext ctx = (HttpListenerContext) state;
			int id = (int) paramz["id"];
			...
		},
		// "GET" is the default
		"GET", "OPTIONS"
	);
	```
	A valid route looks like `[/]segment1/[prefix]{paramName:converter[:style]}[suffix]/segment3[/]`

2. Build the router delegate and start the HTTP backend
	```csharp
	Router route = routerBuilder.Build();

	HttpListener listener = new HttpListener();
	listener.Prefixes.Add("http://localhost:8080/");
	listener.Start();
	...
	while (Listener.IsListening)  // probably this will run in a separate thread
	{
		HttpListenerContext context = Listener.GetContext();
		route(context, context.Request.Url!.AbsolutePath, context.Request.HttpMethod);
	}
	```

For a more comprehensive example check out the [use cases](https://github.com/Sholtee/router/blob/main/TEST/UseCases.cs ) fixture

## Converters
Converters are used to parse variable value coming from the request path. Default converters (`int`, `guid`, `str` and `enum`) can be accessed via the `DefaultConverters.Instance` property.
```csharp
using Solti.Utils.Router;

RouterBuilder routerBuilder = new
(
	defaultHandler: (object? state) => {...},
	converters: new Dictionary<string, ConverterFactory>(DefaultConverters.Instance)
	{
		{"mytype", (string? style) => new MyConverter(style)}
	}
);

class MyTypeConverter: IConverter 
{
    ...
}
```

## Building routes from template
```csharp
using Solti.Utils.Router;

RouteTemplateCompiler compile = RouteTemplate.CreateCompiler("https://localhost:8080/get/picture-{id:int}");
string route = compile(new Dictionary<string, object?> { { "id", 1986 } });  // route == "https://localhost:8080/get/picture-1986"
...
```

## Resources
- [API Docs](https://sholtee.github.io/router )
- [Benchmark Results](https://sholtee.github.io/router/perf )
- [Version History](https://github.com/Sholtee/router/blob/master/history.md )

## Supported frameworks
This project currently targets *.NET Standard* 2.0 and 2.1.