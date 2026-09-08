using System;
using System.IO;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;
using System.Web.SessionState;
using EazyEdu.Web.Models;
using EazyEdu.Web.Security;
using EazyEdu.Web.Services;

namespace EazyEdu.Web.Api
{
    /// <summary>
    /// Shared plumbing for the JSON endpoints: authentication, CSRF, rate limiting,
    /// request/response serialisation and a single place where errors turn into JSON.
    /// </summary>
    public abstract class ApiHandlerBase : IHttpHandler, IRequiresSessionState
    {
        public bool IsReusable { get { return false; } }

        /// <summary>Endpoints that change state require the CSRF header; read-only ones do not.</summary>
        protected virtual bool RequiresCsrf { get { return true; } }
        protected virtual bool RequiresAuthentication { get { return true; } }
        protected virtual bool RateLimited { get { return true; } }

        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "application/json";
            context.Response.Cache.SetCacheability(HttpCacheability.NoCache);
            context.Response.AddHeader("X-Content-Type-Options", "nosniff");

            try
            {
                UserAccount user = AppServices.Auth.GetCurrentUser(context);

                if (RequiresAuthentication && user == null)
                {
                    Fail(context, 401, "Your session has expired. Please sign in again.");
                    return;
                }

                if (RequiresCsrf && !AntiForgery.Validate(context))
                {
                    Fail(context, 403, "Security token missing or stale. Refresh the page and try again.");
                    return;
                }

                if (RateLimited && !AppServices.Limiter.TryAcquire(RateKey(context, user)))
                {
                    Fail(context, 429, "That's a lot of questions at once — give me a few seconds.");
                    return;
                }

                Handle(context, user);
            }
            catch (Exception ex)
            {
                context.Response.Clear();
                context.Response.ContentType = "application/json";
                Fail(context, 500, "Something went wrong while answering. Please try again.");
                System.Diagnostics.Trace.TraceError("EazyEdu API error: " + ex);
            }
        }

        protected abstract void Handle(HttpContext context, UserAccount user);

        private static string RateKey(HttpContext context, UserAccount user)
        {
            if (user != null) return "u:" + user.Id;
            if (context.Session != null) return "s:" + context.Session.SessionID;
            return "ip:" + context.Request.UserHostAddress;
        }

        protected static T ReadJson<T>(HttpContext context) where T : class, new()
        {
            // Validating the CSRF token may have touched Request.Form, which drains the
            // input stream, so always rewind before reading the JSON body.
            if (context.Request.InputStream.CanSeek) context.Request.InputStream.Position = 0;

            using (var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8))
            {
                string body = reader.ReadToEnd();
                if (string.IsNullOrWhiteSpace(body)) return new T();

                var serializer = new JavaScriptSerializer { MaxJsonLength = 1024 * 1024 };
                try
                {
                    return serializer.Deserialize<T>(body) ?? new T();
                }
                catch (ArgumentException)
                {
                    return new T();
                }
            }
        }

        protected static void WriteJson(HttpContext context, object payload)
        {
            var serializer = new JavaScriptSerializer { MaxJsonLength = 8 * 1024 * 1024 };
            context.Response.Write(serializer.Serialize(payload));
        }

        protected static void Fail(HttpContext context, int status, string message)
        {
            context.Response.StatusCode = status;
            context.Response.TrySkipIisCustomErrors = true;
            WriteJson(context, new { ok = false, error = message });
        }
    }
}
