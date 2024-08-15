namespace Basket.API.Basket.GetBasket;

public record GetBasketQuery(string UserName) : IQuery<GetBasketResult>;
public record GetBasketResult(ShoppingCart Cart);

public class GetBasketHandler
    (IBasketRepository repository)
    : IQueryHandler<GetBasketQuery, GetBasketResult>
{
    public async Task<GetBasketResult> Handle(GetBasketQuery request, CancellationToken cancellationToken)
    {
        var basket = await repository.GetBasket(request.UserName, cancellationToken);

        if (basket is null)
            throw new BasketNotFoundException(request.UserName);

        return new GetBasketResult(basket);
    }
}
