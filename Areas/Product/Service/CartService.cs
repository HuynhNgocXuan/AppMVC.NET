
using Newtonsoft.Json;
using webMVC.Areas.Product.Models;

public class CartService
{
        // Key lưu chuỗi json của Cart
    public const string CARTKEY = "cart";

    private readonly IHttpContextAccessor _context;

    private readonly HttpContext? HttpContext;

    public CartService(IHttpContextAccessor context)
    {
        _context = context;
        HttpContext = context.HttpContext;
    }


    // Lấy cart từ Session (danh sách CartItem)
    public List<CartItem> GetCartItems () {

        var session = HttpContext!.Session;
        string? jsonCart = session.GetString (CARTKEY);
        if (jsonCart != null) {
            return JsonConvert.DeserializeObject<List<CartItem>> (jsonCart)!;
        }
        return new List<CartItem> ();
    }

    // Xóa cart khỏi session
    public  void ClearCart () {
        var session = HttpContext!.Session;
        session.Remove (CARTKEY);
    }

    // Lưu Cart (Danh sách CartItem) vào session
    public  void SaveCartSession (List<CartItem> ls) {
        var session = HttpContext!.Session;
        string jsonCart = JsonConvert.SerializeObject (ls);
        session.SetString (CARTKEY, jsonCart);
    }
}