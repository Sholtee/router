/********************************************************************************
* RouteTemplateCompilerTests.cs                                                 *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System;
using System.Collections.Generic;

using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using Properties;

    [TestFixture]
    public class RouteTemplateCompilerTests
    {
        [TestCase("/", null, "/")]
        [TestCase("/cica", null, "/cica")]
        [TestCase("{param:int}", 1986, "/1986")]
        [TestCase("/{param:int}", 1986, "/1986")]
        [TestCase("/cica/{param:int}", 1986, "/cica/1986")]
        [TestCase("/{param:int}/cica", 1986, "/1986/cica")]
        [TestCase("/cica/{param:int}/kutya", 1986, "/cica/1986/kutya")]
        [TestCase("/cica/pre-{param:int}/kutya", 1986, "/cica/pre-1986/kutya")]
        [TestCase("/cica/{param:int}-su/kutya", 1986, "/cica/1986-su/kutya")]
        [TestCase("/cica/pre-{param:int}-su/kutya", 1986, "/cica/pre-1986-su/kutya")]
        public void CompilerShouldSubstitute(string template, object val, string expected)
        {
            RouteTemplateCompiler compile = RouteTemplate.CreateCompiler(template);
            Assert.That(compile(new Dictionary<string, object?> { { "param", val } }), Is.EqualTo(expected));
        }

        [Test]
        public void CompilerShouldThrowOnMissingParameter()
        {
            RouteTemplateCompiler compile = RouteTemplate.CreateCompiler("/{param:int}/cica");

            Assert.Throws<ArgumentException>(() => compile(new Dictionary<string, object?> { }), Resources.INAPPROPRIATE_PARAMETERS);
        }

        [Test]
        public void CompilerShouldThrowOnInappropriateParameter()
        {
            RouteTemplateCompiler compile = RouteTemplate.CreateCompiler("/{param:int}/cica");

            Assert.Throws<ArgumentException>(() => compile(new Dictionary<string, object?> { { "param", "string" } }), Resources.INAPPROPRIATE_PARAMETERS);
        }
    }
}