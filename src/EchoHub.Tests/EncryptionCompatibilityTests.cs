using System.Security.Cryptography;
using EchoHub.Client.Services;
using EchoHub.Server.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace EchoHub.Tests;

/// <summary>
/// Tests that server and client encryption services are fully interoperable —
/// content encrypted by one can be decrypted by the other using the same key.
/// </summary>
public class EncryptionCompatibilityTests
{
    private static readonly string SharedKey =
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

    private static MessageEncryptionService CreateServer(string? key = null)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Encryption:Key"] = key ?? SharedKey,
                ["Encryption:EncryptDatabase"] = "false",
            })
            .Build();

        var logger = NullLoggerFactory.Instance.CreateLogger<MessageEncryptionService>();
        return new MessageEncryptionService(config, logger);
    }

    private static ClientEncryptionService CreateClient(string? key = null)
    {
        var service = new ClientEncryptionService();
        service.SetKey(key ?? SharedKey);
        return service;
    }

    // ── Cross-service round trips ────────────────────────────────────

    [Fact]
    public void ClientEncrypt_ServerDecrypt()
    {
        var client = CreateClient();
        var server = CreateServer();
        var original = "Hello from client!";

        var encrypted = client.Encrypt(original);
        var decrypted = server.Decrypt(encrypted);

        Assert.Equal(original, decrypted);
    }

    [Fact]
    public void ServerEncrypt_ClientDecrypt()
    {
        var server = CreateServer();
        var client = CreateClient();
        var original = "Hello from server!";

        var encrypted = server.Encrypt(original);
        var decrypted = client.Decrypt(encrypted);

        Assert.Equal(original, decrypted);
    }

    [Theory]
    [InlineData("")]
    [InlineData("a")]
    [InlineData("Hello, world!")]
    [InlineData("Hello 🌍 世界 مرحبا")]
    [InlineData("Line1\nLine2\nLine3")]
    public void CrossDecrypt_VariousMessages(string message)
    {
        var server = CreateServer();
        var client = CreateClient();

        // Client → Server
        var clientEncrypted = client.Encrypt(message);
        Assert.Equal(message, server.Decrypt(clientEncrypted));

        // Server → Client
        var serverEncrypted = server.Encrypt(message);
        Assert.Equal(message, client.Decrypt(serverEncrypted));
    }

    [Fact]
    public void CrossDecrypt_LargeMessage()
    {
        var server = CreateServer();
        var client = CreateClient();
        var original = new string('X', 10000);

        var encrypted = client.Encrypt(original);
        Assert.Equal(original, server.Decrypt(encrypted));
    }

    // ── Key mismatch ─────────────────────────────────────────────────

    [Fact]
    public void DifferentKeys_ClientEncrypt_ServerCantDecrypt()
    {
        var clientKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var serverKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var client = CreateClient(clientKey);
        var server = CreateServer(serverKey);

        var encrypted = client.Encrypt("secret");
        var result = server.Decrypt(encrypted);

        Assert.Contains("decryption failed", result);
    }

    [Fact]
    public void DifferentKeys_ServerEncrypt_ClientCantDecrypt()
    {
        var clientKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));
        var serverKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32));

        var server = CreateServer(serverKey);
        var client = CreateClient(clientKey);

        var encrypted = server.Encrypt("secret");
        var result = client.Decrypt(encrypted);

        Assert.Contains("decryption failed", result);
    }

    // ── Plaintext passthrough ────────────────────────────────────────

    [Fact]
    public void Server_DecryptsPlaintext_AsPassthrough()
    {
        var server = CreateServer();
        Assert.Equal("plain text", server.Decrypt("plain text"));
    }

    [Fact]
    public void Client_DecryptsPlaintext_AsPassthrough()
    {
        var client = CreateClient();
        Assert.Equal("plain text", client.Decrypt("plain text"));
    }

    // ── Full E2E flow simulation ─────────────────────────────────────

    [Fact]
    public void FullFlow_ClientSend_ServerProcess_BroadcastBack()
    {
        var client = CreateClient();
        var server = CreateServer();

        // 1. Client encrypts and sends
        var originalMessage = "Hello everyone!";
        var clientEncrypted = client.Encrypt(originalMessage);
        Assert.StartsWith("$ENC$v1$", clientEncrypted);

        // 2. Server decrypts for processing
        var serverPlaintext = server.Decrypt(clientEncrypted);
        Assert.Equal(originalMessage, serverPlaintext);

        // 3. Server re-encrypts for broadcast (different nonce)
        var serverEncrypted = server.Encrypt(serverPlaintext);
        Assert.StartsWith("$ENC$v1$", serverEncrypted);
        Assert.NotEqual(clientEncrypted, serverEncrypted); // different nonce

        // 4. Receiving client decrypts the broadcast
        var receivedPlaintext = client.Decrypt(serverEncrypted);
        Assert.Equal(originalMessage, receivedPlaintext);
    }

    [Fact]
    public void FullFlow_ServerGeneratedMessage_EncryptForBroadcast()
    {
        var server = CreateServer();
        var client = CreateClient();

        // Server generates a system message (e.g. file upload notification)
        var systemMessage = "user uploaded file.png";

        // Server encrypts for broadcast
        var encrypted = server.Encrypt(systemMessage);

        // Client decrypts
        var decrypted = client.Decrypt(encrypted);
        Assert.Equal(systemMessage, decrypted);
    }

    [Fact]
    public void FullFlow_HistoryRetrieve_ServerEncrypts_ClientDecrypts()
    {
        var server = CreateServer();
        var client = CreateClient();

        // Simulate loading N messages from DB (plaintext) and encrypting for transport
        var messages = new[] { "msg1", "Hello 🌍", "msg with\nnewline" };

        foreach (var original in messages)
        {
            var encrypted = server.Encrypt(original);
            var decrypted = client.Decrypt(encrypted);
            Assert.Equal(original, decrypted);
        }
    }
}
