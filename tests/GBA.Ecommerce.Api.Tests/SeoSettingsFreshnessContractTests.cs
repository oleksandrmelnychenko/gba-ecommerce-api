using System;
using System.Reflection;
using GBA.Ecommerce.Controllers;
using Microsoft.AspNetCore.OutputCaching;

namespace GBA.Ecommerce.Api.Tests;

public sealed class SeoSettingsFreshnessContractTests {
    [Fact]
    public void Shop_settings_with_the_selected_payment_card_are_not_output_cached() {
        MethodInfo allSettings = typeof(SeoPageController).GetMethod(
            nameof(SeoPageController.GetAll),
            Type.EmptyTypes)!;
        MethodInfo localizedSettings = typeof(SeoPageController).GetMethod(
            nameof(SeoPageController.GetAll),
            new[] { typeof(string) })!;

        Assert.Empty(allSettings.GetCustomAttributes<OutputCacheAttribute>());
        Assert.Empty(localizedSettings.GetCustomAttributes<OutputCacheAttribute>());
    }
}
