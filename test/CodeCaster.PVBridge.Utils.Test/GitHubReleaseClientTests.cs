using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading.Tasks;
using CodeCaster.PVBridge.Utils.GitHub;
using Moq;
using NUnit.Framework;
using PVBridge.Test.Shared;

namespace CodeCaster.PVBridge.Utils.Test
{
    public class GitHubReleaseClientTests
    {
#pragma warning disable CS8618 // Setup() for each call
        private GitHubReleaseClient _classUnderTest;
        private Release _fakeRelease;
#pragma warning restore CS8618

        [SetUp]
        public void Setup()
        {
            var mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);

            _fakeRelease = new Release
            {
                TagName = "v0.0.5",
                PublishedAt = new DateTime(202, 04, 25, 15, 10, 39, DateTimeKind.Utc),
                HtmlUrl = "https://github.com/CodeCasterNL/WindowsServiceExtensions/releases/tag/v3.0.1",
                Body = "Fix: Test.\r\n\r\nFix another: Test2.\r\n\r\n**Full Changelog**: https://github.com/CodeCasterNL/PVBridge/compare/v0.0.4...v0.0.5"
            };

            mockHandler.MockResponse((request, token) =>
            {
                var releaseJson = JsonSerializer.Serialize(_fakeRelease);

                var message = new HttpResponseMessage
                {
                    Content = new StringContent(releaseJson)
                    {
                        Headers = { ContentType = MediaTypeHeaderValue.Parse("application/json") }
                    }
                };

                return message;
            });

            var httpClient = new HttpClient(mockHandler.Object);

            _classUnderTest = new GitHubReleaseClient(httpClient);
        }

        [Test]
        public async Task GetLatestRelease_Parses_Json()
        {
            // Act
            var release = await _classUnderTest!.GetLatestAsync();

            // Assert
            Assert.That(release, Is.Not.Null);
            
            Assert.Multiple(() =>
            {
                Assert.That(release!.TagName, Is.EqualTo(_fakeRelease.TagName));
                Assert.That(release.PublishedAt, Is.EqualTo(_fakeRelease.PublishedAt));
                Assert.That(release.HtmlUrl, Is.EqualTo(_fakeRelease.HtmlUrl));
                Assert.That(release.Body, Is.EqualTo(_fakeRelease.Body));
            });
        }
    }
}
