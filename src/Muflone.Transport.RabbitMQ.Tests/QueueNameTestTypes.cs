// Fake handler types with the same simple class name but different namespaces,
// mimicking the real-world collision scenario (e.g. Products.Facade vs Delivery.Facade).

namespace FakeNamespace.Products
{
    public class GlobalSettingsUpdatedIntegrationEventHandler;
}

namespace FakeNamespace.Delivery
{
    public class GlobalSettingsUpdatedIntegrationEventHandler;
}
