using Microsoft.Extensions.Hosting;
using NUnitComposition.DependencyInjection;

namespace Umbraco.Community.Integration.Tests.Extensions;

public class ServiceProviderAttribute() : InjectionProviderAttribute(nameof(IHost.Services)) { }
