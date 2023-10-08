/********************************************************************************
* LookupBuilderTests.cs                                                         *
*                                                                               *
* Author: Denes Solti                                                           *
********************************************************************************/
using System.Collections.Generic;
using System.Linq;

using NUnit.Framework;

namespace Solti.Utils.Router.Tests
{
    using Internals;
    using Primitives;

    [TestFixture]
    public class StaticDictionaryTests
    {
        DelegateCompiler Compiler { get; set; } = null!;

        [SetUp]
        public void SetupTest() => Compiler = new DelegateCompiler();

        [Test]
        public void Get_ShouldThrowOnMissingKey()
        {
            StaticDictionaryBuilder builder = new();
            StaticDictionary dict = builder.CreateFactory(Compiler).Invoke();
            Compiler.Compile();
            Assert.Throws<KeyNotFoundException>(() => _ = dict["key"]);
        }

        [Test]
        public void TryGet_ShouldFailOnMissingKey()
        {
            StaticDictionaryBuilder builder = new();
            StaticDictionary dict = builder.CreateFactory(Compiler).Invoke();
            Compiler.Compile();
            Assert.False(dict.TryGetValue("key", out _));
        }

        [Test]
        public void Get_ShouldThrowOnUnassignedKey()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler).Invoke();
            Compiler.Compile();
            Assert.Throws<KeyNotFoundException>(() => _ = dict["key"]);
        }

        [Test]
        public void TryGet_ShouldFailOnUnassignedKey()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler).Invoke();
            Compiler.Compile();
            Assert.False(dict.TryGetValue("key", out _));
        }

        [Test]
        public void Keys_ShouldNotReturnUnassignedKeys()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler).Invoke();
            Compiler.Compile();
            Assert.That(dict.Keys, Is.Empty);
        }

        [Test]
        public void Values_ShouldNotReturnUnassignedValues()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler).Invoke();
            Compiler.Compile();
            Assert.That(dict.Values, Is.Empty);
        }

        [Test]
        public void Count_ShouldNotTakeUnassignedValuesIntoAccount()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler).Invoke();
            Compiler.Compile();
            Assert.That(dict.Count, Is.Zero);
        }

        [Test]
        public void Enumerator_ShouldNotReturnUnassignedValues()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler).Invoke();
            Compiler.Compile();
            Assert.That(dict.ToList(), Is.Empty);
        }

        [Test]
        public void Get_ShouldReturnTheCorrectValue()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler).Invoke();
            Compiler.Compile();
            dict.Add("key", "value");
            Assert.That(dict["key"], Is.EqualTo("value"));
        }

        [Test]
        public void TryGet_ShouldReturnTheCorrectValue()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler).Invoke();
            Compiler.Compile();
            dict.Add("key", "value");
            Assert.That(dict.TryGetValue("key", out object? val));
            Assert.That(val, Is.EqualTo("value"));
        }

        [Test]
        public void Keys_ShouldReturnAssignedKeys()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler).Invoke();
            Compiler.Compile();
            dict.Add("key", "value");
            Assert.That(dict.Keys.Single(), Is.EqualTo("key"));
        }

        [Test]
        public void VAlues_ShouldReturnAssignedValues()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler).Invoke();
            Compiler.Compile();
            dict.Add("key", "value");
            Assert.That(dict.Values.Single(), Is.EqualTo("value"));
        }

        [Test]
        public void Count_ShouldTakeAssignedValuesIntoAccount()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler).Invoke();
            Compiler.Compile();
            dict.Add("key", "value");
            Assert.That(dict.Count, Is.EqualTo(1));
        }

        [Test]
        public void Enumerator_ShouldReturnAssignedValues()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler).Invoke();
            Compiler.Compile();
            dict.Add("key", "value");
            Assert.That(dict.SequenceEqual(new[] { new KeyValuePair<string, object?>("key", "value") }));
        }
    }
}