﻿using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace WebServerHoster
{
    /// <summary>
    /// Parameters used when rendering an .ecs page
    /// </summary>
    public class RenderParams : IEnumerable<KeyValuePair<string, string>>
    {
        private readonly IDictionary<string, string> _dict = new Dictionary<string, string>();

        /// <summary>
        /// Adds tag and replacement-data pair to parameters
        /// </summary>
        /// <param name="parName">The tag id</param>
        /// <param name="parData">The replacement-data for the tag</param>
        public void Add(string parName, string parData)
        {
            _dict.Add($"<%{parName.Trim(' ')}%>", parData);
        }

        /// <summary>
        /// Adds tag and json-serialized replacement-data pair to parameters
        /// </summary>
        /// <param name="parName">The tag id</param>
        /// <param name="parData">The replacement-data object for the tag</param>
        public void Add(string parName, object parData)
        {
            _dict.Add($"<%{parName.Trim(' ')}%>", JsonConvert.SerializeObject(parData));
        }

        /// <summary>
        /// Attempts to retrieve the replacement data associated with the tag
        /// Returns empty string if tag not found in parameters
        /// </summary>
        /// <param name="parName">The name of the tag to find replacement data for</param>
        /// <returns>Replacement data for the given tag</returns>
        public string this[string parName]
        {
            get
            {
                string res = "";
                _dict.TryGetValue(parName, out res);
                return res;
            }
        }

        /// <summary>
        /// Returns the enumeration of tag and replacement-data pairs
        /// </summary>
        /// <returns>Pairs of tag and replacement-data</returns>
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return _dict.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}