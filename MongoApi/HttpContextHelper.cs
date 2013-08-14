using System.IO;
using System.Web;

namespace MongoApi
{
    public static class HttpContextHelper
    {
        public static string GetAllRequestContent(this HttpRequestBase request)
        {
            string document;

            request.InputStream.Seek(0, SeekOrigin.Begin);
            using (var reader = new StreamReader(request.InputStream))
            {
                document = reader.ReadToEnd();
            }
            return document;
        }
    }
}