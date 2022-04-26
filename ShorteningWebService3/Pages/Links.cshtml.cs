using LiteDB;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace ShorteningWebService3.Pages
{
    public class LinksModel : PageModel
    {
        private readonly ILiteDatabase _context;

        public LinksModel(ILiteDatabase context)
        {
            _context = context;
        }

        public ILiteCollection<ShortLink> Links { get; set; }

        public async Task OnGetAsync()
        {
            Links = _context.GetCollection<ShortLink>();
        }
        private readonly ILogger<LinksModel> _logger;

    }
}