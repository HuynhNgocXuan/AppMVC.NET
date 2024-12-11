using System.Net;

namespace webMVC.ExtendMethods
{
    public static class AppExtends
    {
        public static void AddStatusCodePage(this IApplicationBuilder app)
        {
            app.UseStatusCodePages(async context =>
            {
                var response = context.HttpContext.Response;
                var statusCode = response.StatusCode;

                // Create content
                var content = @$"
                    <html lang='en'>
                    <head>
                        <meta charset='UTF-8'>
                        <title>{statusCode}</title>
                        <style>
                            body {{ text-align: center; font-family: Arial, sans-serif; }}
                            h1 {{ color: #ff0000; }}
                        </style>
                    </head>
                    <body>
                        <h2>Có lỗi {statusCode} - {(HttpStatusCode)statusCode}</h2>
                    </body>
                    </html>";

                // Write the content to the response
                await response.WriteAsync(content);
            });
        }
    }

}
