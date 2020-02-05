using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using static BeatSaverSharp.Tests.Utils;

[assembly: Parallelize(Workers = 4, Scope = ExecutionScope.MethodLevel)]

namespace BeatSaverSharp.Tests
{
    [TestClass]
    public class BeatSaverTests
    {
        #region Constructor Tests
        [TestMethod]
        public void DefaultOptions()
        {
            _ = new BeatSaver();

            var options = new HttpOptions();
            _ = new BeatSaver(options);
        }

        [TestMethod]
        public void ApplicationNameAndVersion()
        {
            var options = new HttpOptions()
            {
                ApplicationName = "TestApp",
                Version = new Version(1, 0),
            };

            _ = new BeatSaver(options);
        }

        [TestMethod]
        public void ApplicationNameWithoutVersion()
        {
            var options = new HttpOptions()
            {
                ApplicationName = "TestApp",
            };

            Assert.ThrowsException<ArgumentException>(() =>
            {
                _ = new BeatSaver(options);
            });
        }
        #endregion

        #region Internal Method Tests
        [TestMethod]
        public async Task WithCancellationToken()
        {
            using CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;

            var map = await Client.Key("4c19", token);
            CheckOvercooked(map);
        }

        [TestMethod]
        public async Task WithCancellationTokenCancelled()
        {
            using CancellationTokenSource cts = new CancellationTokenSource();
            CancellationToken token = cts.Token;
            cts.Cancel();

            Beatmap map = null;
            try
            {
                map = await Client.Key("4c19", token: token);
            }
            catch (TaskCanceledException)
            {
                // Pass
            }
            catch (Exception ex)
            {
                throw ex;
            }

            Assert.IsNull(map);
        }

        [TestMethod]
        public async Task WithProgress()
        {
            var map = await Client.Key("4c19");
            Assert.IsNotNull(map);

            int updates = 0;
            Progress<double> progress = new Progress<double>();
            progress.ProgressChanged += (_, p) =>
            {
                updates += 1;

                Assert.IsTrue(p >= 0);
                Assert.IsTrue(p <= 1);
            };

            await Task.Run(() => map.FetchCoverImage(progress: progress));
            Assert.IsTrue(updates > 0, $"Updates: {updates}");
        }
        #endregion

        #region Method Tests
        #region Latest
        [TestMethod]
        public async Task ValidLatestOrder()
        {
            var maps = await Client.Latest();
            Assert.IsNotNull(maps);

            DateTime? previous = null;
            foreach (var map in maps.Docs)
            {
                if (previous == null)
                {
                    previous = map.Uploaded;
                    continue;
                }

                Assert.IsTrue(map.Uploaded < previous);
                previous = map.Uploaded;
            }
        }
        #endregion

        #region Key
        [TestMethod]
        public async Task ValidKey()
        {
            var map = await Client.Key("17f9");
            CheckTowerOfHeaven(map);
        }

        [TestMethod]
        public async Task InvalidKey()
        {
            var map = await Client.Key("map");
            Assert.IsNull(map);
        }

        [TestMethod]
        public async Task NullKey()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                var map = await Client.Key(null);
            });
        }
        #endregion

        #region Hash
        [TestMethod]
        public async Task ValidHash()
        {
            var map = await Client.Hash("108c239db3c0596f1ba7426353af1b4cc4fd8b08");
            CheckTowerOfHeaven(map);
        }

        [TestMethod]
        public async Task InvalidHash()
        {
            var map = await Client.Hash("map");
            Assert.IsNull(map);
        }

        [TestMethod]
        public async Task NullHash()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                var map = await Client.Hash(null);
            });
        }
        #endregion

        #region Search
        [TestMethod]
        public async Task ValidSearch()
        {
            var maps = await Client.Search("overcooked");
            Assert.IsTrue(maps.TotalDocs > 0);
            Assert.IsTrue(maps.Docs.Count > 0);
        }

        [TestMethod]
        public async Task NullSearch()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                var map = await Client.Search(null);
            });
        }
        #endregion

        #region SearchAdvanced
        [TestMethod]
        public async Task ValidSearchAdvanced()
        {
            var maps = await Client.SearchAdvanced("uploader.username:lolpants AND name:overcooked");
            Assert.IsTrue(maps.TotalDocs == 1);
            Assert.IsTrue(maps.Docs.Count == 1);

            var map = maps.Docs[0];
            CheckOvercooked(map);
        }

        [TestMethod]
        public async Task NullSearchAdvanced()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                var map = await Client.SearchAdvanced(null);
            });
        }
        #endregion

        #region User
        [TestMethod]
        public async Task ValidUser()
        {
            var user = await Client.User("5cff0b7298cc5a672c84e98d");

            Assert.AreEqual(user.ID, "5cff0b7298cc5a672c84e98d");
            Assert.AreEqual(user.Username, "bennydabeast");
        }

        [TestMethod]
        public async Task InvalidUser()
        {
            var user = await Client.User("user");
            Assert.IsNull(user);
        }

        [TestMethod]
        public async Task NullUser()
        {
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () =>
            {
                var user = await Client.User(null);
            });
        }
        #endregion
        #endregion
    }
}
