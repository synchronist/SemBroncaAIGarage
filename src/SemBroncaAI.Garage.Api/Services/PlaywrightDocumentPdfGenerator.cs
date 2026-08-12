using Microsoft.Playwright;

namespace SemBroncaAI.Garage.Api.Services;

public sealed class PlaywrightDocumentPdfGenerator(IConfiguration configuration) : IDocumentPdfGenerator
{
    public async Task<byte[]> GenerateAsync(string relativeDocumentUrl, string readySelector, CancellationToken cancellationToken = default)
    {
        var webBaseUrl = configuration["Web:BaseUrl"] ?? throw new InvalidOperationException("A URL do Web não foi configurada para geração de PDF.");
        using var playwright = await Playwright.CreateAsync();
        await using var browser = await playwright.Chromium.LaunchAsync(new() { Headless = true });
        var page = await browser.NewPageAsync(new() { ViewportSize = new() { Width = 1280, Height = 900 } });
        await page.GotoAsync(new Uri(new Uri(webBaseUrl), relativeDocumentUrl).ToString(), new() { WaitUntil = WaitUntilState.NetworkIdle });
        await page.Locator(readySelector).WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 30000 });
        return await page.PdfAsync(new()
        {
            Format = "A4", PrintBackground = true, PreferCSSPageSize = true,
            Margin = new() { Top = "0", Right = "0", Bottom = "0", Left = "0" }
        });
    }
}
