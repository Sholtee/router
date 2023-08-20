/********************************************************************************
* UseCases.cs                                                                   *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
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

            RouterBuilder routerBuilder = new(static (object? state) => (HttpStatusCode.NotFound, (object) "Not Found"));

            routerBuilder.AddRoute("{a:int}/add/{b:int}", static (IReadOnlyDictionary<string, object?> paramz, object? state) => 
            (
                HttpStatusCode.OK,
                (object) ((int) paramz["a"]! + (int) paramz["b"]!))
            );

            Router router = routerBuilder.Build();

            listener.Start();

            _ = Task.Factory.StartNew(() =>
            {
                while (listener.IsListening)
                {
                    try
                    {
                        HttpListenerContext context = listener.GetContext();

                        (HttpStatusCode Status, object? Body) data;

                        try
                        {
                            data = ((HttpStatusCode Status, object? Body)) router
                            (
                                context.Request,
                                context.Request.Url!.AbsolutePath,
                                context.Request.HttpMethod
                            )!;
                        }
                        catch (Exception e)
                        {
                            SendReponse((HttpStatusCode.InternalServerError, e.Message));
                            continue;
                        }

                        SendReponse(data);

                        void SendReponse((HttpStatusCode Status, object? Body) data)
                        {
                            context.Response.StatusCode = (int) data.Status;
                            context.Response.ContentType = "application/json";

                            using (StreamWriter streamWriter = new(context.Response.OutputStream))
                            {
                                streamWriter.Write(JsonSerializer.Serialize(data.Body));
                            }

                            context.Response.Close();
                        }
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

            resp = await client.PostAsync("http://localhost:8080/1/add/2", JsonContent.Create(new object()));
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.NotFound));

            resp = await client.GetAsync("http://localhost:8080/1/add/2");
            Assert.That(resp.StatusCode, Is.EqualTo(HttpStatusCode.OK));

            using Stream stm = await resp.Content.ReadAsStreamAsync();
            Assert.That(await JsonSerializer.DeserializeAsync<int>(stm), Is.EqualTo(3));

            listener.Stop();
        }
    }
}
