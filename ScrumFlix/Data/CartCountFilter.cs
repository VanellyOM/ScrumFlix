/*
 * File: /ScrumFlix/Data/CartCountFilter.cs
 * Description: Global action filter that injects the current cart item count into ViewBag
 *              for every controller action, enabling the navbar cart badge to update automatically.
 */


namespace ScrumFlix.Data;

/// <summary>
/// A global result filter that sets ViewBag.CartCount before every view renders,
/// ensuring the navbar cart badge always reflects the current session cart state.
/// </summary>
public class CartCountFilter : IResultFilter
{
    private readonly CartService _cart;

    /// <summary>
    /// Initializes CartCountFilter with the scoped cart service.
    /// </summary>
    /// <param name="cart">The session-based cart service.</param>
    public CartCountFilter(CartService cart)
    {
        _cart = cart;
    }

    /// <summary>
    /// Executes before the action result runs. Sets ViewBag.CartCount for use in the layout.
    /// </summary>
    /// <param name="context">The result executing context.</param>
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Controller is Controller controller)
            controller.ViewBag.CartCount = _cart.GetItemCount();
    }

    /// <summary>
    /// Executes after the action result runs. No operation required here.
    /// </summary>
    /// <param name="context">The result executed context.</param>
    public void OnResultExecuted(ResultExecutedContext context) { }
}
