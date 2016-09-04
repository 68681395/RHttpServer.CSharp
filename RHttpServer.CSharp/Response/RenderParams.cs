﻿using System.Collections;
using System.Collections.Generic;
using RHttpServer.Plugins;
using RPlugin.RHttpServer;
using IPageRenderer = RHttpServer.Plugins.IPageRenderer;

namespace RHttpServer.Response
{
    /// <summary>
    ///     Parameters used when rendering a page
    /// </summary>
    public sealed class RenderParams : IEnumerable<KeyValuePair<string, string>>
    {
        internal RenderParams()
        {
        }

        private readonly IDictionary<string, string> _dict = new Dictionary<string, string>();

        private IPageRenderer _renderer;

        /// <summary>
        ///     Attempts to retrieve the replacement data associated with the tag
        ///     Returns empty string if tag not found in parameters
        /// </summary>
        /// <param name="parName">The name of the tag to find replacement data for</param>
        /// <returns>Replacement data for the given tag</returns>
        public string this[string parName]
        {
            get
            {
                var res = "";
                _dict.TryGetValue(parName, out res);
                return res;
            }
        }

        /// <summary>
        ///     Adds tag and replacement-data pair to parameters
        /// </summary>
        /// <param name="parTag">The tag id</param>
        /// <param name="parData">The replacement-data for the tag</param>
        public void Add(string parTag, string parData)
        {
            _dict.Add(_renderer.Parametrize(parTag, parData));
        }

        /// <summary>
        ///     Adds tag and json-serialized replacement-data pair to parameters
        /// </summary>
        /// <param name="parTag">The tag id</param>
        /// <param name="parData">The replacement-data object for the tag</param>
        public void Add(string parTag, object parData)
        {
            _dict.Add(_renderer.ParametrizeObject(parTag, parData));
        }

        internal void SetRenderer(IPageRenderer renderer)
        {
            _renderer = renderer;
        }

        /// <summary>
        ///     Returns the enumeration of tag and replacement-data pairs
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