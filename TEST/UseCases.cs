/********************************************************************************
* UseCases.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    [TestFixture]
    public class UseCases
    {
        [Test]
        public async Task Calculator()
        {
            using HttpListener listener = new();

            listener.Prefixes.Add("http://localhost:8080/");

            RouterBuilder routerBuilder = new((object? state, string _) =>
            {
                HttpListenerContext ctx = (HttpListenerContext) state!;
                ctx.Response.StatusCode = (int) HttpStatusCode.NotFound;
                ctx.Response.Close();
                return true;
            });

            routerBuilder.AddRoute("{a:int}/add/{b:int}", (IReadOnlyDictionary<string, object?> paramz, object? state, string _) =>
            {
                HttpListenerContext ctx = (HttpListenerContext) state!;
                ctx.Response.StatusCode = (int) HttpStatusCode.OK;
                ctx.Response.ContentType = "application/json";

                using StreamWriter streamWriter = new(ctx.Response.OutputStream);
                streamWriter.Write(JsonSerializer.Serialize((int) paramz["a"]! + (int) paramz["b"]!));
                streamWriter.Close();

                ctx.Response.Close();
                return true;
            });

            Router router = routerBuilder.Build();

            listener.Start();

            _ = Task.Factory.StartNew(() =>
            {
                while (listener.IsListening)
                {
                    try
                    {
                        HttpListenerContext state = listener.GetContext();
                        router(state, state.Request.Url.AbsolutePath);
                    }
                    catch (HttpListenerException e)
                    {
                        if (e.ErrorCode == 995) // listener.Stop() has been called
                            return;

                        Debug.WriteLine(e);                      
                    }
                }
            });

            using HttpClient client = new();

            HttpResponseMessage resp = await client.GetAsync("http://localhost:8080/");
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            resp = await client.GetAsync("http://localhost:8080/1/add/2");
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            using Stream stm = await resp.Content.ReadAsStreamAsync();
            Assert.That(await JsonSerializer.DeserializeAsync<int>(stm), Is.EqualTo(3));

            listener.Stop();
        }
    }
}
