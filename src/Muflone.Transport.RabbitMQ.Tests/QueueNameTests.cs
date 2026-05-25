using System.Security.Cryptography;
using System.Text;

namespace Muflone.Transport.RabbitMQ.Tests;

/// <summary>
/// Unit tests for queue name uniqueness logic.
/// These tests do NOT require a running RabbitMQ instance.
/// </summary>
public class QueueNameTests
{
    // -------------------------------------------------------------------------
    // Scenario: two handlers share the same simple class name but live in
    // different namespaces (e.g. Products.Facade vs Delivery.Facade).
    // The fix uses FullName instead of Name so the ConsumerTypeName is unique.
    // -------------------------------------------------------------------------

    [Fact]
    public void SameSimpleName_DifferentNamespaces_HaveDifferentFullName()
    {
        var type1 = typeof(FakeNamespace.Products.GlobalSettingsUpdatedIntegrationEventHandler);
        var type2 = typeof(FakeNamespace.Delivery.GlobalSettingsUpdatedIntegrationEventHandler);

        // Before fix: both yield the same Name → collision
        Assert.Equal(type1.Name, type2.Name);

        // After fix: FullName is unique
        Assert.NotEqual(type1.FullName, type2.FullName);
        Assert.NotEqual(type1.FullName ?? type1.Name, type2.FullName ?? type2.Name);
    }

    [Fact]
    public void QueueNameFormula_TwoHandlers_SameEvent_DifferentNamespaces_ProduceDifferentQueueNames()
    {
        const string clientId = "MyService";
        const string eventTypeName = "GlobalSettingsUpdated";
        var baseQueueName = $"{clientId}.{eventTypeName}";

        var type1 = typeof(FakeNamespace.Products.GlobalSettingsUpdatedIntegrationEventHandler);
        var type2 = typeof(FakeNamespace.Delivery.GlobalSettingsUpdatedIntegrationEventHandler);

        // Simulate the new queue name formula: base + FullName
        var queueName1 = $"{baseQueueName}.{type1.FullName}";
        var queueName2 = $"{baseQueueName}.{type2.FullName}";

        Assert.NotEqual(queueName1, queueName2);
    }

    // -------------------------------------------------------------------------
    // Scenario: fully-qualified name pushes the queue name past 255 chars.
    // The fix must produce a stable (deterministic) SHA256-based hash suffix.
    // -------------------------------------------------------------------------

    [Fact]
    public void QueueNameFormula_WhenExceeds255Chars_FallsBackToStableHash()
    {
        const string clientId = "MyService";
        const string eventTypeName = "GlobalSettingsUpdated";
        var baseQueueName = $"{clientId}.{eventTypeName}";

        // Build a consumer type name long enough to push the total over 255.
        // baseQueueName = "MyService.GlobalSettingsUpdated" (30 chars) + "." → need > 224 more chars.
        // 12 segments of "VeryLongNamespaceSegment" (24 chars each) + ".EventHandler" = ~312 chars.
        var longConsumerTypeName = string.Join(".", Enumerable.Repeat("VeryLongNamespaceSegment", 12))
                                   + ".GlobalSettingsUpdatedIntegrationEventHandler";

        var candidate = $"{baseQueueName}.{longConsumerTypeName}";
        Assert.True(candidate.Length > 255, "Precondition: candidate must exceed 255 chars for this test to be meaningful.");

        var result1 = ComputeQueueName(baseQueueName, longConsumerTypeName);
        var result2 = ComputeQueueName(baseQueueName, longConsumerTypeName);

        // Hash is deterministic across calls
        Assert.Equal(result1, result2);
        // Result respects the 255-char limit
        Assert.True(result1.Length <= 255);
        // Result is not the full candidate
        Assert.NotEqual(candidate, result1);
    }

    [Fact]
    public void QueueNameFormula_WhenExceeds255Chars_DifferentInputs_ProduceDifferentHashes()
    {
        const string clientId = "MyService";
        const string eventTypeName = "GlobalSettingsUpdated";
        var baseQueueName = $"{clientId}.{eventTypeName}";

        var suffix = string.Join(".", Enumerable.Repeat("VeryLongNamespaceSegment", 12));
        var longName1 = suffix + ".Products.GlobalSettingsUpdatedIntegrationEventHandler";
        var longName2 = suffix + ".Delivery.GlobalSettingsUpdatedIntegrationEventHandler";

        var candidate1 = $"{baseQueueName}.{longName1}";
        var candidate2 = $"{baseQueueName}.{longName2}";
        Assert.True(candidate1.Length > 255);
        Assert.True(candidate2.Length > 255);

        var result1 = ComputeQueueName(baseQueueName, longName1);
        var result2 = ComputeQueueName(baseQueueName, longName2);

        Assert.NotEqual(result1, result2);
    }

    // Mirrors the logic in RabbitMQSubscriber.GetQueueName()
    private static string ComputeQueueName(string baseQueueName, string consumerTypeName)
    {
        const int maxQueueNameLength = 255;
        var candidate = $"{baseQueueName}.{consumerTypeName}";
        if (candidate.Length <= maxQueueNameLength)
            return candidate;

        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(consumerTypeName));
        var hash = Convert.ToHexString(hashBytes)[..16];
        var hashCandidate = $"{baseQueueName}.{hash}";
        return hashCandidate[..Math.Min(hashCandidate.Length, maxQueueNameLength)];
    }
}

// Fake handler types are defined in QueueNameTestTypes.cs (separate file)
// to avoid mixing file-scoped and block-scoped namespace declarations.
