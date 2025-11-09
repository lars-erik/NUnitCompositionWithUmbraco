using Microsoft.Extensions.Hosting;
using NUnitComposition.DependencyInjection;

namespace UmbracoTestsComposition.Common;

public class ServiceProviderAttribute() : InjectionProviderAttribute(nameof(IHost.Services)) { }
