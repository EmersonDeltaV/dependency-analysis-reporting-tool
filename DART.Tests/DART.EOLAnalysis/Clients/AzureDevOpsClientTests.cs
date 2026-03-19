using DART.EOLAnalysis.Clients;
using DART.EOLAnalysis.Models;
using Microsoft.Extensions.Logging;
using NSubstitute;
using System.Net;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;

namespace DART.Tests.DART.EOLAnalysis.Clients
{
    public class AzureDevOpsClientTests
    {
        [Fact]
        public void Constructor_SetsBasicAuthHeaderAndJsonAcceptHeader()
        {
            var pat = "test-pat";
            var sut = new AzureDevOpsClient(pat, Substitute.For<ILogger<AzureDevOpsClient>>());

            var httpClient = GetPrivateHttpClient(sut);
            var expectedCredentials = Convert.ToBase64String(Encoding.ASCII.GetBytes($":{pat}"));

            Assert.NotNull(httpClient.DefaultRequestHeaders.Authorization);
            Assert.Equal("Basic", httpClient.DefaultRequestHeaders.Authorization!.Scheme);
            Assert.Equal(expectedCredentials, httpClient.DefaultRequestHeaders.Authorization.Parameter);
            Assert.Contains(httpClient.DefaultRequestHeaders.Accept, v => v.MediaType == "application/json");
        }

        [Fact]
        public async Task FindCsProjFilesAsync_FiltersExpectedFiles_AndAppendsBranchQuery()
        {
            var requestedUris = new List<Uri>();
            var responseBody = """
            {
              "value": [
                { "gitObjectType": "blob", "path": "/src/App/App.csproj" },
                { "gitObjectType": "blob", "path": "/src/App/App.txt" },
                { "gitObjectType": "tree", "path": "/src/Folder/App.csproj" },
                { "gitObjectType": "blob", "path": "/src/UnitTests/Test.csproj" },
                { "gitObjectType": "blob", "path": "/src/skip-me/Skipped.csproj" }
              ]
            }
            """;

            var sut = CreateClient(
                (request, _) =>
                {
                    requestedUris.Add(request.RequestUri!);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent(responseBody)
                    };
                });

            var repo = CreateRepository(branch: "feature/test", fileSkipFilter: ["skip-me"]);

            var result = await sut.FindCsProjFilesAsync(repo);

            Assert.Single(result);
            Assert.Equal("/src/App/App.csproj", result[0].Path);
            Assert.Single(requestedUris);
            Assert.Contains("versionDescriptor.version=feature%2Ftest", requestedUris[0].Query);
            Assert.Contains("versionDescriptor.versionType=branch", requestedUris[0].Query);
        }

