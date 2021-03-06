using System;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace BeatSaverSharp
{
    /// <summary>
    /// Page of Results
    /// </summary>
    public sealed class Page
    {
        #region JSON Properties
        /// <summary>
        /// Documents on this page
        /// </summary>
        [JsonProperty("docs")]
        public ReadOnlyCollection<Beatmap> Docs { get; private set; }

        /// <summary>
        /// Total number of documents for the specified endpoint
        /// </summary>
        [JsonProperty("totalDocs")]
        public int TotalDocs { get; private set; }

        /// <summary>
        /// Index of the Last Page
        /// </summary>
        [JsonProperty("lastPage")]
        public int LastPage { get; private set; }

        /// <summary>
        /// Index of the Previous Page
        /// </summary>
        [JsonProperty("prevPage")]
        public int? PreviousPage { get; private set; }

        /// <summary>
        /// Index of the Next Page
        /// </summary>
        [JsonProperty("nextPage")]
        public int? NextPage { get; private set; }
        #endregion

        #region Properties
        [JsonIgnore]
        internal BeatSaver Client { get; set; }

        [JsonIgnore]
        internal string PageURI { get; set; }
        [JsonIgnore]
        internal string Query { get; set; }
        #endregion

        #region Methods
        /// <summary>
        /// Fetch the previous page in this sequence
        /// </summary>
        /// <param name="progress">Optional progress reporter</param>
        /// <returns></returns>
        public async Task<Page> FetchPreviousPage(IProgress<double> progress = null) => await FetchPreviousPage(CancellationToken.None, progress).ConfigureAwait(false);
        /// <summary>
        /// Fetch the previous page in this sequence
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <param name="progress">Optional progress reporter</param>
        /// <returns></returns>
        public async Task<Page> FetchPreviousPage(CancellationToken token, IProgress<double> progress = null)
        {
            if (PreviousPage == null) return null;

            string url = $"{PageURI}/{PreviousPage}";
            if (Query != null) url += $"?{Query}";
            Page p = await Client.FetchPaged(url, token, progress).ConfigureAwait(false);

            p.PageURI = PageURI;
            p.Query = Query;

            return p;
        }

        /// <summary>
        /// Fetch the next page in this sequence
        /// </summary>
        /// <param name="progress">Optional progress reporter</param>
        /// <returns></returns>
        public async Task<Page> FetchNextPage(IProgress<double> progress = null) => await FetchNextPage(CancellationToken.None, progress).ConfigureAwait(false);
        /// <summary>
        /// Fetch the next page in this sequence
        /// </summary>
        /// <param name="token">Cancellation token</param>
        /// <param name="progress">Optional progress reporter</param>
        /// <returns></returns>
        public async Task<Page> FetchNextPage(CancellationToken token, IProgress<double> progress = null)
        {
            if (NextPage == null) return null;

            string url = $"{PageURI}/{NextPage}";
            if (Query != null) url += $"?{Query}";
            Page p = await Client.FetchPaged(url, token, progress).ConfigureAwait(false);

            p.PageURI = PageURI;

            p.Query = Query;
            return p;
        }
        #endregion
    }
}
