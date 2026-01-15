using System;
using System.IO;
using Intersect.Server.Localization;
using NUnit.Framework;

namespace Intersect.Tests.Server.Database;

[TestFixture]
public class LocalizationRepositoryTests
{
    private string _tempDirectory = string.Empty;

    [SetUp]
    public void SetUp()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), $"intersect-localization-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDirectory);
    }

    [TearDown]
    public void TearDown()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Test]
    public void GetReturnsFallbackLanguageWhenMissing()
    {
        var databasePath = Path.Combine(_tempDirectory, "translations.db");
        var repository = new LocalizationRepository(databasePath);
        repository.EnsureSchema();

        repository.Upsert("Item", "item-1", "Name", "en", "Sword", "hash1");
        repository.Upsert("Item", "item-1", "Name", "es", "Espada", "hash1");

        Assert.That(repository.Get("Item", "item-1", "Name", "es"), Is.EqualTo("Espada"));
        Assert.That(repository.Get("Item", "item-1", "Name", "fr"), Is.EqualTo("Sword"));
    }
}
