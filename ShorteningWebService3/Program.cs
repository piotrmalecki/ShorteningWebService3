using ShorteningWebService3;
using System.Threading.Tasks;
using LiteDB;
using System.Linq;
using System.Configuration;
using System.Diagnostics;

var builder = WebApplication.CreateBuilder(args);
{
    var services = builder.Services;
    var env = builder.Environment;

    // Add the necessary components for HTTP routing.
    services.AddRouting();

    // Add LiteDB
    services.AddSingleton<ILiteDatabase, LiteDatabase>(_ => new LiteDatabase("short-links.db"));
}
// Add services to the container.
builder.Services.AddRazorPages();


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}



app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapRazorPages();

app.UseEndpoints((endpoints) =>
{
    endpoints.MapFallback(HandleRedirect);
    endpoints.MapPost("/shorten", HandleShortenUrl);
});

app.Run();

static Task HandleShortenUrl(HttpContext context)
{
    if (!context.Request.HasFormContentType || !context.Request.Form.ContainsKey("url"))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return context.Response.WriteAsync("Cannot process request");
    }

    context.Request.Form.TryGetValue("url", out var formData);
    var requestedUrl = formData.ToString();

    if (!Uri.TryCreate(requestedUrl, UriKind.Absolute, out Uri result))
    {
        context.Response.StatusCode = StatusCodes.Status400BadRequest;
        return context.Response.WriteAsync("Could not understand URL.");
    }

    var url = result.ToString();
    // Ask for LiteDB and persist a short link
    var liteDB = context.RequestServices.GetService<ILiteDatabase>();
    var links = liteDB.GetCollection<ShortLink>(BsonAutoId.Int32);

    var entry = new ShortLink
    {
        Url = url
    };

    links.Insert(entry);

    var urlChunk = entry.GetUrlChunk();
    var responseUri = $"{context.Request.Scheme}://{context.Request.Host}/{urlChunk}";
    context.Response.Redirect($"/#{responseUri}");
    return Task.CompletedTask;
}

static Task HandleRedirect(HttpContext context)
{
    var db = context.RequestServices.GetService<ILiteDatabase>();
    var collection = db.GetCollection<ShortLink>();

    var path = context.Request.Path.ToUriComponent().Trim('/');
    var id = ShortLink.GetId(path);
    var entry = collection.Find(p => p.Id == id).FirstOrDefault();

    if (entry != null)
    {
        Trace.WriteLine("Redirect from " + path + "to: " + entry.Url);
        context.Response.Redirect(entry.Url);
    }
    else
    {
        context.Response.Redirect("/");
    }

    return Task.CompletedTask;
}
