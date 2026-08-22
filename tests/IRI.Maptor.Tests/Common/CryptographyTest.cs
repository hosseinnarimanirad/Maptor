using IRI.Maptor.Core.Security;

namespace IRI.Maptor.Tests.Common;

/// <summary>
/// Tests for RSA+AES encryption/decryption functionality using EncryptedMessage
/// </summary>
public class CryptographyTest
{
    // Test fixture data - RSA key pair for testing
    private const string TestPublicKey = @"PFJTQUtleVZhbHVlPjxNb2R1bHVzPjh6VU9UWnM1WW81RWlqa3pJZFZqaVE3Zkx3SVk1NlZoeGUxdVl0KzU4djBPSGZ5VDhsczFmRzcxYUg1LzQrVlFSVnV2NEpFSnhQQW5VZGtPakJTa3U0eUxBdEhUazRaVEQ4SzE4cFNuTlNjeHZzRytvOEVxWWtlakVvcmwyZ3VDQ1l6VlBjdCs3clRJbXVhNzZUR1VnYjlTL3Z2ei8rMmNsS1RpZUtMUmluaz08L01vZHVsdXM+PEV4cG9uZW50PkFRQUI8L0V4cG9uZW50PjwvUlNBS2V5VmFsdWU+";

    private const string TestPrivateKey = @"PFJTQUtleVZhbHVlPjxNb2R1bHVzPjh6VU9UWnM1WW81RWlqa3pJZFZqaVE3Zkx3SVk1NlZoeGUxdVl0KzU4djBPSGZ5VDhsczFmRzcxYUg1LzQrVlFSVnV2NEpFSnhQQW5VZGtPakJTa3U0eUxBdEhUazRaVEQ4SzE4cFNuTlNjeHZzRytvOEVxWWtlakVvcmwyZ3VDQ1l6VlBjdCs3clRJbXVhNzZUR1VnYjlTL3Z2ei8rMmNsS1RpZUtMUmluaz08L01vZHVsdXM+PEV4cG9uZW50PkFRQUI8L0V4cG9uZW50PjxQPjlOK0grNFZzQVNLVUM2NG1oWWJsV2pRTFc1a2crRjhNeTFxSlV0cHN0WW8wczJIRWM5cHNwT1plbTI3NlVNckdINkRSbmhVM040U3FpZkFCMmhlWjN3PT08L1A+PFE+L2tJbFlPVi9DL2U3bEdYbFlNRjB6N0JqdVBxQkY4UUtoWWZEY1RObWhHTGI4VmI1K2F0R0s3Y0FHelZMYWZuUHl0N3hjeFNTekwyTjVKSGQ5aTBXcHc9PTwvUT48RFA+MUlidFRxK04vYTQxTDY0R1lQMmpNWmJhQkxYeWw1NW5URmRYdUVFNitKVHJDSVZpSytyM1FHZHcxUmFNeW5JelltQUJqbUo3ZWdQNnY1MCsvanBkb1E9PTwvRFA+PERRPjNIMStMcTQyWTZsOCtPNzRZTlRET015TlhqK1dyWVpyWFdyam1RcHJEOGt2VlBZSkozTlpFZFhMK014Wnp0ZzlVMy9Nd1BDSmNaVzhOQWd2QlNvS3B3PT08L0RRPjxJbnZlcnNlUT4zYVg4SWtmeUlzQzFWMHdSeWYxcnhOU3VoRENvREc3Qkh5Y0xrQ2dTczVqUTQ4TDFTbVRMdysvZGgrUTMxVUswVXdleG5WbWhjamsrMHdpTGxENXlzQT09PC9JbnZlcnNlUT48RD5QeXNpb2VtVlNCSG5uM2NuM3J2TDlJZFdWS0ZZMHFIVCtWS24veXBZNDlIeVhydUJ1Y3NTNDFUMmpNTitlRFRSV3BKcjVnb0YzWTc2eDNsM0c4OG8wY1FVNWN0dStZTXl5RVMyaEhINTFTdEJiMVpzK3kxOTFXeXR1VERpWHpNRVhIbjlITi9Ob2s2ckpDRGhvdVZ6TDYvQmhhaUFDT3lwT1pZdUZNTnJMaGs9PC9EPjwvUlNBS2V5VmFsdWU+";

    [Fact]
    public void EncryptAndDecrypt_WithSimpleString_ShouldReturnOriginalMessage()
    {
        // Arrange
        const string originalMessage = "hello!";

        // Act
        var encryptedMessage = EncryptedMessage.Create(originalMessage, TestPublicKey);
        var decryptedMessage = encryptedMessage?.Decrypt<string>(TestPrivateKey);

        // Assert
        Assert.NotNull(encryptedMessage);
        Assert.NotNull(decryptedMessage);
        Assert.Equal(originalMessage, decryptedMessage);
    }

