using EchoHub.Core.Constants;
using EchoHub.Core.Models;
using EchoHub.Server.Config;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EchoHub.Tests;

public class UploadLimitsTests
{
    [Fact]
    public void Defaults_MirrorHubConstants()
    {
        var limits = new UploadLimits();

        Assert.Equal(HubConstants.MaxFileSizeBytes, limits.MaxFileSizeBytes);
        Assert.Equal(HubConstants.MaxImageSizeBytes, limits.MaxImageSizeBytes);
        Assert.Equal(HubConstants.MaxAudioFileSizeBytes, limits.MaxAudioSizeBytes);
        Assert.Equal(HubConstants.MaxAvatarSizeBytes, limits.MaxAvatarSizeBytes);
        Assert.Equal(HubConstants.MaxAttachmentsPerMessage, limits.MaxAttachmentsPerMessage);
    }

    [Fact]
    public void MaxForKind_MapsEachAttachmentKind()
    {
        var limits = new UploadLimits
        {
            MaxImageSizeMB = 5,
            MaxAudioSizeMB = 7,
            MaxFileSizeMB = 11,
        };

        Assert.Equal(5L * 1024 * 1024, limits.MaxForKind(AttachmentKind.Image));
        Assert.Equal(7L * 1024 * 1024, limits.MaxForKind(AttachmentKind.Audio));
        Assert.Equal(11L * 1024 * 1024, limits.MaxForKind(AttachmentKind.File));
    }

    [Fact]
    public void MaxRequestBodyBytes_IsFileSizeTimesAttachmentCap()
    {
        var limits = new UploadLimits { MaxFileSizeMB = 20, MaxAttachmentsPerMessage = 4 };

        Assert.Equal(20L * 1024 * 1024 * 4, limits.MaxRequestBodyBytes);
    }

    [Fact]
    public void BoundFromConfiguration_OverridesDefaults()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Uploads:MaxFileSizeMB"] = "250",
                ["Uploads:MaxImageSizeMB"] = "25",
                ["Uploads:MaxAttachmentsPerMessage"] = "3",
            })
            .Build();

        var limits = config.GetSection("Uploads").Get<UploadLimits>()!;

        Assert.Equal(250L * 1024 * 1024, limits.MaxFileSizeBytes);
        Assert.Equal(25L * 1024 * 1024, limits.MaxImageSizeBytes);
        Assert.Equal(3, limits.MaxAttachmentsPerMessage);
        // Unspecified values keep their HubConstants-derived defaults.
        Assert.Equal(HubConstants.MaxAudioFileSizeBytes, limits.MaxAudioSizeBytes);
    }
}
