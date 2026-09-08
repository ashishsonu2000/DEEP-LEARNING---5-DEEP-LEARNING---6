using System;
using System.Collections.Generic;

namespace EazyEdu.Web.Models
{
    /// <summary>Named values pulled out of the user's sentence.</summary>
    public class EntitySet
    {
        public EntitySet()
        {
            Values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public IDictionary<string, string> Values { get; private set; }

        public string Get(string key)
        {
            string value;
            return Values.TryGetValue(key, out value) ? value : null;
        }

        public bool Has(string key)
        {
            return !string.IsNullOrEmpty(Get(key));
        }

        public void Set(string key, string value)
        {
            if (string.IsNullOrEmpty(value)) return;
            Values[key] = value;
        }
    }

    public class IntentMatch
    {
        public string Intent { get; set; }
        public double Score { get; set; }
    }

    public class IntentResult
    {
        public IntentResult()
        {
            Entities = new EntitySet();
            Alternatives = new List<IntentMatch>();
        }

        public string Intent { get; set; }
        public double Confidence { get; set; }
        public EntitySet Entities { get; set; }
        public List<IntentMatch> Alternatives { get; set; }
        /// <summary>Text after normalisation and spell-correction — handy when debugging.</summary>
        public string NormalizedText { get; set; }
        public string CorrectedText { get; set; }
    }

    public class SentimentResult
    {
        /// <summary>positive, neutral or negative.</summary>
        public string Label { get; set; }
        public double Score { get; set; }
        /// <summary>True when the message reads like a complaint that a human should see.</summary>
        public bool NeedsHuman { get; set; }
    }
}
