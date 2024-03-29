﻿/********************************************************************************
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
            StaticDictionary dict = builder.CreateFactory(Compiler, out _).Invoke();
            Compiler.Compile();
            Assert.Throws<KeyNotFoundException>(() => _ = dict["key"]);
        }

        [Test]
        public void TryGet_ShouldFailOnMissingKey()
        {
            StaticDictionaryBuilder builder = new();
            StaticDictionary dict = builder.CreateFactory(Compiler, out _).Invoke();
            Compiler.Compile();
            Assert.False(dict.TryGetValue("key", out _));
        }

        [Test]
        public void Get_ShouldThrowOnUnassignedKey()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler, out _).Invoke();
            Compiler.Compile();
            Assert.Throws<KeyNotFoundException>(() => _ = dict["key"]);
        }

        [Test]
        public void TryGet_ShouldFailOnUnassignedKey()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler, out _).Invoke();
            Compiler.Compile();
            Assert.False(dict.TryGetValue("key", out _));
        }

        [Test]
        public void Keys_ShouldNotReturnUnassignedKeys()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler, out _).Invoke();
            Compiler.Compile();
            Assert.That(dict.Keys, Is.Empty);
        }

        [Test]
        public void Values_ShouldNotReturnUnassignedValues()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler, out _).Invoke();
            Compiler.Compile();
            Assert.That(dict.Values, Is.Empty);
        }

        [Test]
        public void Count_ShouldNotTakeUnassignedValuesIntoAccount()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler, out _).Invoke();
            Compiler.Compile();
            Assert.That(dict.Count, Is.Zero);
        }

        [Test]
        public void Enumerator_ShouldNotReturnUnassignedValues()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler, out _).Invoke();
            Compiler.Compile();
            Assert.That(dict.ToList(), Is.Empty);
        }

        [Test]
        public void Get_ShouldReturnTheCorrectValue([Values(0, 1, 2, 5, 10, 20, 100)]int keyCount)
        {
            StaticDictionaryBuilder builder = new();
            for (int i = 0; i < keyCount; i++)
            {
                builder.RegisterKey($"key{i}");
            }

            StaticDictionary dict = builder.CreateFactory(Compiler, out IReadOnlyDictionary<string, int> shortcuts).Invoke();
            Assert.That(shortcuts.Count, Is.EqualTo(keyCount));

            Compiler.Compile();

            for (int i = 0; i < keyCount; i++)
            {
                Assert.That(shortcuts.TryGetValue($"key{i}", out int id));
                dict[id] = $"value{i}";
            }

            for (int i = 0; i < keyCount; i++)
            {
                Assert.That(dict[$"key{i}"], Is.EqualTo($"value{i}"));
            }
        }

        [Test]
        public void GetById_ShouldReturnTheCorrectValue([Values(0, 1, 2, 5, 10, 20, 100)] int keyCount)
        {
            StaticDictionaryBuilder builder = new();
            for (int i = 0; i < keyCount; i++)
            {
                builder.RegisterKey($"key{i}");
            }

            StaticDictionary dict = builder.CreateFactory(Compiler, out IReadOnlyDictionary<string, int> shortcuts).Invoke();
            Assert.That(shortcuts.Count, Is.EqualTo(keyCount));

            Compiler.Compile();

            for (int i = 0; i < keyCount; i++)
            {
                Assert.That(shortcuts.TryGetValue($"key{i}", out int id));
                dict[id] = $"value{i}";
            }

            for (int i = 0; i < keyCount; i++)
            {
                Assert.That(dict[shortcuts[$"key{i}"]], Is.EqualTo($"value{i}"));
            }
        }

        [Test]
        public void TryGet_ShouldReturnTheCorrectValue()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key1");
            builder.RegisterKey("key2");
            StaticDictionary dict = builder.CreateFactory(Compiler, out IReadOnlyDictionary<string, int> shortcuts).Invoke();
            Compiler.Compile();
            dict[shortcuts["key1"]] = "value";
            Assert.That(dict.TryGetValue("key1", out object? val));
            Assert.That(val, Is.EqualTo("value"));
        }

        [Test]
        public void Keys_ShouldReturnAssignedKeys()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key1");
            builder.RegisterKey("key2");
            StaticDictionary dict = builder.CreateFactory(Compiler, out IReadOnlyDictionary<string, int> shortcuts).Invoke();
            Compiler.Compile();
            dict[shortcuts["key1"]] = "value";
            Assert.That(dict.Keys.Single(), Is.EqualTo("key1"));
        }

        [Test]
        public void ContainsKey_ShouldReturnTrueIfTheKeyExists()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key1");
            builder.RegisterKey("key2");
            StaticDictionary dict = builder.CreateFactory(Compiler, out IReadOnlyDictionary<string, int> shortcuts).Invoke();
            Compiler.Compile();
            dict[shortcuts["key1"]] = "value";
            Assert.False(dict.ContainsKey("invalid"));
            Assert.False(dict.ContainsKey("key2"));
            Assert.True(dict.ContainsKey("key1"));
        }

        [Test]
        public void Values_ShouldReturnAssignedValues()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler, out IReadOnlyDictionary<string, int> shortcuts).Invoke();
            Compiler.Compile();
            dict[shortcuts["key"]] = "value";
            Assert.That(dict.Values.Single(), Is.EqualTo("value"));
        }

        [Test]
        public void Count_ShouldTakeAssignedValuesIntoAccount()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler, out IReadOnlyDictionary<string, int> shortcuts).Invoke();
            Compiler.Compile();
            dict[shortcuts["key"]] = "value";
            Assert.That(dict.Count, Is.EqualTo(1));
        }

        [Test]
        public void Enumerator_ShouldReturnAssignedValues()
        {
            StaticDictionaryBuilder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler, out IReadOnlyDictionary<string, int> shortcuts).Invoke();
            Compiler.Compile();
            dict[shortcuts["key"]] = "value";
            Assert.That(dict.SequenceEqual(new[] { new KeyValuePair<string, object?>("key", "value") }));
        }
    }
}