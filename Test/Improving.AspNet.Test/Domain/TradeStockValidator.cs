namespace Improving.AspNet.Tests.Domain
{
    using FluentValidation;

    public class TradeStockValidator : AbstractValidator<TradeStock>
    {
        public TradeStockValidator()
        {
            RuleFor(us => us.Stock).NotEmpty();
            When(us => us.Stock != null, () =>
                                             {
                                                 RuleFor(ts => ts.Stock.Symbol).NotEmpty();
                                                 RuleFor(ts => ts.Stock.Quote).GreaterThan(0M);
                                             });
        }
    }
}