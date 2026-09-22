using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using Pry.Core.Inference;
using Pry.Core.Models;
using Xunit;

namespace Pry.Core.Tests;

public sealed class LlamaServerSecurityTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Launch_keeps_key_out_of_argv_and_preserves_model_options(bool useGpu)
    {
        var secret = Guid.NewGuid().ToString("N");
        var hostKey = Environment.GetEnvironmentVariable("LLAMA_API_KEY");
        var profile = new ModelProfile
        {
            Id = "local", DisplayName = "Local", ModelPath = "models/model with spaces.gguf",
            MmprojPath = "models/vision projection.gguf", ApiKey = secret,
            BaseUrl = "http://127.0.0.1:51234/v1", ContextSize = 2048, GpuLayers = 12,
            EnableThinking = false
        };
        var info = LlamaServerManager.CreateStartInfo("runtime/llama-server.exe", profile,
            useGpu ? new LlamaComputeDevice("CUDA0", "Test device", false) : null);

        Assert.False(info.UseShellExecute);
        Assert.True(info.CreateNoWindow);
        Assert.True(info.RedirectStandardError);
        Assert.Equal(secret, info.Environment["LLAMA_API_KEY"]);
        Assert.False(info.Environment.ContainsKey("LLAMA_ARG_API_KEY_FILE"));
        Assert.DoesNotContain(secret, info.Arguments);
        Assert.All(info.ArgumentList, argument => Assert.DoesNotContain(secret, argument));
        Assert.DoesNotContain("--api-key", info.ArgumentList);
        Assert.DoesNotContain("--api-key-file", info.ArgumentList);
        Assert.Equal("127.0.0.1", ValueAfter(info, "--host"));
        Assert.Equal("51234", ValueAfter(info, "--port"));
        Assert.Equal(profile.ModelPath, ValueAfter(info, "-m"));
        Assert.Equal(profile.MmprojPath, ValueAfter(info, "--mmproj"));
        Assert.Equal(useGpu ? "12" : "0", ValueAfter(info, "-ngl"));
        Assert.Equal("off", ValueAfter(info, "--reasoning"));
        Assert.Equal(hostKey, Environment.GetEnvironmentVariable("LLAMA_API_KEY"));
    }

    [Fact]
    public void Embeddings_only_launch_keeps_embedding_mode_and_still_hides_the_key()
    {
        var secret = Guid.NewGuid().ToString("N");
        var profile = new ModelProfile
        {
            Id = "local", DisplayName = "Local", ModelPath = "models/embedding.gguf", ApiKey = secret,
            BaseUrl = "http://127.0.0.1:51235/v1", ContextSize = 4096, EnableThinking = true
        };
        var info = LlamaServerManager.CreateStartInfo("runtime/llama-server.exe", profile, null, embeddingsOnly: true);

        Assert.Contains("--embedding", info.ArgumentList);
        Assert.DoesNotContain("--reasoning", info.ArgumentList);
        Assert.DoesNotContain(secret, info.Arguments);
        Assert.Equal(secret, info.Environment["LLAMA_API_KEY"]);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" \t")]
    [InlineData("replacement-test-value")]
    public void Child_authentication_cannot_inherit_extra_keys(string? configuredKey)
    {
        var info = new ProcessStartInfo();
        info.Environment["LLAMA_API_KEY"] = "inherited-test-value";
        info.Environment["LLAMA_ARG_API_KEY_FILE"] = "unrelated-keys.txt";
        info.Environment["PRY_TEST_UNRELATED"] = "preserved";

        LlamaServerManager.ConfigureAuthentication(info, configuredKey);

        Assert.False(info.Environment.ContainsKey("LLAMA_ARG_API_KEY_FILE"));
        if (string.IsNullOrWhiteSpace(configuredKey)) Assert.False(info.Environment.ContainsKey("LLAMA_API_KEY"));
        else Assert.Equal(configuredKey, info.Environment["LLAMA_API_KEY"]);
        Assert.Equal("preserved", info.Environment["PRY_TEST_UNRELATED"]);
    }

    [Fact]
    public async Task Configured_llama_server_requires_the_child_environment_key()
    {
        var executable = Environment.GetEnvironmentVariable("PRY_TEST_LLAMA_SERVER");
        var model = Environment.GetEnvironmentVariable("PRY_TEST_LLAMA_MODEL");
        if (string.IsNullOrWhiteSpace(executable) || string.IsNullOrWhiteSpace(model))
            Assert.Skip("Set PRY_TEST_LLAMA_SERVER and PRY_TEST_LLAMA_MODEL for the local authentication integration test.");
        Assert.True(File.Exists(executable));
        Assert.True(File.Exists(model));

        int port;
        using (var listener = new TcpListener(IPAddress.Loopback, 0))
        {
            listener.Start();
            port = ((IPEndPoint)listener.LocalEndpoint).Port;
        }
        var profile = new ModelProfile
        {
            Id = "auth-test", DisplayName = "Authentication test", ModelPath = model,
            ApiKey = Guid.NewGuid().ToString("N"), BaseUrl = $"http://127.0.0.1:{port}/v1",
            ComputeDevice = "cpu", ContextSize = 512
        };
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(TestContext.Current.CancellationToken);
        deadline.CancelAfter(TimeSpan.FromSeconds(90));
        var manager = new LlamaServerManager();
        try
        {
            await manager.StartAsync(executable!, profile, deadline.Token);
            using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            foreach (var key in new[] { null, "incorrect-test-value", profile.ApiKey })
            {
                // /health and /v1/models are public in llama.cpp; exercise a protected inference route.
                using var request = new HttpRequestMessage(HttpMethod.Post, $"{profile.BaseUrl}/completions")
                {
                    Content = new StringContent("{\"prompt\":\"Hello\",\"max_tokens\":1,\"stream\":false}",
                        System.Text.Encoding.UTF8, "application/json")
                };
                if (key is not null) request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", key);
                using var response = await client.SendAsync(request, deadline.Token);
                Assert.Equal(key == profile.ApiKey ? HttpStatusCode.OK : HttpStatusCode.Unauthorized, response.StatusCode);
                Assert.DoesNotContain(profile.ApiKey!, await response.Content.ReadAsStringAsync(deadline.Token));
            }
        }
        finally { await manager.DisposeAsync(); }
        Assert.False(manager.IsRunning);
    }

    private static string ValueAfter(ProcessStartInfo info, string name) =>
        info.ArgumentList[info.ArgumentList.IndexOf(name) + 1];
}
