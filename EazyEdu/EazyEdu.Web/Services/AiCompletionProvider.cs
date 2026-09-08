using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace EazyEdu.Web.Services
{
    /// <summary>
    /// Optional generative fallback. When the rule engine cannot classify a message the
    /// assistant can ask a hosted model, giving it only the record summary it is allowed
    /// to talk about. Disabled unless an API key is configured.
    /// </summary>
    public interface IAiCompletionProvider
    {
        bool IsEnabled { get; }
        /// <summary>Returns the answer text, or null when the call fails or is disabled.</summary>
        string Complete(string systemPrompt, string userMessage);
    }

    /// <summary>Used when no key is configured: the assistant stays fully offline and rule-driven.</summary>
    public class NullAiCompletionProvider : IAiCompletionProvider
    {
        public bool IsEnabled { get { return false; } }

        public string Complete(string systemPrompt, string userMessage)
        {
            return null;
        }
    }

    /// <summary>Calls the Claude Messages API over plain HTTPS — no external packages needed.</summary>
    public class ClaudeCompletionProvider : IAiCompletionProvider
    {
        private readonly string _apiKey;
        private readonly string _endpoint;
        private readonly string _model;
        private readonly int _maxTokens;
        private readonly int _timeoutMs;

        public ClaudeCompletionProvider()
        {
            _apiKey = ConfigurationManager.AppSettings["EazyEdu:AiApiKey"];
            _endpoint = Or(ConfigurationManager.AppSettings["EazyEdu:AiEndpoint"], "https://api.anthropic.com/v1/messages");
            _model = Or(ConfigurationManager.AppSettings["EazyEdu:AiModel"], "claude-sonnet-5");
            _maxTokens = ParseInt(ConfigurationManager.AppSettings["EazyEdu:AiMaxTokens"], 400);
            _timeoutMs = ParseInt(ConfigurationManager.AppSettings["EazyEdu:AiTimeoutMs"], 8000);
        }

        public bool IsEnabled { get { return !string.IsNullOrWhiteSpace(_apiKey); } }

        public string Complete(string systemPrompt, string userMessage)
        {
            if (!IsEnabled || string.IsNullOrWhiteSpace(userMessage)) return null;

            try
            {
                var serializer = new JavaScriptSerializer();
                string body = serializer.Serialize(new Dictionary<string, object>
                {
                    { "model", _model },
                    { "max_tokens", _maxTokens },
                    { "system", systemPrompt ?? string.Empty },
                    { "messages", new object[]
                        {
                            new Dictionary<string, object>
                            {
                                { "role", "user" },
                                { "content", userMessage }
                            }
                        }
                    }
                });

                ServicePointManager.SecurityProtocol |= SecurityProtocolType.Tls12;

                var request = (HttpWebRequest)WebRequest.Create(_endpoint);
                request.Method = "POST";
                request.ContentType = "application/json";
                request.Timeout = _timeoutMs;
                request.ReadWriteTimeout = _timeoutMs;
                request.Headers.Add("x-api-key", _apiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");

                byte[] payload = Encoding.UTF8.GetBytes(body);
                request.ContentLength = payload.Length;
                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(payload, 0, payload.Length);
                }

                using (var response = (HttpWebResponse)request.GetResponse())
                using (Stream stream = response.GetResponseStream())
                {
                    if (stream == null) return null;
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        return ExtractText(reader.ReadToEnd(), serializer);
                    }
                }
            }
            catch (WebException)
            {
                return null;      // network trouble or a non-2xx status: fall back to the rule answer
            }
            catch (InvalidOperationException)
            {
                return null;
            }
        }

        private static string ExtractText(string json, JavaScriptSerializer serializer)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;

            var root = serializer.DeserializeObject(json) as Dictionary<string, object>;
            if (root == null) return null;

            object contentObject;
            if (!root.TryGetValue("content", out contentObject)) return null;

            var blocks = contentObject as object[];
            if (blocks == null) return null;

            var text = new StringBuilder();
            foreach (object block in blocks)
            {
                var map = block as Dictionary<string, object>;
                if (map == null) continue;

                object type;
                if (map.TryGetValue("type", out type) && !string.Equals(Convert.ToString(type), "text", StringComparison.OrdinalIgnoreCase))
                    continue;

                object value;
                if (map.TryGetValue("text", out value)) text.Append(Convert.ToString(value));
            }

            string result = text.ToString().Trim();
            return result.Length == 0 ? null : result;
        }

        private static string Or(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value.Trim();
        }

        private static int ParseInt(string value, int fallback)
        {
            int parsed;
            return int.TryParse(value, out parsed) && parsed > 0 ? parsed : fallback;
        }
    }
}