    [Fact]
    public void EncryptedMessage_Create_ShouldPopulateAllRequiredProperties()
    {
        // Arrange
        const string testMessage = "test data";

        // Act
        var encryptedMessage = EncryptedMessage.Create(testMessage, TestPublicKey);

        // Assert
        Assert.NotNull(encryptedMessage);
        Assert.NotNull(encryptedMessage.Token);
        Assert.NotEmpty(encryptedMessage.Token);
        Assert.NotNull(encryptedMessage.IV);
        Assert.NotEmpty(encryptedMessage.IV);
        Assert.NotNull(encryptedMessage.Message);
        Assert.NotEmpty(encryptedMessage.Message);
    }

    [Fact]
    public void EncryptAndDecrypt_WithComplexObject_ShouldReturnEquivalentObject()
    {
        // Arrange
        var originalObject = new TestDataObject
        {
            Id = 123,
            Name = "Test User",
            Email = "test@example.com",
            IsActive = true
        };

        // Act
        var encryptedMessage = EncryptedMessage.Create(originalObject, TestPublicKey);
        var decryptedObject = encryptedMessage?.Decrypt<TestDataObject>(TestPrivateKey);

        // Assert
        Assert.NotNull(encryptedMessage);
        Assert.NotNull(decryptedObject);
        Assert.Equal(originalObject.Id, decryptedObject!.Id);
        Assert.Equal(originalObject.Name, decryptedObject.Name);
        Assert.Equal(originalObject.Email, decryptedObject.Email);
        Assert.Equal(originalObject.IsActive, decryptedObject.IsActive);
    }

    [Fact]
    public void Decrypt_WithWrongPrivateKey_ShouldReturnNull()
    {
        // Arrange
        const string message = "secret message";
        const string wrongPrivateKey = "invalid_key";
        var encryptedMessage = EncryptedMessage.Create(message, TestPublicKey);

        // Act
        var decryptedMessage = encryptedMessage?.Decrypt<string>(wrongPrivateKey);

        // Assert
        Assert.Null(decryptedMessage);
    }

    [Fact]
    public void Create_WithInvalidPublicKey_ShouldReturnNull()
    {
        // Arrange
        const string message = "test message";
        const string invalidPublicKey = "invalid_public_key";

        // Act
        var encryptedMessage = EncryptedMessage.Create(message, invalidPublicKey);

        // Assert
        Assert.Null(encryptedMessage);
    }

    [Fact]
    public void EncryptedMessage_MultipleCalls_ShouldProduceDifferentCiphertext()
    {
        // Arrange
        const string message = "same message";

        // Act
        var encrypted1 = EncryptedMessage.Create(message, TestPublicKey);
        var encrypted2 = EncryptedMessage.Create(message, TestPublicKey);

        // Assert - Due to random IV, encrypted messages should differ
        Assert.NotNull(encrypted1);
        Assert.NotNull(encrypted2);
        Assert.NotEqual(encrypted1.Message, encrypted2.Message);
        Assert.NotEqual(encrypted1.IV, encrypted2.IV);

        // But both should decrypt to the same original message
        var decrypted1 = encrypted1.Decrypt<string>(TestPrivateKey);
        var decrypted2 = encrypted2.Decrypt<string>(TestPrivateKey);
        Assert.Equal(message, decrypted1);
        Assert.Equal(message, decrypted2);
    }

    [Fact]
    public void EncryptAndDecrypt_WithEmptyString_ShouldReturnEmptyString()
    {
        // Arrange
        const string emptyMessage = "";

        // Act
        var encryptedMessage = EncryptedMessage.Create(emptyMessage, TestPublicKey);
        var decryptedMessage = encryptedMessage?.Decrypt<string>(TestPrivateKey);

        // Assert
        Assert.NotNull(encryptedMessage);
        Assert.NotNull(decryptedMessage);
        Assert.Equal(emptyMessage, decryptedMessage);
    }

    [Theory]
    [InlineData("Short")]
    [InlineData("This is a much longer message that contains multiple words and special characters: !@#$%^&*()")]
    [InlineData("Unicode test: 你好世界 مرحبا العالم Привет мир")]
    [InlineData("12345")]
    public void EncryptAndDecrypt_WithVariousStringFormats_ShouldReturnOriginalMessage(string testMessage)
    {
        // Arrange - test message provided by theory data

        // Act
        var encryptedMessage = EncryptedMessage.Create(testMessage, TestPublicKey);
        var decryptedMessage = encryptedMessage?.Decrypt<string>(TestPrivateKey);

        // Assert
        Assert.NotNull(encryptedMessage);
        Assert.NotNull(decryptedMessage);
        Assert.Equal(testMessage, decryptedMessage);
    }

    #region Test Data Classes

    /// <summary>
    /// Test data object for complex object encryption tests
    /// </summary>
    private class TestDataObject
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    #endregion
}