        [Fact]
        public async Task FindPackageJsonFilesAsync_FiltersNodeModulesAndCustomSkipPaths()
        {
            var responseBody = """
            {
              "value": [
                { "gitObjectType": "blob", "path": "/web/package.json" },
                { "gitObjectType": "blob", "path": "/web/node_modules/x/package.json" },
                { "gitObjectType": "blob", "path": "/web/skip-this/package.json" },
                { "gitObjectType": "blob", "path": "/web/package-lock.json" },
                { "gitObjectType": "tree", "path": "/web/tree/package.json" }
              ]
            }
            """;

            var sut = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody)
            });

            var repo = CreateRepository(fileSkipFilter: ["skip-this"]);

            var result = await sut.FindPackageJsonFilesAsync(repo);

            Assert.Single(result);
            Assert.Equal("/web/package.json", result[0].Path);
        }

        [Fact]
        public async Task FindDirectoryPackagesPropsFilesAsync_MatchesFileNameOnlyAndAppliesExclusions()
        {
            var responseBody = """
            {
              "value": [
                { "gitObjectType": "blob", "path": "/src/Directory.Packages.props" },
                { "gitObjectType": "blob", "path": "/src/sub/Directory.Packages.props" },
                { "gitObjectType": "blob", "path": "/src/sub/Directory.Packages.props.backup" },
                { "gitObjectType": "blob", "path": "/src/UnitTests/Directory.Packages.props" },
                { "gitObjectType": "blob", "path": "/src/legacy/Directory.Packages.props" }
              ]
            }
            """;

            var sut = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody)
            });

            var repo = CreateRepository(fileSkipFilter: ["legacy"]);

            var result = await sut.FindDirectoryPackagesPropsFilesAsync(repo);

            Assert.Equal(2, result.Count);
            Assert.Contains(result, item => item.Path == "/src/Directory.Packages.props");
            Assert.Contains(result, item => item.Path == "/src/sub/Directory.Packages.props");
        }

        [Fact]
        public async Task FindCsProjFilesAsync_ReturnsEmpty_WhenResponseIsNonSuccess()
        {
            var sut = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.InternalServerError)
            {
                ReasonPhrase = "server error"
            });

            var repo = CreateRepository();
            var result = await sut.FindCsProjFilesAsync(repo);

            Assert.Empty(result);
        }

        [Fact]
        public async Task FindPackageJsonFilesAsync_ReturnsEmpty_WhenStatusIs203()
        {
            var sut = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.NonAuthoritativeInformation));
            var repo = CreateRepository();

            var result = await sut.FindPackageJsonFilesAsync(repo);

            Assert.Empty(result);
        }

        [Fact]
        public async Task FindDirectoryPackagesPropsFilesAsync_ReturnsEmpty_WhenJsonIsInvalid()
        {
            var sut = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{not-json")
            });
            var repo = CreateRepository();

            var result = await sut.FindDirectoryPackagesPropsFilesAsync(repo);

            Assert.Empty(result);
        }

        [Fact]
        public async Task FindCsProjFilesAsync_ReturnsEmpty_WhenDeserializedValueIsNull()
        {
            var sut = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"count\":1,\"value\":null}")
            });
            var repo = CreateRepository();

            var result = await sut.FindCsProjFilesAsync(repo);

            Assert.Empty(result);
        }

        [Fact]
        public async Task GetFileContentAsync_ReturnsResponseBody_WhenRequestSucceeds()
        {
            var requestedUris = new List<Uri>();
            var sut = CreateClient(
                (request, _) =>
                {
                    requestedUris.Add(request.RequestUri!);
                    return new HttpResponseMessage(HttpStatusCode.OK)
                    {
                        Content = new StringContent("file-body")
                    };
                });

            var repo = CreateRepository(branch: "feature/my branch");
            var content = await sut.GetFileContentAsync(repo, "/src/My Folder/App.csproj");

            Assert.Equal("file-body", content);
            Assert.Single(requestedUris);
            Assert.Contains("path=%2Fsrc%2FMy%20Folder%2FApp.csproj", requestedUris[0].Query);
            Assert.Contains("versionDescriptor.version=feature%2Fmy%20branch", requestedUris[0].Query);
        }

        [Fact]
        public async Task GetFileContentAsync_ReturnsEmpty_WhenRequestFails()
        {
            var sut = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                ReasonPhrase = "bad request",
                Content = new StringContent("error")
            });

            var repo = CreateRepository(branch: "");
            var content = await sut.GetFileContentAsync(repo, "/src/App.csproj");

            Assert.Equal(string.Empty, content);
        }

        [Fact]
        public void Dispose_CanBeCalledMultipleTimes_AndProtectedDisposeFalseSetsDisposed()
        {
            var logger = Substitute.For<ILogger<AzureDevOpsClient>>();
            var sut = new TestableAzureDevOpsClient("pat", logger);

            sut.InvokeProtectedDispose(false);
            Assert.True(GetDisposedFlag(sut));

            sut.Dispose();
            sut.Dispose();
        }

        [Fact]
        public async Task Dispose_WithDisposingTrue_DisposesHttpClient()
        {
            var sut = new TestableAzureDevOpsClient("pat", Substitute.For<ILogger<AzureDevOpsClient>>());
            SetPrivateHttpClient(sut, new HttpClient(new StubHttpMessageHandler((_, _) => new HttpResponseMessage(HttpStatusCode.OK))));

            sut.InvokeProtectedDispose(true);

            await Assert.ThrowsAsync<ObjectDisposedException>(() => sut.GetFileContentAsync(CreateRepository(), "/src/App.csproj"));
        }

        [Fact]
        public async Task FindCsProjFilesAsync_ReturnsEmpty_WhenJsonBodyIsNullLiteral()
        {
            var sut = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("null")
            });
            var repo = CreateRepository();

            var result = await sut.FindCsProjFilesAsync(repo);

            Assert.Empty(result);
        }

        [Fact]
        public async Task FindDirectoryPackagesPropsFilesAsync_IgnoresEmptyPathItems()
        {
            var responseBody = """
            {
              "value": [
                { "gitObjectType": "blob", "path": "" },
                { "gitObjectType": "blob", "path": "/src/Directory.Packages.props" }
              ]
            }
            """;

            var sut = CreateClient((_, _) => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(responseBody)
            });

            var result = await sut.FindDirectoryPackagesPropsFilesAsync(CreateRepository());

            Assert.Single(result);
            Assert.Equal("/src/Directory.Packages.props", result[0].Path);
        }

        private static Repository CreateRepository(string? branch = "main", List<string>? fileSkipFilter = null)
        {
            var repository = new Repository
            {
                Url = "https://dev.azure.com/org-name/project-name/_git/repo-name",
                Branch = branch ?? "main",
                FileSkipFilter = fileSkipFilter ?? []
            };

            repository.ParseUrl();
            return repository;
        }

        private static AzureDevOpsClient CreateClient(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler)
        {
            var logger = Substitute.For<ILogger<AzureDevOpsClient>>();
            var client = new AzureDevOpsClient("pat", logger);
            var fakeHttpClient = new HttpClient(new StubHttpMessageHandler(handler));
            fakeHttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", "any");
            fakeHttpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            SetPrivateHttpClient(client, fakeHttpClient);
            return client;
        }

        private static HttpClient GetPrivateHttpClient(AzureDevOpsClient client)
        {
            var field = typeof(AzureDevOpsClient).GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            return (HttpClient)field!.GetValue(client)!;
        }

        private static void SetPrivateHttpClient(AzureDevOpsClient client, HttpClient replacement)
        {
            var field = typeof(AzureDevOpsClient).GetField("_httpClient", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            var original = (HttpClient)field!.GetValue(client)!;
            field.SetValue(client, replacement);
            original.Dispose();
        }

        private static bool GetDisposedFlag(AzureDevOpsClient client)
        {
            var field = typeof(AzureDevOpsClient).GetField("_disposed", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            return (bool)field!.GetValue(client)!;
        }

        private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, CancellationToken, HttpResponseMessage> handler) : HttpMessageHandler
        {
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            {
                return Task.FromResult(handler(request, cancellationToken));
            }
        }

        private sealed class TestableAzureDevOpsClient : AzureDevOpsClient
        {
            public TestableAzureDevOpsClient(string pat, ILogger<AzureDevOpsClient> logger)
                : base(pat, logger)
            {
            }

            public void InvokeProtectedDispose(bool disposing)
            {
                base.Dispose(disposing);
            }
        }
    }
}
