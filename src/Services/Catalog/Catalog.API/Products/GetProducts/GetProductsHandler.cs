namespace Catalog.API.Products.GetProducts;

public record GetProductsQuery(int? PageNumber = 1, int? PageSize = 10) : IQuery<GetProductsResult>;
public record GetProductsResult(IEnumerable<Product> Products, int TotalRecords, int TotalPage);
internal class GetProductsHandler
    (IDocumentSession session)
    : IQueryHandler<GetProductsQuery, GetProductsResult>
{
    public async Task<GetProductsResult> Handle(GetProductsQuery query, CancellationToken cancellationToken)
    {
        var totalCount = await session.Query<Product>().CountAsync();
        var totalPage = (int)Math.Ceiling((double)totalCount / (query.PageSize ?? 10));
        var products = await session.Query<Product>()
            .ToPagedListAsync(query.PageNumber ?? 1, query.PageSize ?? 10, cancellationToken);
        return new GetProductsResult(products, totalCount, totalPage);
    }
}
