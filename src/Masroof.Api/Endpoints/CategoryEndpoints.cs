using Masroof.Application.Categories;

namespace Masroof.Api.Endpoints;

public static class CategoryEndpoints
{
    public static void MapCategoryEndpoints(this RouteGroupBuilder group)
    {
        group.MapGet("/categories", async (GetCategoriesHandler handler, CancellationToken ct) =>
                Results.Ok(await handler.HandleAsync(ct)))
            .WithSummary("The taxonomy, localized to the user.");
    }
}
