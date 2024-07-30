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
    using Primitives;

    using StaticDictionary = Internals.StaticDictionary<object?>;

    [TestFixture]
    public class StaticDictionaryTests
    {
        DelegateCompiler Compiler { get; set; } = null!;

        [SetUp]
        public void SetupTest() => Compiler = new DelegateCompiler();

        [Test]
        public void Get_ShouldThrowOnMissingKey()
        {
            StaticDictionary.Builder builder = new();
            StaticDictionary dict = builder.CreateFactory(Compiler, out _).Invoke();
            Compiler.Compile();
            Assert.Throws<KeyNotFoundException>(() => _ = dict["key"]);
        }

        [Test]
        public void TryGet_ShouldFailOnMissingKey()
        {
            StaticDictionary.Builder builder = new();
            StaticDictionary dict = builder.CreateFactory(Compiler, out _).Invoke();
            Compiler.Compile();
            Assert.False(dict.TryGetValue("key", out _));
        }

        [Test]
        public void Get_ShouldThrowOnUnassignedKey()
        {
            StaticDictionary.Builder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler, out _).Invoke();
            Compiler.Compile();
            Assert.Throws<KeyNotFoundException>(() => _ = dict["key"]);
        }

        [Test]
        public void TryGet_ShouldFailOnUnassignedKey()
        {
            StaticDictionary.Builder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler, out _).Invoke();
            Compiler.Compile();
            Assert.False(dict.TryGetValue("key", out _));
        }

        [Test]
        public void Keys_ShouldNotReturnUnassignedKeys()
        {
            StaticDictionary.Builder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler, out _).Invoke();
            Compiler.Compile();
            Assert.That(dict.Keys, Is.Empty);
        }

        [Test]
        public void Values_ShouldNotReturnUnassignedValues()
        {
            StaticDictionary.Builder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler, out _).Invoke();
            Compiler.Compile();
            Assert.That(dict.Values, Is.Empty);
        }

        [Test]
        public void Count_ShouldNotTakeUnassignedValuesIntoAccount()
        {
            StaticDictionary.Builder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler, out _).Invoke();
            Compiler.Compile();
            Assert.That(dict.Count, Is.Zero);
        }

        [Test]
        public void Enumerator_ShouldNotReturnUnassignedValues()
        {
            StaticDictionary.Builder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler, out _).Invoke();
            Compiler.Compile();
            Assert.That(dict.ToList(), Is.Empty);
        }

        [Test]
        public void Get_ShouldReturnTheCorrectValue([Values(0, 1, 2, 5, 10, 20, 100)]int keyCount)
        {
            StaticDictionary.Builder builder = new();
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
            StaticDictionary.Builder builder = new();
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
            StaticDictionary.Builder builder = new();
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
            StaticDictionary.Builder builder = new();
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
            StaticDictionary.Builder builder = new();
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
            StaticDictionary.Builder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler, out IReadOnlyDictionary<string, int> shortcuts).Invoke();
            Compiler.Compile();
            dict[shortcuts["key"]] = "value";
            Assert.That(dict.Values.Single(), Is.EqualTo("value"));
        }

        [Test]
        public void Count_ShouldTakeAssignedValuesIntoAccount()
        {
            StaticDictionary.Builder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler, out IReadOnlyDictionary<string, int> shortcuts).Invoke();
            Compiler.Compile();
            dict[shortcuts["key"]] = "value";
            Assert.That(dict.Count, Is.EqualTo(1));
        }

        [Test]
        public void Enumerator_ShouldReturnAssignedValues()
        {
            StaticDictionary.Builder builder = new();
            builder.RegisterKey("key");
            StaticDictionary dict = builder.CreateFactory(Compiler, out IReadOnlyDictionary<string, int> shortcuts).Invoke();
            Compiler.Compile();
            dict[shortcuts["key"]] = "value";
            Assert.That(dict.SequenceEqual(new[] { new KeyValuePair<string, object?>("key", "value") }));
        }
    }
}